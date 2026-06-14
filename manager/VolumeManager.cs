using UnityEngine;
using URPGlitch;
using UnityEngine.Rendering;
using DG.Tweening;

public class VolumeManager : MonoBehaviour
{
    [Header("Volume Reference")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private Volume glitchVolume;
    [SerializeField] private Volume pauseVolume;

    [Header("Pause Volume Animation")]
    [SerializeField] private float pauseFadeDuration = 0.5f;
    [SerializeField] private Ease pauseFadeEase = Ease.OutQuad;

    [Header("Duration Settings")]
    [Tooltip("反抗值=1时，单次Glitch持续的最长时间")]
    public float maxDuration = 1.0f;
    [Tooltip("反抗值=0时，单次Glitch持续的最短时间")]
    public float minDuration = 0.1f;
    [Tooltip("是否使用固定单次 Glitch 时长（优先于 min/max 计算）")]
    public bool useFixedDuration = true;
    [Tooltip("固定单次 Glitch 时长（秒）")]
    public float fixedGlitchDuration = 0.5f;

    [Header("Cooldown Settings")]
    [Tooltip("反抗值=1时，两次Glitch之间的最短冷却")]
    public float minCooldown = 0.3f;
    [Tooltip("反抗值=0时，两次Glitch之间的最长冷却")]
    public float maxCooldown = 5.0f;

    [Header("Intensity Settings")]
    [Tooltip("Analog Glitch 强度缩放系数")]
    public float analogGlitchScale = 0.3f;
    [Tooltip("目标强度的随机偏移比例（相对于反抗值）")]
    public float randomOffsetRatio = 0.15f;
    [Tooltip("外部传入的 resistanceDegree 到实际强度的缩放（可降低抵抗度影响）")]
    public float intensityScale = 0.5f;
    [Tooltip("脉动计算的基准倍率，越小触发越少")]
    public float pulseMultiplier = 5f;

    // Volume Profile 中的 Glitch 参数组件
    private DigitalGlitchVolume _digitalGlitch;
    private AnalogGlitchVolume _analogGlitch;

    // 状态
    private float _currentIntensity = 0f;
    private float _targetIntensity = 0f;
    private float _glitchEndTime = 0f;
    private float _cooldownEndTime = 0f;
    private bool _isGlitching = false;

    // Pause Volume Tween
    private Tween _pauseTween;

    void Start()
    {
        TryGetComponents();
        Debug.LogFormat("[VolumeManager] Start: glitchVolume={0}, globalVolume={1}, digitalGlitch={2}, analogGlitch={3}",
            glitchVolume != null ? glitchVolume.name : "null",
            globalVolume != null ? globalVolume.name : "null",
            _digitalGlitch != null ? _digitalGlitch.name : "null",
            _analogGlitch != null ? _analogGlitch.name : "null");
        //ResetGlitch();
    }

    void Update()
    {
        // 如果反抗值为0，不做任何处理
        if (_targetIntensity <= 0f)
        {
            if (_isGlitching)
                ResetGlitch();
            return;
        }

        // 冷却中，不触发
        if (Time.time < _cooldownEndTime)
            return;

        // Glitch 进行中
        if (_isGlitching)
        {
            // 不规律跳动：在持续时间内，强度在 0 和目标值之间随机切换
            float pulseProbability = pulseMultiplier * _targetIntensity * Time.deltaTime;
            if (Random.value < pulseProbability)
            {
                if (_currentIntensity < 0.01f)
                {
                    // 从0跳到目标强度（加入随机偏移）
                    float randomOffset = _targetIntensity * randomOffsetRatio * Random.Range(-1f, 1f);
                    _currentIntensity = Mathf.Clamp01(_targetIntensity + randomOffset);
                }
                else
                {
                    _currentIntensity = 0f;
                }
                ApplyGlitch();
            }

            // 持续时间结束
            if (Time.time >= _glitchEndTime)
            {
                EndGlitch();
            }
        }
        else
        {
            // 不在 Glitch 中 → 触发新一次 Glitch
            StartGlitch();
        }
    }

    /// <summary>
    /// 外部调用：传入当前反抗值，触发 Glitch 效果
    /// </summary>
    /// <param name="resistanceDegree">当前反抗值 (0-1)</param>
    public void TriggerGlitch(float resistanceDegree)
    {
        Debug.Log($"[VolumeManager] TriggerGlitch called with resistanceDegree={resistanceDegree}");
        // 允许缩放抵抗值对最终强度的影响，降低过强效果
        _targetIntensity = Mathf.Clamp01(resistanceDegree * intensityScale);

        if (_targetIntensity <= 0f)
        {
            ResetGlitch();
            return;
        }

        if (_digitalGlitch == null || _analogGlitch == null)
            TryGetComponents();

        var targetVolume = glitchVolume != null ? glitchVolume : globalVolume;
        if (targetVolume == null)
        {
            Debug.LogWarning("[VolumeManager] No target volume assigned for glitch. Please assign glitchVolume or globalVolume.");
        }
        else if (targetVolume.weight <= 0f)
        {
            targetVolume.weight = 1f;
            Debug.Log("[VolumeManager] Target volume weight was 0, set to 1.");
        }
    }

    private void StartGlitch()
    {
        _isGlitching = true;

        // 持续时间：优先使用固定时长，或根据强度插值
        float duration = useFixedDuration ? fixedGlitchDuration : Mathf.Lerp(minDuration, maxDuration, _targetIntensity);
        _glitchEndTime = Time.time + duration;

        // 冷却时间：反抗值越高，冷却越短（触发越频繁）
        // 降低强度对冷却的影响，避免连续过于频繁
        float cooldown = Mathf.Lerp(maxCooldown, minCooldown, _targetIntensity * 0.5f);
        _cooldownEndTime = _glitchEndTime + cooldown;

        // 初始跳动：从0跳到目标强度
        float randomOffset = _targetIntensity * randomOffsetRatio * Random.Range(-1f, 1f);
        _currentIntensity = Mathf.Clamp01(_targetIntensity + randomOffset);
        ApplyGlitch();
    }

    private void EndGlitch()
    {
        _isGlitching = false;
        _currentIntensity = 0f;
        ApplyGlitch();
    }

    private void ApplyGlitch()
    {
        if (_digitalGlitch != null)
        {
            _digitalGlitch.intensity.overrideState = true;
            _digitalGlitch.intensity.value = _currentIntensity;
        }
        else
        {
            Debug.LogWarning("[VolumeManager] _digitalGlitch is null in ApplyGlitch.");
        }

        if (_analogGlitch != null)
        {
            float analogIntensity = _currentIntensity * analogGlitchScale;
            _analogGlitch.scanLineJitter.overrideState = true;
            _analogGlitch.scanLineJitter.value = analogIntensity;
            _analogGlitch.verticalJump.overrideState = true;
            _analogGlitch.verticalJump.value = analogIntensity * 0.6f;
            _analogGlitch.horizontalShake.overrideState = true;
            _analogGlitch.horizontalShake.value = analogIntensity * 0.4f;
            _analogGlitch.colorDrift.overrideState = true;
            _analogGlitch.colorDrift.value = analogIntensity * 0.5f;
        }
        else
        {
            Debug.LogWarning("[VolumeManager] _analogGlitch is null in ApplyGlitch.");
        }
    }

    public void ResetGlitch()
    {
        _isGlitching = false;
        _currentIntensity = 0f;
        _targetIntensity = 0f;
        _glitchEndTime = 0f;
        _cooldownEndTime = 0f;

        if (_digitalGlitch != null)
        {
            _digitalGlitch.intensity.overrideState = true;
            _digitalGlitch.intensity.value = 0f;
        }

        if (_analogGlitch != null)
        {
            _analogGlitch.scanLineJitter.overrideState = true;
            _analogGlitch.scanLineJitter.value = 0f;
            _analogGlitch.verticalJump.overrideState = true;
            _analogGlitch.verticalJump.value = 0f;
            _analogGlitch.horizontalShake.overrideState = true;
            _analogGlitch.horizontalShake.value = 0f;
            _analogGlitch.colorDrift.overrideState = true;
            _analogGlitch.colorDrift.value = 0f;
        }
    }

    #region Pause Volume Animation

    /// <summary>
    /// 渐入暂停 Volume（weight 从 0 -> 1）
    /// </summary>
    public void FadeInPauseVolume()
    {
        KillPauseTween();
        if (pauseVolume == null) return;

        pauseVolume.weight = 0f;
        _pauseTween = DOTween.To(() => pauseVolume.weight, x => pauseVolume.weight = x, 1f, pauseFadeDuration)
            .SetEase(pauseFadeEase)
            .SetUpdate(true); // 忽略 Time.timeScale
    }

    /// <summary>
    /// 渐出暂停 Volume（weight 从 1 -> 0）
    /// </summary>
    public void FadeOutPauseVolume()
    {
        KillPauseTween();
        if (pauseVolume == null) return;

        pauseVolume.weight = 1f;
        _pauseTween = DOTween.To(() => pauseVolume.weight, x => pauseVolume.weight = x, 0f, pauseFadeDuration)
            .SetEase(pauseFadeEase)
            .SetUpdate(true); // 忽略 Time.timeScale
    }

    /// <summary>
    /// 立即将 pauseVolume.weight 设为指定值，并停止当前动画
    /// </summary>
    public void SetPauseVolumeWeight(float weight)
    {
        KillPauseTween();
        if (pauseVolume != null)
            pauseVolume.weight = Mathf.Clamp01(weight);
    }

    private void KillPauseTween()
    {
        if (_pauseTween != null && _pauseTween.IsActive())
        {
            _pauseTween.Kill();
            _pauseTween = null;
        }
    }

    #endregion

    private void TryGetComponents()
    {
        if (globalVolume == null)
            globalVolume = GetComponent<Volume>();

        Volume targetVolume = glitchVolume != null ? glitchVolume : globalVolume;
        if (targetVolume == null)
        {
            Debug.LogWarning("[VolumeManager] TryGetComponents could not find a valid Volume component.");
            return;
        }

        if (targetVolume.profile == null)
        {
            Debug.LogWarning($"[VolumeManager] Target volume '{targetVolume.name}' has no profile assigned.");
            return;
        }

        targetVolume.profile.TryGet(out _digitalGlitch);
        targetVolume.profile.TryGet(out _analogGlitch);

        Debug.LogFormat("[VolumeManager] TryGetComponents: targetVolume={0}, digitalGlitch={1}, analogGlitch={2}",
            targetVolume.name,
            _digitalGlitch != null ? _digitalGlitch.name : "null",
            _analogGlitch != null ? _analogGlitch.name : "null");
    }
}

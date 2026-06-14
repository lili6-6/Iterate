using UnityEngine;
using MoreMountains.CorgiEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace pp
{
    /// <summary>
    /// 脚步声系统脚本
    /// 放在玩家身上，检测脚下接触指定Layer时播放随机脚步声
    /// 玩家停止运动时自动停止音效
    /// </summary>
    public class FootstepSoundSystem : CorgiMonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("脚步声音源列表")]
        public List<AudioSource> FootstepAudioSources = new List<AudioSource>();

        [Header("Layer Detection")]
        [Tooltip("脚下需要检测的Layer（例如地面Layer）")]
        public LayerMask GroundLayer;

        [Header("Movement Detection")]
        [Tooltip("判断为停止运动的速度阈值")]
        public float StopThreshold = 0.1f;

        [Tooltip("检查速度的时间间隔")]
        public float SpeedCheckInterval = 0.1f;

        [Header("Playback Settings")]
        [Tooltip("播放脚步声的时间间隔（秒）")]
        public float FootstepInterval = 0.4f;

        [Tooltip("脚步声音量")]
        [Range(0f, 1f)]
        public float AudioVolume = 1f;

        [Header("Debug")]
        [Tooltip("开启调试日志，便于查看触发和运动状态")]
        public bool DebugLogs = true;

        [Header("Pitch Settings")]
        [Tooltip("根据地表/运动速度调整音效音高")]
        public float PitchMultiplier = 1f;

        [Tooltip("最小播放音高")]
        public float MinPitch = 0.8f;

        [Tooltip("最大播放音高")]
        public float MaxPitch = 1.5f;

        private float _lastFootstepTime;
        private float _lastSpeedCheckTime;
        private Rigidbody2D _rigidbody2D;
        private CorgiController _corgiController;
        private float _originSpeedFactor = 1f;
        private Vector3 _lastPosition;
        private bool _isMoving;
        private bool _isGrounded;
        private int _groundContactCount;
        private AudioSource _currentAudioSource;
        private Tween _fadeTween;

        private void Start()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _corgiController = GetComponent<CorgiController>();

            if (_corgiController != null)
            {
                _originSpeedFactor = _corgiController.DefaultParameters.SpeedFactor;
            }

            foreach (var audioSource in FootstepAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.volume = AudioVolume;
                    audioSource.playOnAwake = false;
                    audioSource.loop = false;
                }
            }

            _lastPosition = transform.position;
            _lastFootstepTime = -FootstepInterval;
            _lastSpeedCheckTime = 0f;
            _groundContactCount = 0;
            _isGrounded = false;

            if (DebugLogs)
            {
                Debug.Log($"FootstepSoundSystem initialized. Rigidbody2D present: {_rigidbody2D != null}, CorgiController present: {_corgiController != null}, GroundLayer: {GroundLayer.value}", gameObject);
            }
        }

        private void Update()
        {
            _lastSpeedCheckTime += Time.deltaTime;
            if (_lastSpeedCheckTime >= SpeedCheckInterval)
            {
                UpdateMovementState();
                _lastSpeedCheckTime = 0f;
            }

            if (_isMoving && _isGrounded)
            {
                if (_currentAudioSource == null || !_currentAudioSource.isPlaying)
                {
                    RandomPlay();
                }
                else
                {
                    UpdateAudioPitch();
                }
            }
            else
            {
                StopAudioSmooth();
            }
        }

        private void UpdateMovementState()
        {
            float currentSpeed = 0f;
            if (_corgiController != null)
            {
                currentSpeed = Mathf.Abs(_corgiController.Speed.x);
            }
            else if (_rigidbody2D != null)
            {
                currentSpeed = _rigidbody2D.linearVelocity.magnitude;
            }
            else
            {
                Vector3 displacement = transform.position - _lastPosition;
                currentSpeed = displacement.magnitude / SpeedCheckInterval;
                _lastPosition = transform.position;
            }

            bool previousMoving = _isMoving;
            bool previousGrounded = _isGrounded;

            _isMoving = currentSpeed > StopThreshold;
            _isGrounded = _groundContactCount > 0;

            if (DebugLogs && (previousMoving != _isMoving || previousGrounded != _isGrounded))
            {
                Debug.Log($"FootstepSoundSystem state: moving={_isMoving} grounded={_isGrounded} speed={currentSpeed:F2} contactCount={_groundContactCount}", gameObject);
            }
        }

        private bool IsGroundLayer(int layer)
        {
            return (GroundLayer.value & (1 << layer)) != 0;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsGroundLayer(other.gameObject.layer))
            {
                _groundContactCount++;
                _isGrounded = true;
                Debug.Log($"FootstepSoundSystem OnTriggerEnter2D: {other.name} layer {other.gameObject.layer}, count {_groundContactCount}", gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsGroundLayer(other.gameObject.layer))
            {
                _groundContactCount = Mathf.Max(_groundContactCount - 1, 0);
                _isGrounded = _groundContactCount > 0;
                if (DebugLogs)
                {
                    Debug.Log($"FootstepSoundSystem OnTriggerExit2D: {other.name} layer {other.gameObject.layer}, count {_groundContactCount}", gameObject);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (IsGroundLayer(other.gameObject.layer))
            {
                _isGrounded = true;
                if (DebugLogs)
                {
                    Debug.Log($"FootstepSoundSystem OnTriggerStay2D: {other.name} layer {other.gameObject.layer}, grounded={_isGrounded}", gameObject);
                }
            }
        }

        /// <summary>
        /// 播放随机脚步声
        /// </summary>
        private void RandomPlay()
        {
            if (FootstepAudioSources.Count == 0)
            {
                Debug.LogWarning("FootstepSoundSystem: 没有可用的AudioSource!", gameObject);
                return;
            }

            int randomIndex = Random.Range(0, FootstepAudioSources.Count);
            AudioSource selectedSource = FootstepAudioSources[randomIndex];

            if (selectedSource == null)
            {
                Debug.LogWarning("FootstepSoundSystem: 随机选择的 AudioSource 为 null", gameObject);
                return;
            }

            if (selectedSource.clip == null)
            {
                Debug.LogWarning($"FootstepSoundSystem: AudioSource '{selectedSource.name}' 没有音频片段", gameObject);
                return;
            }

            _currentAudioSource = selectedSource;
            _currentAudioSource.loop = true;
            _currentAudioSource.volume = AudioVolume;
            _currentAudioSource.pitch = 1f;
            _currentAudioSource.Play();
            UpdateAudioPitch();

            if (DebugLogs)
            {
                Debug.Log($"FootstepSoundSystem: 开始播放脚步声 {selectedSource.clip.name}", gameObject);
            }
        }

        private void UpdateAudioPitch()
        {
            if (_currentAudioSource == null || !_currentAudioSource.isPlaying)
                return;

            float targetPitch = 1f;
            if (_corgiController != null)
            {
                float currentFactor = _corgiController.DefaultParameters.SpeedFactor;
                float relativeSpeed = _originSpeedFactor > 0f ? currentFactor / _originSpeedFactor : 1f;
                targetPitch = Mathf.Clamp(relativeSpeed * PitchMultiplier, MinPitch, MaxPitch);
            }
            else if (_rigidbody2D != null)
            {
                targetPitch = Mathf.Clamp(_rigidbody2D.linearVelocity.magnitude * PitchMultiplier, MinPitch, MaxPitch);
            }

            _currentAudioSource.pitch = targetPitch;
        }

        private void StopAudioSmooth()
        {
            if (_currentAudioSource == null || !_currentAudioSource.isPlaying)
                return;

            _fadeTween?.Kill();
            _fadeTween = DOTween.To(
                () => _currentAudioSource.volume,
                v => _currentAudioSource.volume = v,
                0f,
                0.2f
            ).OnComplete(() =>
            {
                if (_currentAudioSource != null)
                {
                    _currentAudioSource.Stop();
                    _currentAudioSource.volume = AudioVolume;
                    _currentAudioSource = null;
                }
            });
        }

        /// <summary>
        /// 停止所有脚步声
        /// </summary>
        private void StopFootsteps()
        {
            StopAudioSmooth();
        }

        /// <summary>
        /// 外部接口：强制停止脚步声
        /// </summary>
        public void ForceStop()
        {
            StopFootsteps();
            _isMoving = false;
        }

        /// <summary>
        /// 外部接口：获取当前是否在播放脚步声
        /// </summary>
        public bool IsPlayingFootsteps()
        {
            return _isMoving && _isGrounded;
        }

        private void OnDestroy()
        {
            StopFootsteps();
        }
    }
}

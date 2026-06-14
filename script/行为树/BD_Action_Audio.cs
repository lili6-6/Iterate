using UnityEngine;
using DG.Tweening;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using TweenSetting = PP.BehaviorTree.TweenSetting;

namespace PP
{
  

[TaskCategory("Custom/Audio")]
[TaskDescription("执行音乐音效相关行为（通用独立版）")]
public class BD_Action_Audio : Action
{
    public enum ACTION_NAME
    {
        NULL,
        PLAY_AUDIO,
        STOP_AUDIO,
        PAUSE_AUDIO,
        TWEEN_TRIGGER_VOLUME,
        TWEEN_BGM_VOLUME,
        RESET_TRIGGER_VOLUME,
        RESET_BGM_VOLUME,
        PLAY_BGM,
        STOP_BGM
    }

    [Tooltip("选择要执行的音频行为")]
    public ACTION_NAME triggerAction;

    [Tooltip("音效播放源（AudioSource）")]
    public AudioSource audioSource;

    [Tooltip("BGM专用播放源")]
    public AudioSource bgmSource;

    [Tooltip("要播放的音频片段")]
    public AudioClip audioClip;

    [Tooltip("音量渐变目标值")]
    public float targetValue;

    [Tooltip("Resources 目录下的音频路径（可选）")]
    public string targetName;

    [Tooltip("DOTween 动画设置")]
    public TweenSetting tweenSetting = new TweenSetting();

    // 初始音量缓存
    private float? initialAudioVolume;
    private float? initialBgmVolume;

    public override void OnStart()
    {
        // 基础校验
        if (triggerAction == ACTION_NAME.NULL)
        {
            Debug.LogError($"{FriendlyName} 未选择任何行为！");
            return;
        }

        CacheInitialVolumes();
        CallAction();
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.Success;
    }

    private void CallAction()
    {
        switch (triggerAction)
        {
            case ACTION_NAME.PLAY_AUDIO:
                PlayAudio();
                break;

            case ACTION_NAME.STOP_AUDIO:
                if (audioSource != null) audioSource.Stop();
                break;

            case ACTION_NAME.PAUSE_AUDIO:
                if (audioSource != null) audioSource.Pause();
                break;

            case ACTION_NAME.TWEEN_TRIGGER_VOLUME:
                if (audioSource != null) TweenVolume(audioSource, targetValue);
                break;

            case ACTION_NAME.TWEEN_BGM_VOLUME:
                if (bgmSource != null) TweenVolume(bgmSource, targetValue);
                break;

            case ACTION_NAME.RESET_TRIGGER_VOLUME:
                if (audioSource != null && initialAudioVolume.HasValue)
                    TweenVolume(audioSource, initialAudioVolume.Value);
                break;

            case ACTION_NAME.RESET_BGM_VOLUME:
                if (bgmSource != null && initialBgmVolume.HasValue)
                    TweenVolume(bgmSource, initialBgmVolume.Value);
                break;

            case ACTION_NAME.PLAY_BGM:
                PlayBGM();
                break;

            case ACTION_NAME.STOP_BGM:
                if (bgmSource != null) bgmSource.Stop();
                break;
        }
    }

    private void PlayAudio()
    {
        if (audioSource == null)
        {
            Debug.LogWarning($"{FriendlyName} 未指定 AudioSource！");
            return;
        }

        // 优先使用指定的 AudioClip
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
            return;
        }

        // 从 Resources 加载
        if (!string.IsNullOrWhiteSpace(targetName))
        {
            AudioClip clip = Resources.Load<AudioClip>(targetName);
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning($"{FriendlyName} Resources 未找到音频：{targetName}");
            }
        }
        else
        {
            // 无配置时直接播放当前剪辑
            audioSource.Play();
        }
    }

    private void PlayBGM()
    {
        if (bgmSource == null)
        {
            Debug.LogWarning($"{FriendlyName} 未指定 BGM AudioSource！");
            return;
        }

        if (audioClip != null) bgmSource.clip = audioClip;
        bgmSource.Play();
    }

    private void CacheInitialVolumes()
    {
        if (audioSource != null) initialAudioVolume = audioSource.volume;
        if (bgmSource != null) initialBgmVolume = bgmSource.volume;
    }

    private void TweenVolume(AudioSource source, float toValue)
    {
        if (source == null) return;

        // 无动画直接设置
        if (tweenSetting == null || tweenSetting.Duration <= 0)
        {
            source.volume = toValue;
            return;
        }

        // DOTween 渐变
        DOTween.To(
                () => source.volume,
                value => source.volume = value,
                toValue,
                tweenSetting.Duration
            )
            .SetDelay(tweenSetting.Delay)
            .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
            .SetEase(tweenSetting.EaseType);
    }
}}
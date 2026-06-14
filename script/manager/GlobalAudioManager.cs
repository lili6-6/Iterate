using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace PP
{
public class GlobalAudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string uiVolumeParameter = "UiVolume";
    [SerializeField] private string sfxVolumeParameter = "SfxVolume";

    [Header("Volume (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float music = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float UI = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfx = 1f;

    private float _lastMusicVolume = -1f;
    private float _lastUiVolume = -1f;
    private float _lastSfxVolume = -1f;

    private void Start()
    {
        ResolveParameters();
        ApplyVolumes(force: true);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GlobalAudioManager] Scene loaded: {scene.name}, audioMixer: {audioMixer != null}, music={music}, UI={UI}, sfx={sfx}");
        ResolveParameters();
        ApplyVolumes(force: true);
    }

    private void Update()
    {
        ApplyVolumes(force: false);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        ResolveParameters();
        ApplyVolumes(force: true);
    }

    public void SetMusicVolume(float value)
    {
        music = Mathf.Clamp01(value);
        ApplyVolumes(force: true);
    }

    public void SetUiVolume(float value)
    {
        UI = Mathf.Clamp01(value);
        ApplyVolumes(force: true);
    }

    public void SetSfxVolume(float value)
    {
        sfx = Mathf.Clamp01(value);
        ApplyVolumes(force: true);
    }

    public float GetMusicVolume() => music;
    public float GetUiVolume() => UI;
    public float GetSfxVolume() => sfx;

    private void ApplyVolumes(bool force)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[GlobalAudioManager] audioMixer is null, cannot apply volumes");
            return;
        }

        if (force || !Mathf.Approximately(music, _lastMusicVolume))
        {
            _lastMusicVolume = music;
            TrySetFloat(musicVolumeParameter, LinearToDb(music), "MusicVolume", "Music");
        }

        if (force || !Mathf.Approximately(UI, _lastUiVolume))
        {
            _lastUiVolume = UI;
            TrySetFloat(uiVolumeParameter, LinearToDb(UI), "UiVolume", "UI");
        }

        if (force || !Mathf.Approximately(sfx, _lastSfxVolume))
        {
            _lastSfxVolume = sfx;
            TrySetFloat(sfxVolumeParameter, LinearToDb(sfx), "SfxVolume", "SFX", "Sfx");
        }
    }

    private void ResolveParameters()
    {
        if (audioMixer == null) return;
        musicVolumeParameter = ResolveParameterName(musicVolumeParameter, "MusicVolume", "Music");
        uiVolumeParameter = ResolveParameterName(uiVolumeParameter, "UiVolume", "UI");
        sfxVolumeParameter = ResolveParameterName(sfxVolumeParameter, "SfxVolume", "SFX", "Sfx");
    }

    private string ResolveParameterName(string current, params string[] candidates)
    {
        if (audioMixer == null) return current;
        if (HasParameter(current)) return current;
        foreach (string candidate in candidates)
        {
            if (HasParameter(candidate)) return candidate;
        }
        return current;
    }

    private bool HasParameter(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return audioMixer.GetFloat(name, out _);
    }

    private void TrySetFloat(string name, float value, params string[] fallbacks)
    {
        if (HasParameter(name))
        {
            audioMixer.SetFloat(name, value);
            Debug.Log($"[GlobalAudioManager] Set {name} to {value} dB");
            return;
        }
        foreach (string fallback in fallbacks)
        {
            if (HasParameter(fallback))
            {
                audioMixer.SetFloat(fallback, value);
                Debug.Log($"[GlobalAudioManager] Set {fallback} to {value} dB (fallback)");
                return;
            }
        }
        Debug.LogWarning("AudioMixer exposed parameter not found: " + name);
    }

    private float LinearToDb(float value)
    {
        if (value <= 0.0001f) return -80f;
        return Mathf.Log10(value) * 20f;
    }
}
}

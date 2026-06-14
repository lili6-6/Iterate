using PP;
using UnityEngine;
using System.Collections;

namespace PP
{
public class SetManager : MonoBehaviour
{
    private GlobalAudioManager AudioManager;
    [SerializeField] private Michsky.MUIP.SliderManager musicSlider;
    [SerializeField] private Michsky.MUIP.SliderManager uiSlider;
    [SerializeField] private Michsky.MUIP.SliderManager sfxSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager = GlobalManager.Instance.globalAudioManager;
        InitializeSliders();
    }

    private void InitializeSliders()
    {
        if (AudioManager == null) return;

        if (musicSlider != null && musicSlider.mainSlider != null)
        {
            musicSlider.mainSlider.value = AudioManager.GetMusicVolume();
        }
        if (uiSlider != null && uiSlider.mainSlider != null)
        {
            uiSlider.mainSlider.value = AudioManager.GetUiVolume();
        }
        if (sfxSlider != null && sfxSlider.mainSlider != null)
        {
            sfxSlider.mainSlider.value = AudioManager.GetSfxVolume();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void setMusicVolume(float value)
    {
        if (AudioManager == null) {
            
            return;
        }
        AudioManager.SetMusicVolume(value);
    }
    public void setUIVolume(float value)
    {
        if (AudioManager == null) {
            return;
        }

        AudioManager.SetUiVolume(value);
    }
    public void setSfxVolume(float value)
    {
        if (AudioManager == null) {
            return;
        }

        AudioManager.SetSfxVolume(value);
    }
}}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")] public Slider sfxSlider; 
    public Slider musicSlider;

    [Header("Labels (Opcionales)")] public TextMeshProUGUI sfxLabel; 
    public TextMeshProUGUI musicLabel;

    [Header("Formato Label")] public string percentageFormat = "{0}%"; // 0 = valor porcentaje redondeado

    private void OnEnable()
    {
        if (SoundManager.instance != null)
        {
            // Inicializar sin disparar callbacks
            if (sfxSlider)
            {
                sfxSlider.SetValueWithoutNotify(SoundManager.instance.SfxVolume);
                UpdateSfxLabel(SoundManager.instance.SfxVolume);
            }
            if (musicSlider)
            {
                musicSlider.SetValueWithoutNotify(SoundManager.instance.MusicVolume);
                UpdateMusicLabel(SoundManager.instance.MusicVolume);
            }
        }
        SoundManager.OnSfxVolumeChanged += HandleSfxChanged;
        SoundManager.OnMusicVolumeChanged += HandleMusicChanged;
    }

    private void OnDisable()
    {
        SoundManager.OnSfxVolumeChanged -= HandleSfxChanged;
        SoundManager.OnMusicVolumeChanged -= HandleMusicChanged;
    }

    private void HandleSfxChanged(float v)
    {
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(v);
        UpdateSfxLabel(v);
    }

    private void HandleMusicChanged(float v)
    {
        if (musicSlider) musicSlider.SetValueWithoutNotify(v);
        UpdateMusicLabel(v);
    }

    public void OnSfxSliderChanged(float v)
    {
        if (SoundManager.instance != null) SoundManager.instance.SetSfxVolume(v);
    }

    public void OnMusicSliderChanged(float v)
    {
        if (SoundManager.instance != null) SoundManager.instance.SetMusicVolume(v);
    }

    private void UpdateSfxLabel(float v)
    {
        if (!sfxLabel) return; 
        int p = Mathf.RoundToInt(v * 100f);
        sfxLabel.text = string.Format(percentageFormat, p);
    }

    private void UpdateMusicLabel(float v)
    {
        if (!musicLabel) return; 
        int p = Mathf.RoundToInt(v * 100f);
        musicLabel.text = string.Format(percentageFormat, p);
    }
}

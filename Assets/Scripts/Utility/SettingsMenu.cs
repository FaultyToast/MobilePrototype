using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public float GetHorizontalSensitivity()
    {
        return Settings.horizontalSensitivity;
    }

    public void SetHorizontalSensitivity(float horizontalSensitivity)
    {
        Settings.horizontalSensitivity = horizontalSensitivity;
        PlayerPrefs.SetFloat(Settings.horizontalSensitivityKey, horizontalSensitivity);
    }

    public float GetVerticalSensitivity()
    {
        return Settings.verticalSensitivity;
    }

    public void SetVerticalSensitivity(float value)
    {
        Settings.verticalSensitivity = value;
        PlayerPrefs.SetFloat(Settings.verticalSensitivityKey, value);
    }

    public bool GetInvertX()
    {
        return Settings.invertX;
    }

    public void SetInvertX(bool value)
    {
        Settings.invertX = value;
        Settings.SetBool(Settings.invertXKey, value);
    }

    public bool GetInvertY()
    {
        return Settings.invertY;
    }

    public void SetInvertY(bool value)
    {
        Settings.invertY = value;
        Settings.SetBool(Settings.invertYKey, value);
    }

    public bool GetTutorialsEnabled()
    {
        return Settings.tutorialsEnabled;
    }

    public void SetTutorialsEnabled(bool value)
    {
        Settings.tutorialsEnabled = value;
        Settings.SetBool(Settings.tutorialsEnabledKey, value);
    }

    public float GetMasterVolume()
    {
        return Settings.masterVolume;
    }

    public void SetMasterVolume(float value)
    {
        Settings.masterVolume = value;
        Settings.UpdateAudioSettings();
        PlayerPrefs.SetFloat(Settings.masterVolumeKey, value);
    }

    public float GetMusicVolume()
    {
        return Settings.musicVolume;
    }

    public void SetMusicVolume(float value)
    {
        Settings.musicVolume = value;
        Settings.UpdateAudioSettings();
        PlayerPrefs.SetFloat(Settings.musicVolumeKey, value);
    }

    public float GetSFXVolume()
    {
        return Settings.sfxVolume;
    }

    public void SetSFXVolume(float value)
    {
        Settings.sfxVolume = value;
        Settings.UpdateAudioSettings();
        PlayerPrefs.SetFloat(Settings.sfxVolumeKey, value);
    }
    public RectTransform layoutRoot;

    public MenuSlider horizontalSensitivitySlider;
    public MenuSlider verticalSensitivitySlider;
    public MenuSlider masterVolumeSlider;
    public MenuSlider musicVolumeSlider;
    public MenuSlider sfxVolumeSlider;
    public MenuTickBox invertXTickBox;
    public MenuTickBox invertYTickBox;
    public MenuTickBox enableTutorialsTickBox;

    public void Start()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }

    public void Awake()
    {
        horizontalSensitivitySlider.percent = GetHorizontalSensitivity();
        horizontalSensitivitySlider.OnValueChanged = SetHorizontalSensitivity;

        verticalSensitivitySlider.percent = GetVerticalSensitivity();
        verticalSensitivitySlider.OnValueChanged = SetVerticalSensitivity;

        masterVolumeSlider.percent = GetMasterVolume();
        masterVolumeSlider.OnValueChanged = SetMasterVolume;

        musicVolumeSlider.percent = GetMusicVolume();
        musicVolumeSlider.OnValueChanged = SetMusicVolume;

        sfxVolumeSlider.percent = GetSFXVolume();
        sfxVolumeSlider.OnValueChanged = SetSFXVolume;

        invertXTickBox.value = GetInvertX();
        invertXTickBox.OnValueChanged = SetInvertX;

        invertYTickBox.value = GetInvertY();
        invertYTickBox.OnValueChanged = SetInvertY;

        enableTutorialsTickBox.value = GetTutorialsEnabled();
        enableTutorialsTickBox.OnValueChanged = SetTutorialsEnabled;
    }

}

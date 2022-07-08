using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings
{
    public enum BoolSetting
    {
        InvertX,
        InvertY
    }

    public enum FloatSetting
    {
        HorizontalSensitivity,
        VerticalSensitivity,
        MasterVolume,
        MusicVolume,
        SFXVolume
    }

    public static bool allyDamageNumbersVisible;

    public static float horizontalSensitivity;
    public static string horizontalSensitivityKey = "HorizontalSensitivity";

    public static float verticalSensitivity;
    public static string verticalSensitivityKey = "VerticalSensitivity";

    public static bool invertX;
    public static string invertXKey = "InvertX";

    public static bool invertY;
    public static string invertYKey = "InvertY";

    public static float masterVolume;
    public static string masterVolumeKey = "MasterVolume";

    public static float musicVolume;
    public static string musicVolumeKey = "MusicVolume";

    public static float sfxVolume;
    public static string sfxVolumeKey = "SFXVolume";

    public static bool tutorialsEnabled;
    public static string tutorialsEnabledKey = "TutorialsEnabled";


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void GetSavedSettings()
    {
        horizontalSensitivity = PlayerPrefs.GetFloat(horizontalSensitivityKey, 50f);
        verticalSensitivity = PlayerPrefs.GetFloat(verticalSensitivityKey, 50f);
        invertX = GetBool(invertXKey, 0);
        invertY = GetBool(invertYKey, 0);
        masterVolume = PlayerPrefs.GetFloat(masterVolumeKey, 100f);
        musicVolume = PlayerPrefs.GetFloat(musicVolumeKey, 100f);
        sfxVolume = PlayerPrefs.GetFloat(sfxVolumeKey, 100f);
        tutorialsEnabled = GetBool(tutorialsEnabledKey, 1);

        UpdateAudioSettings();
    }

    public static bool GetBool(string key, int defaultValue)
    {
        return PlayerPrefs.GetInt(key, defaultValue) == 1;
    }

    public static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    public static void UpdateAudioSettings()
    {
        AudioCaller.instance.audioManagerData.musicVolume = musicVolume;
        AudioCaller.instance.audioManagerData.masterVolume = masterVolume;
        AudioCaller.instance.audioManagerData.soundFXVolume = sfxVolume;
    }
}

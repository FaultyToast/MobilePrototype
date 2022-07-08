using UnityEngine;
using UnityEditor;

/** 
 * Static Class
 * Script that detects if playmode is entered or exited.
 * Used to prevent Preview Objects delete correctly if play mode is entered before they are removed
 */
[InitializeOnLoad]
public static class PlayModeStateDetector
{
    static PlayModeStateDetector()
    {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state)
    {
        AudioManager am = Resources.Load<AudioManager>("AudioManager/AudioManagerData");
        am.RemoveAllPreviewObjects();
    }
}

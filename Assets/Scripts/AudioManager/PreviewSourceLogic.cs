using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Manages preview sound objects creates by Sound class buttons.
 * Detects if they are complete and flags for removal.
 * If Play Mode is entered or exited before these objects are properly removes PlayModeStateDetector Removes them correctly.
 */
[ExecuteAlways]
public class PreviewSourceLogic : MonoBehaviour
{
    public List<AudioSource> audioSource = new List<AudioSource>();/*!< Reference to AudioSource component*/
    public Sound soundRef;/*!< Reference to Sound component*/

    AudioManager audioManager;/*!< Reference to AudioManager Scriptable Object*/
    public bool ReadyForDestruction = false;/*!< Flag for destruction*/

    /** 
     * Start function. Sets AudioManager and AudioSource references. Sets AudioSource Clip, Volume, Pitch
     */
    private void Start()
    {
        audioManager = Resources.Load<AudioManager>("AudioManager/AudioManagerData");
        audioManager.soundObjects.Add(this);
        
        for(int i = 0; i < audioSource.Count; i++)
        {
            audioSource[i].clip = soundRef.audioClips[i].clip;
            audioSource[i].volume = soundRef.audioClips[i].volume * (1f + Random.Range(-soundRef.audioClips[i].volumeVariance / 2f, soundRef.audioClips[i].volumeVariance / 2f));
            audioSource[i].pitch = soundRef.audioClips[i].pitch * (1f + Random.Range(-soundRef.audioClips[i].pitchVariance / 2f, soundRef.audioClips[i].pitchVariance / 2f));
            audioSource[i].pitch = Mathf.Clamp(audioSource[i].pitch, 0.5f, 3f);
            audioSource[i].Play();
        }
        
    }

    /** 
     * Function to update the sound objects. Called by AudioManager ManagePreviewObjects() function which is called every tick in the AudioMangerWindow Update() function.
     * Updates the sound volume and loop of preview sounds when edited while playing.
     * Checks if the clip has finished playing and flags for removal by the AudioManager scriptable Object.
     */
    public void CheckSoundObjects()
    {   
        if (audioSource != null)
        {
            bool allComplete = true;
            for (int i = 0; i < audioSource.Count; i++)
            {
                audioSource[i].volume = soundRef.audioClips[i].volume;
                audioSource[i].volume *= (audioManager.GetVolumeGroup(soundRef.soundType)) / 100;
                audioSource[i].volume *= (audioManager.masterVolume) / 100;
                audioSource[i].loop = soundRef.audioClips[i].loop;

                //Check if atleast 1 sound is still playing
                if (audioSource[i].isPlaying)
                {
                    allComplete = false;
                }
                
            }
            if (allComplete)
            {
                ReadyForDestruction = true;
            }
        }
    }

    public void Update()
    {
        bool allComplete = true;
        for (int i = 0; i < audioSource.Count; i++)
        {
            audioSource[i].volume = soundRef.audioClips[i].volume;
            audioSource[i].volume *= (audioManager.GetVolumeGroup(soundRef.soundType)) / 100;
            audioSource[i].volume *= (audioManager.masterVolume) / 100;

            audioSource[i].loop = soundRef.audioClips[i].loop;

            //Check if atleast 1 sound is still playing
            if (audioSource[i].isPlaying)
            {
                allComplete = false;
            }

        }
        if (allComplete)
        {
            DestroyObject();
        }
    }

    /** 
     * Function called by the AudioManager scriptable object that destroys the objects in editor.
     * Removes Object from AudioManager soundObjects list.
     */
    public void DestroyObject()
    {
        audioManager.soundObjects.Remove(this);
        if (gameObject != null)
        {
            DestroyImmediate(gameObject);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

/** 
 * Monobehaviour that is used on Pooled objects. Position and Sound data is set by AudioCaller class.
 * Checks if the sound clip has finished playing.
 * Updates the AudioSource volume based on Sound SoundType variable and master volume sliders contained in AudioManager scripable object instance.
 */
public class SoundSourceLogic : MonoBehaviour
{
    List<AudioSource> source = new List<AudioSource>();/*!< Reference to AudioSource component*/
    [System.NonSerialized]
    public Sound sound;/*!< Reference to Sound Object set by AudioCaller Play() function*/

    List<float> baseVolume = new List<float>();/*!< Keeps track of the initial volume set with variance*/

    [System.NonSerialized]
    public bool inPool = false; /*Is this sound in a pool*/

    [System.NonSerialized]
    public bool active = true; /*Should this sound be playing? used for detecting when to stop a music fade out*/

    [System.NonSerialized]
    public float fadeMultiplier = 1f; /*Multiplier for music fading*/

    /**
     * Function to set the AudioSource component variables based on Sound object
     * Assigns Clip, Volume, Volume Variance, Pitch, Pitch Variance, Spatial Blend, Looping.
     * Plays the Sound Once complete
     */
    public void SetAudioData()
    {
        for (int i = 0; i < sound.audioClips.Length; i++)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.clip = sound.audioClips[i].clip;
            audioSource.pitch = sound.audioClips[i].pitch * (1f + Random.Range(-sound.audioClips[i].pitchVariance / 2f, sound.audioClips[i].pitchVariance / 2f));
            audioSource.volume = sound.audioClips[i].volume * (1f + Random.Range(-sound.audioClips[i].volumeVariance / 2f, sound.audioClips[i].volumeVariance / 2f));
            baseVolume.Add(audioSource.volume);
            audioSource.spatialBlend = sound.spatialBlend;
            audioSource.loop = sound.audioClips[i].loop;
            audioSource.maxDistance = sound.MaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            source.Add(audioSource);
            UpdateVolume(source.Count - 1);

            audioSource.Play();
        }
    }

    public void UpdateAllVolumes()
    {
        for (int i = 0; i < source.Count; i++)
        {
            UpdateVolume(i);
        }

    }
    public void UpdateVolume(int index)
    {
        source[index].volume = baseVolume[index];
        source[index].volume *= (AudioCaller.instance.audioManagerData.GetVolumeGroup(sound.soundType)) / 100f;
        source[index].volume *= (AudioCaller.instance.audioManagerData.masterVolume) / 100f;
        source[index].volume *= fadeMultiplier;
    }

        /**
         * Update function to check if the AudioSource clip has finished playing.
         * If the clip is complete return it to the ObjectPooler by calling SoundComplete() function in AudioCaller.
         * Updates the volume of the AudioSource to match the AudioManager data instances volume sliders based on the Sound soundType variable.
         */
    private void Update()
    {
        bool allComplete = true;
        if (sound != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                UpdateVolume(i);
                //Check if atleast 1 sound is still playing
                if (source[i].isPlaying)
                {
                    allComplete = false;
                }
            }
        }

        if (allComplete)
        {
            FlagSourcesForDestruction();
            
        }
    }

    public void FlagSourcesForDestruction()
    {

        sound = null;
        //Remove All added sound Components
        for (int i = 0; i < source.Count; i++)
        {
            Destroy(source[i]);
        }
        AudioCaller.instance.SoundComplete(this);
        source.Clear();
        baseVolume.Clear();

        //Position Contraint
        PositionConstraint c = gameObject.GetComponent<PositionConstraint>();
        if (c != null)
        {
            c.constraintActive = false;
            for (int i = 0; i < c.sourceCount; i++)
            {
                c.RemoveSource(0);
            }
        }
    }

    public void AlterBaseVolume(float value)
    {
        for (int i = 0; i < source.Count; i++)
        {
            source[i].volume = value;
        }
    }
}

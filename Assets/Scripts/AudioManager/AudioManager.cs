using UnityEngine.Audio;
using System;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
[CreateAssetMenu(fileName = "AudioManagerData", menuName = "ScriptableObjects/AudioManagerData", order = 1)]
/** 
 * Scriptable class that creates AudioManagerData objects that contain data used by the AudioManagerWindow editor window.
 * Used hold a list of Sounds to be edited in the AudioMangerWindow editor window.
 */
public class AudioManager : ScriptableObject
{
	[Range(0, 100)] public float masterVolume;/*!< Master volume slider within a range of 0 - 100. Allows the overall volume of sounds to be effected. Public to be altered through a UI menu*/
	[Range(0, 100)] public float musicVolume;/*!< MusicVolume volume slider within a range of 0 - 100. Allows all Sound instances of SoundType.Music volume to be changed. Public to be altered through a UI menu*/
	[Range(0, 100)] public float soundFXVolume;/*!< Sound volume slider within a range of 0 - 100. Allows all Sound instances of SoundType.SoundFX volume to be changed. Public to be altered through a UI menu*/
	[Range(0, 100)] public float voiceVolume;/*!< Voice volume slider within a range of 0 - 100. Allows all Sound instances of SoundType.Voice volume to be changed. Public to be altered through a UI menu*/

	public AudioMixerGroup mixerGroup;/*!< Allows AudioSources to be mixed*/

	public Sound[] sounds;/*!< List of Serializable Sound instances to be edited by AudioManagerWindow by the user. */

	public Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();

	[System.NonSerialized]
	public List<PreviewSourceLogic> soundObjects = new List<PreviewSourceLogic>();/*!< List of preview sound objects to be managed in edit mode */

	/** 
	 * Function to get sound by name, used in AudioCaller Play() functions to locate the correct sound by string.
	 * If no sound can be found return a warning to the console
	 */
	public Sound GetSound(string sound)
	{
		if (soundDictionary.Count != sounds.Length)
        {
			CreateSoundDictionary();
        }

		Sound s = null;
		soundDictionary.TryGetValue(sound, out s);
		if (s == null)
		{
			Debug.LogWarning("Sound: " + sound + " not found!");
			return null;
		}

		return s;
	}

	public void CreateSoundDictionary()
    {
		soundDictionary.Clear();
		for (int i = 0; i < sounds.Length; i++)
        {
			soundDictionary.Add(sounds[i].soundName, sounds[i]);
        }
    }

	/**
	 * Function called by AudioManagerEditor Update() to update preview objects PreviewSourceLogic. Updates and flags objects for deletion when complete.
	 */
	public void ManagePreviewObjects()
	{
		List<PreviewSourceLogic> soundObjectsToDestroy = new List<PreviewSourceLogic>();

		foreach (PreviewSourceLogic so in soundObjects)
		{
			so.CheckSoundObjects();
			if (so.ReadyForDestruction)
			{
				soundObjectsToDestroy.Add(so);
			}
		}
		foreach (PreviewSourceLogic so in soundObjectsToDestroy)
		{
			so.DestroyObject();
		}
		soundObjectsToDestroy.Clear();
	}

	public void RemoveAllPreviewObjects()
	{
		for (int i = 0; i < soundObjects.Count; i++)
		{
			DestroyImmediate(soundObjects[i].gameObject);
		}
		soundObjects.Clear();
	}

	/**
	 * Function to get the relevant volume slider value relating to Sound soundType.
	 * Returns a Float
	 */
	public float GetVolumeGroup(Sound.SoundType type)
	{
		float volume = 0f;
		switch (type)
		{
			case Sound.SoundType.Music:
				volume = musicVolume;
				break;
			case Sound.SoundType.SoundFX:
				volume = soundFXVolume;
				break;
			case Sound.SoundType.Voice:
				volume = voiceVolume;
				break;
		}
		return volume;
	}
}

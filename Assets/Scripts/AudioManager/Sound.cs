using UnityEngine.Audio;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
/** Class that holds Serializable variables to be used in AudioManager scriptable object.
 * Holds important data for the user to edit in AudioManagerWindow editor window.
*/
public class Sound {

	/** SoundType Enum
	 * Defines Which Audio Volume Slider effects this sound 
	*/
	public enum SoundType
	{
		Music, /*!< Music Volume Slider*/
		SoundFX,/*!< SoundFX Volume Slider*/
		Voice,/*!< Voice Volume Slider*/
	}

	public string soundName; /*!< String that is used when calling AudioCaller Play()*/

	public AudioClipGroup[] audioClips;

	[Range(0, 1f)]
	public float spatialBlend = 0;/*!< Specifies if the Sound is 3D or not. A spatialBlend of 0 is 2D and can be heard reguadless of position. A spatialBlend of 1 is fully 3D. */

	[Range(0f, 2000f)]
	public float MaxDistance = 1500f;/*!< Defines the max distance a listener object can be before the sound is not heard. Also effects the volume falloff. Only matters is spatial blend is not 0*/

	public SoundType soundType = SoundType.Music;/*!< The type of sound the Sound is. This is used with Volume Sliders in AudioManager scriptable object*/

	public AudioMixerGroup mixerGroup;/*!< Allows AudioSources to be mixed*/

	List<GameObject> soundPreviewObjects = new List<GameObject>();/*!< Private list of GameObjects. Used to keep track of soundPreviewObjects*/

	/** Function that creates an object with an AudioSource component. Uses Sound variables to set values and plays a preview sound.
	 * Used by the AudioManagerWindow to preview audio through the Play button. 
	 * AudioManagerWindow volume sliders don't alter 
	*/
	public void PlayPreviewSound()
	{
		GameObject soundPreviewObject = new GameObject("SoundPreview");
		soundPreviewObjects.Add(soundPreviewObject);

		PreviewSourceLogic so = soundPreviewObject.AddComponent<PreviewSourceLogic>();

		for(int i = 0; i < audioClips.Length; i++)
		{
			AudioSource audioSource = soundPreviewObject.AddComponent<AudioSource>();
			so.audioSource.Add(audioSource);
			so.soundRef = this;
		}
	}

	/** Function that removes all previewSoundObjects that were created by the this Instance of Sound class.
	 * Used by the AudioManagerWindow to preview audio through the Stop button. 
	*/
	public void StopPreviewSound()
	{
		foreach (GameObject soundObj in soundPreviewObjects)
		{
			if(soundObj != null)
			{
				AudioSource[] audioSourceList = soundObj.GetComponents<AudioSource>();
				foreach(AudioSource source in audioSourceList)
				{
					source.Stop();
				}
			}
		}
		soundPreviewObjects.Clear();
	}
}

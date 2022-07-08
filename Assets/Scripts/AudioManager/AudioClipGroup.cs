using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioClipGroup
{
	public AudioClip clip; /*!< AudioClip that defines the sound to be played */

	[Range(0f, 1f)]
	public float volume = .75f;/*!< Base volume Of Audio Clip. */
	[Range(0f, 1f)]
	public float volumeVariance = .1f;/*!< Range the volume can randomly shift up or down to vary the sound between each instance. */

	[Range(0.5f, 3f)]
	public float pitch = 1f;/*!< Base pitch Of Audio Clip. */
	[Range(0f, 1f)]
	public float pitchVariance = .1f;/*!< Range the pitch can randomly shift up or down to vary the pitch between each instance. */

	public bool loop = false;/*!< Specified if the clip will loop or not */

}

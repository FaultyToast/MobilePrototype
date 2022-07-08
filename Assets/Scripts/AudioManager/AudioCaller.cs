using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;

/** 
 * Singleton Class
 * References AudioManager scriptable object reference to access Sound variable data. Creates Object Pool of SoundObjects to be used to play sounds.
 * Plays and Stops 2D or 3D sounds. Sounds can be childed to objects to allow the sound to move in worldspace.
 * Don't destroy on load to persists between scenes for performance and scene load times
 */
public class AudioCaller : MonoBehaviour
{
	private static AudioCaller _instance;/*!< Reference to the instance for Singleton*/

	/** 
    * Gets the instance of the object, if not found creates and sets a new instance.
    */
	public static AudioCaller instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate(Resources.Load("AudioManager/AudioCallerObject") as GameObject).GetComponent<AudioCaller>();
				DontDestroyOnLoad(_instance);
			}
			return _instance;
		}
	}

	public AudioManager audioManagerData;/*!< Reference to audioManagerData scriptable object*/
	GameObject poolObject;/*!< Reference to Object Pool GameObject*/
	ObjectPooler ObjectPool;/*!< Reference to Object Pool Script*/
	List<SoundSourceLogic> SoundObjectsActive = new List<SoundSourceLogic>();/*!< List of active sounds from Object Pool*/
	Dictionary<string, SoundSourceLogic> MusicObjects = new Dictionary<string, SoundSourceLogic>();
	private SoundSourceLogic currentMusic;

	/** 
    * Function called on awake. Checks if another instance exists and removes itself if one is found. 
    * Gets and Sets AudioManager data scriptable object.
	* Gets and Sets ObjectPooler object and script.
    */
	void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
		}
		else
		{
			_instance = this;
			DontDestroyOnLoad(gameObject);
		}

		audioManagerData = Resources.Load<AudioManager>("AudioManager/AudioManagerData");
		if (audioManagerData == null)
		{
			Debug.LogError("No instance of audio manager data. To create one open the Audio Manager window. Window->Audio Manager");
		}
		//audioManagerData.soundObjects.Clear();
		poolObject = ObjectPooler.instance.gameObject;
		if (poolObject == null)
		{
			Debug.LogError("Sound Pool prefab is missing from Resources");
		}
		ObjectPool = poolObject.GetComponent<ObjectPooler>();
	}

	/** 
    * Function to play a 2D sound. Takes in a string for the sound name set in AudioManagerWindow window. Only plays sound at postion 0,0,0. 
	* Should only used with a Sound instances with a spatialBlend of 0.
    */
	public SoundSourceLogic PlaySound(string soundName)
	{
		Sound s = audioManagerData.GetSound(soundName);
		if (s != null)
		{
			GameObject soundSourceObj = ObjectPool.SpawFromPool(Vector3.zero, Quaternion.identity);
			SoundSourceLogic soundLogic = soundSourceObj.GetComponent<SoundSourceLogic>();
			soundLogic.inPool = true;
			SoundObjectsActive.Add(soundLogic);

			soundLogic.sound = s;
			soundLogic.SetAudioData();

			return soundLogic;
		}
		return null;
	}

	public SoundSourceLogic PlayMusic(string soundName)
	{
		SoundSourceLogic existingMusic;
		if (MusicObjects.TryGetValue(soundName, out existingMusic))
        {
			existingMusic.active = true;
			return existingMusic;
        }
		Sound s = audioManagerData.GetSound(soundName);
		if (s != null)
		{
			//GameObject soundSourceObj = ObjectPool.SpawFromPool(Vector3.zero, Quaternion.identity);
			GameObject Music = new GameObject("Music Player: " + soundName);
			Music.transform.parent = gameObject.transform;
			Music.AddComponent<AudioSource>();
			SoundSourceLogic soundLogic = Music.AddComponent<SoundSourceLogic>();
			MusicObjects.TryAdd(soundName, soundLogic);
			currentMusic = soundLogic;

			soundLogic.sound = s;
			soundLogic.SetAudioData();

			return soundLogic;
		}

		return null;
	}

	/** 
    * Overload Function to play a 3D sound. Plays a sound. Takes in a string for the sound name set in AudioManagerWindow window. 
	* Plays a sound at a position. Should be used with a Sound with a spatialBlend greater than 1.
    */
	public SoundSourceLogic PlaySound(string soundName, Vector3 position)
	{
		Sound s = audioManagerData.GetSound(soundName);
		if (s != null)
		{
			GameObject soundSourceObj = ObjectPool.SpawFromPool(position, Quaternion.identity);
			SoundSourceLogic soundLogic = soundSourceObj.GetComponent<SoundSourceLogic>();
			soundLogic.inPool = true;
			SoundObjectsActive.Add(soundLogic);

			soundLogic.sound = s;
			soundLogic.SetAudioData();

			return soundLogic;
		}
		return null;
	}

	/** 
    * Overload Function to play a 3D sound. Plays a sound. Takes in a string for the sound name set in AudioManagerWindow window. 
	* Plays a sound at a position. Should be used with a Sound with a spatialBlend greater than 1.
	* Allows the sound to be parented to a gameobject this moving with it.
	* When stop is called on the object or it is complete it returns and parents to the object pool.
    */
	public SoundSourceLogic PlaySound(string soundName, Vector3 position, GameObject parent = null)
	{
		Sound s = audioManagerData.GetSound(soundName);
		if (s != null)
		{
			GameObject soundSourceObj = ObjectPool.SpawFromPool(position, Quaternion.identity);

			if (parent != null)
			{
				ConstraintSource source = new ConstraintSource();
				source.sourceTransform = parent.transform;
				source.weight = 1;
				PositionConstraint pc = soundSourceObj.GetComponent<PositionConstraint>();
				pc.AddSource(source);
				pc.constraintActive = true;
			}

			SoundSourceLogic soundLogic = soundSourceObj.GetComponent<SoundSourceLogic>();
			soundLogic.inPool = true;
			SoundObjectsActive.Add(soundLogic);

			soundLogic.sound = s;
			soundLogic.SetAudioData();

			return soundLogic;
		}
		return null;
	}

	/** 
    * Function to stop sounds. Primarily used for looping or long sounds
	* Finds sound by name and stops it, returning it to the object pool.
    */
	public void StopSound(string soundName)
	{
		List<SoundSourceLogic> soundObjectsToRemove = new List<SoundSourceLogic>();

		Sound s = audioManagerData.GetSound(soundName);
		foreach (SoundSourceLogic soundlogic in SoundObjectsActive)
		{
			if (soundlogic.sound == s)
			{
				soundObjectsToRemove.Add(soundlogic);
			}
		}

		foreach (SoundSourceLogic so in soundObjectsToRemove)
		{
			so.FlagSourcesForDestruction();
			SoundObjectsActive.Remove(so);
		}
		soundObjectsToRemove.Clear();
	}

	public void StopSound(SoundSourceLogic sourceObject)
	{
		if (sourceObject != null)
		{
			sourceObject.FlagSourcesForDestruction();
		}

		SoundObjectsActive.Remove(sourceObject);
	}

	public void StopMusic(string soundName)
	{
		SoundSourceLogic musicObject = MusicObjects[soundName];
		MusicObjects.Remove(soundName);
		Destroy(musicObject.gameObject);
		//MusicObjects.Clear();
	}

	/** 
    * Function to called by SoundSourceObject to flag it for returning to object pool once finished playing.
    */
	public void SoundComplete(SoundSourceLogic soundSourceObj)
	{
		if (soundSourceObj.inPool)
		{
			ObjectPool.ReturnToPool(soundSourceObj.gameObject);
		}
		else
		{
			Destroy(soundSourceObj.gameObject);
		}

		SoundObjectsActive.Remove(soundSourceObj.GetComponent<SoundSourceLogic>());
	}

	public void TransitionMusic(string trackToPlay, float duration)
	{

		if (currentMusic != null)
		{
			StartCoroutine(MusicFade(currentMusic, false, duration));
		}

		SoundSourceLogic newMusic = PlayMusic(trackToPlay);
		StartCoroutine(MusicFade(newMusic, true, duration));
		newMusic.UpdateAllVolumes();
	}

	IEnumerator MusicFade(SoundSourceLogic soundObject, bool fadeIn, float totalTime)
	{
		// Set the object as inactive to detect if it has been restarted later
		if (!fadeIn)
		{
			soundObject.active = false;
		}

		float timer = 0f;
		float min = fadeIn ? 0 : 1;
		float max = fadeIn ? 1 : 0;
		while (timer < totalTime)
		{
			// If the sound object has been reactivated stop this coroutine
			if (!fadeIn && soundObject.active)
			{
				yield break;
			}
			float lerp = Mathf.Lerp(min, max, timer / totalTime);
			soundObject.fadeMultiplier = lerp;
			timer += Time.deltaTime;
			yield return null;
		}

		if (!fadeIn)
		{
			StopMusic(soundObject.sound.soundName);
		}
	}

	public void StopAllMusic()
	{

		foreach (SoundSourceLogic soundlogic in MusicObjects.Values)
		{
			Destroy(soundlogic.gameObject);
		}
		MusicObjects.Clear();
	}
}

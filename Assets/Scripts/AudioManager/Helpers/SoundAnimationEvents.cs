using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundAnimationEvents : MonoBehaviour
{
    public string[] RandomSounds;
    public Transform overridePosition = null;
    public void PlayRandomSound()
    {
        int random = Random.Range(0, RandomSounds.Length);
        AudioCaller.instance.PlaySound(RandomSounds[random], transform.position, transform.gameObject);
    }

    public void PlaySoundOnModel(string soundName)
    {
        if(overridePosition == null)
            AudioCaller.instance.PlaySound(soundName, transform.position, transform.gameObject);
        else
            AudioCaller.instance.PlaySound(soundName, overridePosition.position, overridePosition.gameObject);
    }

    public void StopSoundOnModel(string soundName)
    {
        AudioCaller.instance.StopSound(soundName);
    }
}

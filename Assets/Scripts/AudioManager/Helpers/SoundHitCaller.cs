using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundHitCaller : MonoBehaviour
{
    public void TriggerSound(string soundname)
    {
        AudioCaller.instance.PlaySound(soundname, transform.position);
    }
}

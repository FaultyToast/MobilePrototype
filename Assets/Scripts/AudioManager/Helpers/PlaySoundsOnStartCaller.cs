using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundsOnStartCaller : MonoBehaviour
{
    public string[] SoundList;

    float age;
    public float delay;

    // Start is called before the first frame update
    void Start()
    {
        //if(delay == 0)
        //    foreach (string soundname in SoundList)
        //    {
        //        AudioCaller.instance.PlaySound(soundname, transform.position);
        //    }
    }

    // Update is called once per frame
    void Update()
    {
        age += Time.deltaTime;
        if(age > delay)
        {
            foreach (string soundname in SoundList)
            {
                AudioCaller.instance.PlaySound(soundname, transform.position);
            }
            Destroy(this);
        }
        
    }
}

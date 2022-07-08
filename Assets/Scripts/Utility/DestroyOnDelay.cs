using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnDelay : MonoBehaviour
{
    public float delay;
    public bool beginTimerOnAwake = true;
    private float timer;
    private bool activated = false;

    private void Awake()
    {
        activated = beginTimerOnAwake;
    }

    private void Start()
    {
        timer = delay;
    }

    private void FixedUpdate()
    {
        if (activated)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void Activate()
    {
        activated = true;
    }

}

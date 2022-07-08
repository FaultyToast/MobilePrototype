using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleDetacher : MonoBehaviour
{
    public List<ParticleSystem> systems;

    public void Detach()
    {
        foreach(ParticleSystem system in systems)
        {
            if (Application.isPlaying)
            {
                system.transform.SetParent(null, true);
                var destructor = system.gameObject.AddComponent<DestroyOnDelay>();
                switch (system.main.startLifetime.mode)
                {
                    case ParticleSystemCurveMode.Constant:
                        {
                            destructor.delay = system.main.startLifetime.constant;
                            break;
                        }
                    case ParticleSystemCurveMode.TwoConstants:
                        {
                            destructor.delay = system.main.startLifetime.constantMax;
                            break;
                        }
                    default:
                        {
                            destructor.delay = 5f;
                            break;
                        }
                }
                system.Stop();
                destructor.Activate();
            } 
        }
    }
}

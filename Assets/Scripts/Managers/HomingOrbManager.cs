using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HomingOrbManager
{
    public List<HomingOrb> orbs = new List<HomingOrb>();

    private static HomingOrbManager _instance = null;
    public static HomingOrbManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new HomingOrbManager();
            }
            return _instance;
        }
    }

    public void AddOrb(HomingOrb homingOrb)
    {
        // Initialize orb
        homingOrb.Initialize();
        homingOrb.SetArrivalTime(Time.time + homingOrb.travelTime);
        orbs.Add(homingOrb);
    }

    public void Update()
    {
        // Update every orb
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i].target == null || orbs[i].flaggedForRemoval)
            {
                orbs.RemoveAt(i);
                i--;
                continue;
            }

            if (orbs[i].arrivalTime <= Time.time)
            {
                orbs[i].OnArrival();
                orbs.RemoveAt(i);
                i--;
            }
        }
    }
}
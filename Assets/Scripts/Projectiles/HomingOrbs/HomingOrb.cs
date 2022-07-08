using System.Collections;
using UnityEngine;
using Mirror;

public class HomingOrb
{
    public float travelTime;
    public NetworkIdentity target;
    public Vector3 startPosition;
    public bool flaggedForRemoval = false;
    public NetworkIdentity owner;

    public float arrivalTime { get; private set; }

    public virtual void Initialize()
    {

    }

    public virtual void OnArrival()
    {

    }

    public void SetArrivalTime(float arrivalTime)
    {
        this.arrivalTime = arrivalTime;
    }
    
}
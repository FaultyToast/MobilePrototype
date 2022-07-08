using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerInterface : MonoBehaviour
{
    private void Awake()
    {
        enabled = false;
        NetworkClient.OnConnectedEvent += Connected;
    }

    public void Connected()
    {
        enabled = true;
        OnConnected();
    }

    public virtual void OnConnected()
    {

    }
}

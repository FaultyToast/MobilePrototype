using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LobbyPlayerDisplay : NetworkBehaviour
{

    public List<Cloth> clothSims;
    private Transform displayPoint;


    // Start is called before the first frame update
    public override void OnStartServer()
    {
        Debug.Log(Time.realtimeSinceStartup);
        if (LobbyManager.instance.availablePlayerDisplayPoints.Count > 0)
        {
            displayPoint = LobbyManager.instance.availablePlayerDisplayPoints[0];
            transform.position = displayPoint.position;
            transform.rotation = displayPoint.rotation;
            transform.localScale = displayPoint.localScale;
            LobbyManager.instance.availablePlayerDisplayPoints.RemoveAt(0);
        }
        GetComponent<NetworkTransform>().SetSyncVarDirtyBit(0U);
    }

    public void OnDestroy()
    {
        if (NetworkServer.active && displayPoint != null)
        {
            LobbyManager.instance.availablePlayerDisplayPoints.Insert(0, displayPoint);
        }
    }

    public void Start()
    {
        foreach (Cloth clothSim in clothSims)
        {
            clothSim.ClearTransformMotion();
        }
    }
}

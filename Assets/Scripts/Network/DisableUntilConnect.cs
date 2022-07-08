using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DisableUntilConnect : NetworkBehaviour
{
    private void Awake()
    {
        foreach(Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    public override void OnStartClient()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
        GameManager.instance.PostClientSetup();
    }
}

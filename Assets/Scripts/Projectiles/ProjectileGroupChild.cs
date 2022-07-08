using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ProjectileGroupChild : NetworkBehaviour
{
    [System.NonSerialized] public bool connectedToParent = true;
    [System.NonSerialized] public Transform parentTransform;

    public virtual void LateUpdate()
    {
        if (!FracturedUtility.HasEffectiveAuthority(netIdentity))
        {
            return;
        }

        if (connectedToParent && parentTransform != null)
        {
            transform.position = parentTransform.position;
            transform.rotation = parentTransform.rotation;
        }
    }
}

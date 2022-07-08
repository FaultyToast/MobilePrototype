using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ProjectileMaster : NetworkBehaviour, IAssetWithID
{
    public int assetID { get; set; }

    [SyncVar]
    [System.NonSerialized] public float genericFloat;

    private DamageComponent damageComponent;

    [SyncVar]
    [System.NonSerialized] public bool isLocal = false;

    [SyncVar]
    [System.NonSerialized] public string parentLocator;

    [SyncVar(hook = "OwnerUpdated")]
    [System.NonSerialized] public NetworkIdentity owner;

    [SyncVar(hook = "ParentUpdated")]
    [System.NonSerialized] public NetworkIdentity parentOverride;

    [SyncVar]
    [System.NonSerialized] public Vector3 localOffset;

    public bool allowDamageRollOnHit = false;

    public void Awake()
    {
        damageComponent = GetComponent<DamageComponent>();

        if (damageComponent != null && !allowDamageRollOnHit)
        {
            damageComponent.damageRollType = DamageComponent.DamageRollType.RollOnCreation;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (owner != null)
        {
            OwnerUpdated(null, owner);
            ParentUpdated(null, parentOverride);
        }
    }

    public void OwnerUpdated(NetworkIdentity oldValue, NetworkIdentity newValue)
    {
        if (newValue != null)
        {
            if (damageComponent != null && newValue != null)
            {
                damageComponent.SetOwner(newValue.GetComponent<CharacterMaster>());
            }

            if (parentOverride != null)
            {
                SetParent(parentOverride);
            }
            else
            {
                SetParent(newValue);
            }
        }
    }

    public void ParentUpdated(NetworkIdentity oldValue, NetworkIdentity newValue)
    {
        if (newValue != null)
        {
            SetParent(newValue);
        }
    }

    public void SetParent(NetworkIdentity parent)
    {
        if (!string.IsNullOrEmpty(parentLocator))
        {
            if (parentLocator == "ROOT")
            {
                transform.SetParent(parent.transform, !isLocal);
                if (isLocal)
                {
                    transform.localPosition = localOffset;
                }
            }
            else
            {
                transform.SetParent(parent.GetComponent<ChildLocator>().GetChild(parentLocator), !isLocal);
                if (isLocal)
                {
                    transform.localPosition = localOffset;
                }
            }
        }
    }
}
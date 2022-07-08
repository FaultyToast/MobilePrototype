using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;
using Mirror;

[Serializable]
public abstract class ActionState : ScriptableObject, IAssetWithID
{
    public int priority = 0;
    [System.NonSerialized] public int actionID = -1;

    [FormerlySerializedAs("minimumInterruptPriority")]
    [SerializeField] private int _minimumInterruptPriority = 0;

    public int minimumInterruptPriority
    {
        get
        {
            return _minimumInterruptPriority;
        }
        set
        {
            _minimumInterruptPriority = value;
            if (!stateEnded)
            {
                outer.EvaluateQueuedState();
            }
        }
    }

    [SerializeField] [HideInInspector] private int _assetID;
    public int assetID
    {
        get
        {
            return _assetID;
        }
        set
        {
            _assetID = value;
        }
    }

    protected bool isAuthority
    {
        get
        {
            return outer.isAuthority;
        }
    }

    protected bool isLocalPlayer
    {
        get
        {
            return outer.isLocalPlayer;
        }
    }

    [System.NonSerialized] public float fixedAge;
    [System.NonSerialized] public bool isOccupied = false;
    [System.NonSerialized] public ActionStateMachine outer;

    [System.NonSerialized] public Animator animator;
    [System.NonSerialized] public CharacterMovement characterMovement;
    [System.NonSerialized] public CharacterMaster characterMaster;
    [System.NonSerialized] public UnityEvent actionStarted = new UnityEvent();
    [System.NonSerialized] public UnityEvent actionEnded = new UnityEvent();
    [System.NonSerialized] public bool stateEnded = false;
    [System.NonSerialized] public StateSyncType stateSyncType = StateSyncType.SyncOriginal;
    [System.NonSerialized] public UnityAction<GameObject> projectileSpawnAction;

    // Determines how this state is synced to clients
    public enum StateSyncType
    {
        SyncClone, // Syncs a clone of the current state manually using Serialize and Deserialize
        SyncOriginal // Synces a clone of the original state in resources
    }

    public delegate void AnimationCallback();
    public Dictionary<string, AnimationCallback> animationCallback = new Dictionary<string, AnimationCallback>();

    protected float cachedAnimFrameRate;
    protected float cachedClipLength;
    protected float cachedAnimationSpeed;

    protected int cachedAnimationHash;
    protected int cachedAnimationLayer;
    [System.NonSerialized] public bool targetInterruptable = false;

    public static ActionState CreateState(string stateName)
    {
        return Instantiate(Resources.Load<ActionState>("ActionStates/Instances/" + stateName));
    }

    protected int GetAnimationFrame(bool useCachedFrameRate = true)
    {
        float frameFloat = GetAnimationFrameFloat(useCachedFrameRate);
        if (frameFloat < 0)
        {
            return -1;
        }
        return Mathf.CeilToInt(frameFloat);
    }

    protected float GetAnimationFrameFloat(bool useCachedFrameRate = true)
    {
        if (useCachedFrameRate)
        {
            return fixedAge * cachedAnimFrameRate;
        }
        else
        {
            AnimatorStateInfo stateInfo = outer.animator.GetNextAnimatorStateInfo(cachedAnimationLayer);
            if (stateInfo.fullPathHash != cachedAnimationHash)
            {
                stateInfo = outer.animator.GetCurrentAnimatorStateInfo(cachedAnimationLayer);
                if (stateInfo.fullPathHash != cachedAnimationHash)
                {
                    return -1;
                }
            }

            return stateInfo.normalizedTime * cachedClipLength * cachedAnimFrameRate;
        }
    }

    private UnityAction<int> RPCAction;
    public void RegisterRPC(UnityAction<int> action)
    {
        RPCAction = action;
    }

    protected float GetAnimationTimeNormalized()
    {
        AnimatorStateInfo stateInfo = outer.animator.GetNextAnimatorStateInfo(cachedAnimationLayer);
        if (stateInfo.fullPathHash != cachedAnimationHash)
        {
            stateInfo = outer.animator.GetCurrentAnimatorStateInfo(cachedAnimationLayer);
            if (stateInfo.fullPathHash != cachedAnimationHash)
            {
                return -1;
            }
        }

        return stateInfo.normalizedTime;
    }

    public void AssignCallbackListener(ActionStateAnimationEventCallback listener)
    {
        listener.actionStateMachine = outer;
        listener.outerObject = true;
        listener.targetState = this;
    }

    // Overload for simple projectile spawning
    public void SpawnProjectile(GameObject projectilePrefab, Vector3 spawnPosition, Quaternion spawnRotation, string parentTransformLocator = "", bool isLocal = false, NetworkIdentity ownerOverride = null)
    {
        SpawnProjectile(new ProjectileSpawnInfo(projectilePrefab, spawnPosition, spawnRotation, parentTransformLocator, isLocal, ownerOverride));
    }

    // Spawns and syncs projectile over network
    public void SpawnProjectile(ProjectileSpawnInfo spawnInfo)
    {
        if (spawnInfo.owner == null)
        {
            spawnInfo.owner = outer.netIdentity;
        }
        GameManager.instance.SpawnProjectile(spawnInfo);
    }

    // Overload for simple projectile spawning
    public GameObject ServerSpawnReferencedProjectile(GameObject projectilePrefab, Vector3 spawnPosition, Quaternion spawnRotation, string parentTransformLocator = "", bool isLocal = false, NetworkIdentity ownerOverride = null)
    {
        return ServerSpawnReferencedProjectile(new ProjectileSpawnInfo(projectilePrefab, spawnPosition, spawnRotation, parentTransformLocator, isLocal, ownerOverride));
    }

    public void SpawnReferencedProjectile(ProjectileSpawnInfo spawnInfo, UnityAction<GameObject> receiveProjectile)
    {
        outer.SpawnReferencedProjectile(spawnInfo, receiveProjectile, this);
    }

    // Same as SpawnProjectile but only usable on server, returns 
    public GameObject ServerSpawnReferencedProjectile(ProjectileSpawnInfo spawnInfo)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("Tried to use Server command on client");
        }
        if (spawnInfo.owner == null)
        {
            spawnInfo.owner = outer.netIdentity;
        }
        return GameManager.instance.ServerSpawnReferencedProjectile(spawnInfo);
    }

    public void PlayAnimationCrossFade(string layerName, string stateName, float crossFadeDuration, bool sync = false, float offset = 0f)
    {
        if (animator == null)
        {
            return;
        }
        PlayAnimationCrossFade(animator.GetLayerIndex(layerName), stateName, crossFadeDuration, sync, offset);
    }

    public void PlayAnimationCrossFade(int layer, string stateName, float crossFadeDuration, bool sync = false, float offset = 0f)
    {
        if (animator == null)
        {
            return;
        }

        if (layer < 0)
        {
            Debug.LogWarning("Layer Issues Here");
            Debug.LogError("HERE the issue is from " + outer.gameObject.name);
            return;
        }

        animator.CrossFadeInFixedTime(stateName, crossFadeDuration, layer, offset);

        animator.Update(0);



        AnimatorStateInfo stateInfo = outer.animator.GetNextAnimatorStateInfo(layer);
        AnimatorClipInfo[] clipInfos = animator.GetNextAnimatorClipInfo(layer);


        if (clipInfos.Length == 0)
        {
            Debug.Log("The animation " + stateName + "does not exist");
            return;
        }
        AnimatorClipInfo clipInfo = clipInfos[0];

        cachedAnimFrameRate = clipInfo.clip.frameRate * stateInfo.speed;
        cachedAnimationLayer = layer;
        cachedAnimationHash = stateInfo.fullPathHash;
        cachedClipLength = clipInfos[0].clip.length / stateInfo.speed;
        cachedAnimationSpeed = stateInfo.speed;

        if (sync)
        {
            if (!NetworkServer.active)
            {
                outer.CmdPlayAnimationSynced(layer, stateName, crossFadeDuration);
            }
            else
            {
                outer.RpcPlayAnimationSynced(layer, stateName, crossFadeDuration);
            }
        }
    }

    public void PlaySound(string soundName, Vector3 position, bool sync, GameObject parent = null)
    {
        outer.PlaySound(soundName, position, sync, parent);
    }

    public void ExitState()
    {
        if (!stateEnded)
        {
            outer.ExitState();
        }
    }

    public void InvokeRPC(int index)
    {
        RPCAction.Invoke(index);
    }

    public virtual void FixedUpdate()
    {

    }

    public virtual void LateUpdate()
    {

    }

    public virtual void Update()
    {

    }

    public virtual void OnEnter()
    {

    }

    // Run before onenter to prevent requirements for using base.OnEnter
    public virtual void BaseOnEnter()
    {

    }

    public virtual void OnExit()
    {

    }

    public virtual void Serialize(NetworkWriter writer)
    {

    }

    public virtual void Deserialize(NetworkReader reader)
    {

    }

    public virtual void PreSync()
    {

    }

    public virtual bool FulfilsAdditionalInterruptConditions(ActionState state)
    {
        return true;
    }

    public virtual bool FulfilsQueueingConditions(ActionState state)
    {
        return true;
    }
}

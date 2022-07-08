using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Mirror;
using UnityEngine.Events;

public class ActionStateMachine : NetworkBehaviour
{
    public ActionState currentState;
    public ActionState queuedState;
    private float queuedStateTimer;
    [System.NonSerialized] float stateQueueTime = 0.3f;
    private bool queuedThisFrame = false;
    private bool priorityQueueActive = false;
    private ActionState spawnProjectileActionState = null;
    private int spawnProjectileInstanceID = -1;

    [HideInInspector] public ActionState nextState;
    [HideInInspector] public SerializableEntityStateType mainStateType;
    [SerializeField] public string customName;

    [HideInInspector] public Animator animator;
    [HideInInspector] public CharacterMovement characterMovement;
    [HideInInspector] public CharacterMaster characterMaster;
    [HideInInspector] public InputBank inputBank;
    [HideInInspector] public ActionLocator actionLocator;

    public bool isAuthority
    {
        get
        {
            return FracturedUtility.HasEffectiveAuthority(characterMaster.networkIdentity);
        }
    }

    public bool isOccupied
    {
        get
        {
            return currentState.isOccupied;
        }
    }

    public void PriorityChanged()
    {
        EvaluateQueuedState();
    }

    public void OnDisable()
    {
        if (isAuthority && NetworkClient.active)
        {
            SetState(InstantiateState(mainStateType));
        }
    }

    public void EvaluateQueuedState()
    {
        if (queuedState != null)
        {
            if (CanInterruptState(queuedState, currentState))
            {
                ActionState nextState = queuedState;
                queuedState = null;
                SetState(nextState);
            }

            queuedStateTimer -= Time.deltaTime;
            if (queuedStateTimer <= 0)
            {
                queuedState = null;
            }
        }
    }

    // Start is called before the first frame update
    public void Awake()
    {
        mainStateType = new SerializableEntityStateType(typeof(Idle));
        animator = GetComponentInChildren<Animator>();
        characterMovement = GetComponent<CharacterMovement>();
        characterMaster = GetComponent<CharacterMaster>();
        inputBank = GetComponent<InputBank>();
        actionLocator = GetComponent<ActionLocator>();

        currentState = InstantiateState(mainStateType);
    }

    public void Update()
    {
        if (nextState != null)
        {
            SetState(nextState);
            nextState = null;
        }

        if (currentState != null)
        {
            try
            {
                currentState.Update();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                ExitState();
            }
        }

        if (queuedState != null)
        {
            queuedStateTimer -= Time.deltaTime;
            if (queuedStateTimer <= 0)
            {
                queuedState = null;
            }
        }
    }

    public void QueueState(ActionState state, bool priorityQueue = false)
    {
        if (!currentState.FulfilsQueueingConditions(state))
        {
            return;
        }
        if ((queuedThisFrame || priorityQueueActive) && queuedState != null)
        {
            if (queuedState.priority > state.priority)
            {
                return;
            }
        }

        queuedState = state;
        queuedStateTimer = stateQueueTime;
        priorityQueueActive = priorityQueue;

        queuedThisFrame = true;
    }

    public void LateUpdate()
    {
        if (queuedThisFrame)
        {
            queuedThisFrame = false;
            EvaluateQueuedState();
        }

        if (currentState != null)
        {
            try
            {
                currentState.LateUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                ExitState();
            }
        }   
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if (currentState != null)
        {
            currentState.fixedAge += Time.fixedDeltaTime;
            try
            {
                currentState.FixedUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                ExitState();
            }
        }
    }

    public void SyncState(ActionState state)
    {
        CmdReceiveState(SerializeState(state));
    }

    [Command(requiresAuthority = false)]
    public void CmdReceiveState(byte[] data)
    {
        if (!NetworkClient.active)
        {
            SetState(DeserializeState(data), true);
        }
        RpcReceiveState(data);
    }

    [ClientRpc]
    public void RpcReceiveState(byte[] data)
    {
        if (!isAuthority)
        {
            SetState(DeserializeState(data), true);
        }
    }

    public void PlaySound(string soundName, Vector3 position, bool sync, GameObject parent = null)
    {
        AudioCaller.instance.PlaySound(soundName, position, parent);

        if (sync)
        {
            if (NetworkServer.active)
            {
                RpcPlaySoundSynced(soundName, position, parent);
            }
            else
            {
                CmdPlaySoundSynced(soundName, position, parent);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPlaySoundSynced(string soundName, Vector3 position, GameObject parent)
    {
        RpcPlaySoundSynced(soundName, position, parent);
    }

    [ClientRpc]
    public void RpcPlaySoundSynced(string soundName, Vector3 position, GameObject parent)
    {
        if (!isLocalPlayer)
        {
            PlaySound(soundName, position, false, parent);
        }
    }


    [Command(requiresAuthority = false)]
    public void CmdPlayAnimationSynced(int layer, string stateName, float crossFadeDuration)
    {
        RpcPlayAnimationSynced(layer, stateName, crossFadeDuration);
    }

    [ClientRpc]
    public void RpcPlayAnimationSynced(int layer, string stateName, float crossFadeDuration)
    {
        if (!isLocalPlayer)
        {
            currentState.PlayAnimationCrossFade(layer, stateName, crossFadeDuration, false);
        }
    }

    public byte[] SerializeState(ActionState state)
    {
        NetworkWriter writer = new NetworkWriter();
        writer.WriteInt((int)state.stateSyncType);

        switch(state.stateSyncType)
        {
            case ActionState.StateSyncType.SyncClone:
                {
                    writer.WriteString(state.GetType().ToString());
                    break;
                }
            case ActionState.StateSyncType.SyncOriginal:
                {
                    if (state.assetID < 0)
                    {
                        Debug.LogError("Trying to sync original state that isn't stored in resources : " + state.name);
                    }
                    writer.WriteInt(state.assetID);

                    break;
                }
        }

        state.Serialize(writer);

        return writer.ToArray();
    }

    public ActionState DeserializeState(byte[] data)
    {
        NetworkReader reader = new NetworkReader(data);
        ActionState.StateSyncType stateSyncType = (ActionState.StateSyncType)reader.ReadInt();
        ActionState cloneState = null;

        switch (stateSyncType)
        {
            case ActionState.StateSyncType.SyncClone:
                {
                    string stateTypeName = reader.ReadString();
                    Type stateType = Type.GetType(stateTypeName);
                    cloneState = ScriptableObject.CreateInstance(stateType) as ActionState;
                    break;
                }
            case ActionState.StateSyncType.SyncOriginal:
                {
                    int stateID = reader.ReadInt();
                    cloneState = Instantiate(FracturedAssets.actionStateInstances[stateID]);
                    break;
                }
        }

        cloneState.Deserialize(reader);

        return cloneState;
    }

    public void AnimationCallback(string name)
    {
        ActionState.AnimationCallback callback;
        if (currentState.animationCallback.TryGetValue(name, out callback))
        {
            callback.Invoke();
        }
    }

    public void ExitState()
    {

        SetState(InstantiateState(mainStateType));
    }

    public void SetNextState(string stateName, bool forceInterrupt = false)
    {
        SetNextState(ActionState.CreateState(stateName), forceInterrupt);
    }

    public void SetNextState(ActionState state, bool forceInterrupt = false)
    {
        if (!CanInterruptState(state, currentState) || !CanInterruptState(state, nextState) && !forceInterrupt)
        {
            return;
        }
        nextState = state;
    }

    public bool CanInterruptState(ActionState state, ActionState stateToInterrupt)
    {
        if (stateToInterrupt == null)
        {
            return true;
        }
        return state.priority >= stateToInterrupt.minimumInterruptPriority && stateToInterrupt.FulfilsAdditionalInterruptConditions(state);
    }

    public void SetState(string stateName)
    {
        SetState(ActionState.CreateState(stateName));
    }

    public void SetState(ActionState state, bool localClone = false)
    {
        if (!FracturedUtility.HasEffectiveAuthority(characterMaster.networkIdentity) && !localClone)
        {
            return;
        }

        if (!NetworkServer.active && !NetworkClient.active)
        {
            return;
        }

        if (currentState != null)
        {
            currentState.stateEnded = true;
            currentState.actionEnded.Invoke();

            try
            {
                currentState.OnExit();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

        }

        currentState = state;
        currentState.fixedAge = 0;
        currentState.animator = animator;
        currentState.characterMovement = characterMovement;
        currentState.characterMaster = characterMaster;
        currentState.outer = this;

        nextState = null;

        if (!localClone)
        {
            state.PreSync();
            SyncState(state);
        }

        state.actionStarted.Invoke();
        currentState.BaseOnEnter();

        try
        {
            currentState.OnEnter();
        }
        catch
        {

        }

        EvaluateQueuedState();

        queuedState = null;
    }

    public void SpawnReferencedProjectile(ProjectileSpawnInfo spawnInfo, UnityAction<GameObject> receiveProjectile, ActionState requestingState)
    {
        if (NetworkServer.active)
        {
            GameObject spawnedProjectile = GameManager.instance.ServerSpawnReferencedProjectile(spawnInfo);
            receiveProjectile.Invoke(spawnedProjectile);
            return;
        }

        requestingState.projectileSpawnAction = receiveProjectile;
        spawnProjectileActionState = requestingState;
        spawnProjectileInstanceID = requestingState.GetInstanceID();
        CmdSpawnReferencedProjectile(spawnInfo);
    }

    [Command]
    public void CmdSpawnReferencedProjectile(ProjectileSpawnInfo spawnInfo)
    {
        GameObject projectile = GameManager.instance.ServerSpawnReferencedProjectile(spawnInfo);
        ReceiveSpawnedProjectile(projectile);
    }

    [ClientRpc]
    public void ReceiveSpawnedProjectile(GameObject projectile)
    {
        if (isAuthority)
        {
            if (spawnProjectileActionState != null && spawnProjectileActionState.GetInstanceID() == spawnProjectileInstanceID)
            {
                spawnProjectileActionState.projectileSpawnAction.Invoke(projectile);
            }
            spawnProjectileActionState = null;
            spawnProjectileInstanceID = -1;
        }
    }

    public static ActionState InstantiateState(SerializableEntityStateType serializableStateType)
    {
        return InstantiateState(serializableStateType.stateType);
    }

    public static ActionState InstantiateState(Type stateType)
    {
        if (stateType != null && stateType.IsSubclassOf(typeof(ActionState)))
        {
            ActionState state = ScriptableObject.CreateInstance(stateType) as ActionState;
            state.stateSyncType = ActionState.StateSyncType.SyncClone;
            return state;
        }
        Debug.LogFormat("Bad stateType {0}", new object[]
        {
                (stateType == null) ? "null" : stateType.FullName
        });
        return null;
    }

    public void InvokeRPC(int index)
    {
        if (NetworkServer.active)
        {
            DoRPC(index);
            RpcInvokeRPC(index);
        }
        else
        {
            CmdInvokeRPC(index);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdInvokeRPC(int index)
    {
        DoRPC(index);
        RpcInvokeRPC(index);
    }

    [ClientRpc]
    public void RpcInvokeRPC(int index)
    {
        if (!NetworkServer.active)
        {
            DoRPC(index);
        }
    }

    public void DoRPC(int index)
    {
        currentState.InvokeRPC(index);
    }
}

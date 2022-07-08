using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using Mirror;
using UnityEngine.InputSystem;

//[RequireComponent(typeof(NavMeshAgent))]
public class GenericAI : NetworkBehaviour
{
    protected InputBank inputBank;
    protected CharacterMaster characterMaster;
    protected NavMeshPath path;
    protected Collider movementCollider;
    protected NavMeshAgent navMeshAgent;
    protected ActionLocator actionLocator;
    protected int targetCorner = 1;

    public bool activated = true;
    public bool useNavMeshPathfinding = true;

    protected float pathRecalculateTimer = 0f;
    protected float pathRecalculateTime = 0.25f;
    protected Vector3 lastDirection;
    protected Vector3 lastPosition;

    protected List<AIActionDef> potentialActions = new List<AIActionDef>();

    protected float age;

    public bool activateOnRoomEntry = false;
    public bool invincibleUntilActivated = false;

    private readonly float targetInterruptTime = 5f;
    private float targetInterruptTimer = 0f;

    [Serializable]
    public class AIActionDef
    {
        public ActionLocator.ActionType actionType;
        public float minDistanceToTarget;
        public float maxDistanceToTarget;
        public float minAngleFromTarget;
        public float maxAngleFromTarget;
        public float minHealthPercent;
        public float maxHealthPercent;
        public float minCharacterAge;
        public bool overrideContest = false;
        public bool canInterrupt = false;
    }

    public AIActionDef[] actions;


    private ActionLocator.ActionType chosenAction = ActionLocator.ActionType.None;

    public class PlayerTargetingInfo
        {
        public NetworkIdentity identity;
        public CharacterMaster characterMaster;
        public float damageDealt;
        public float targetTime;
        public int weight;
        }

    [System.NonSerialized] public Dictionary<int, PlayerTargetingInfo> targetDictionary = new Dictionary<int, PlayerTargetingInfo>();
    [System.NonSerialized] public PlayerTargetingInfo currentTarget;

    public virtual void Awake()
    {
        inputBank = GetComponent<InputBank>();
        characterMaster = GetComponent<CharacterMaster>();
        movementCollider = GetComponent<Collider>();
        actionLocator = GetComponent<ActionLocator>();

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
        }
    }

    public virtual void Start()
    {

    }

    // Start is called before the first frame update
    public override void OnStartServer()
    {
        path = new NavMeshPath();

        GetComponent<ActionLocator>().actionEnded.AddListener(SelectTarget);

        GameManager.instance.onLivingPlayerListChanged.AddListener(RefreshPlayerDictionary);
        RefreshPlayerDictionary();


        characterMaster.onHit.AddListener(AddDamageToTargetValue);

        SelectTarget();
    }

    public void Activate()
    {
        enabled = true;
        characterMaster.characterHealth.invincible = false;
    }

    public void AddDamageToTargetValue(DamageInfo damageInfo)
    {
        // Return if a player didn't cause the damage
        if (damageInfo.attacker == null || damageInfo.attacker.connectionToClient == null)
        {
            return;
        }

        PlayerTargetingInfo attacker;

        if (targetDictionary.TryGetValue(damageInfo.attacker.connectionToClient.connectionId, out attacker))
        {
            attacker.damageDealt += damageInfo.damage;
            if ((characterMaster.actionStateMachine.currentState.GetType() == typeof(Idle) || characterMaster.actionStateMachine.currentState.targetInterruptable) && targetInterruptTimer <= 0)
            {
                SetTarget(attacker);
                targetInterruptTimer = targetInterruptTime;
            }
        }
    }

    public void RefreshPlayerDictionary()
    {
        Dictionary<int, PlayerTargetingInfo> refreshedDictionary = new Dictionary<int, PlayerTargetingInfo>();
        foreach(NetworkIdentity identity in GameManager.instance.livingPlayers)
        {
            PlayerTargetingInfo info;
            if (targetDictionary.TryGetValue(identity.connectionToClient.connectionId, out info))
            {
                targetDictionary.Remove(identity.connectionToClient.connectionId);
            }
            else
            {
                info = new PlayerTargetingInfo()
                {
                    identity = identity,
                    characterMaster = identity.GetComponent<CharacterMaster>(),
                };
            }

            refreshedDictionary.TryAdd(identity.connectionToClient.connectionId, info);
        }

        Dictionary<int, PlayerTargetingInfo> removedDictionary = targetDictionary;
        targetDictionary = refreshedDictionary;

        if (currentTarget == null || removedDictionary.ContainsKey(currentTarget.identity.connectionToClient.connectionId))
        {
            SelectTarget();
        }
    }

    public void AddToPlayerDictionary(NetworkConnection conn)
    {
        PlayerTargetingInfo targetingInfo = new PlayerTargetingInfo()
        {
            identity = conn.identity,
            characterMaster = conn.identity.GetComponent<CharacterMaster>(),
        };

        targetingInfo.characterMaster.onDisable.AddListener(RemoveFromPlayerDictionary);
        targetingInfo.characterMaster.onEnable.RemoveListener(AddToPlayerDictionary);

        targetDictionary.TryAdd(conn.connectionId, targetingInfo);
    }

    public void RemoveFromPlayerDictionary(NetworkConnection conn)
    {
        PlayerTargetingInfo targetingInfo;
        if (targetDictionary.TryGetValue(conn.connectionId, out targetingInfo))
        {
            targetDictionary.Remove(conn.connectionId);
            if (ReferenceEquals(targetingInfo, currentTarget))
            {
                SelectTarget();
            }
        }
        else
        {
            targetDictionary.Remove(conn.connectionId);
        }

        var characterMaster = conn.identity.GetComponent<CharacterMaster>();
        characterMaster.onDisable.RemoveListener(RemoveFromPlayerDictionary);
        characterMaster.onEnable.AddListener(AddToPlayerDictionary);
    }

    private void Update()
    {
        if (NetworkServer.active)
        {
            if (characterMaster.combatTarget == null)
            {
                SelectTarget();
            }
            if (characterMaster.combatTarget != null)
            {
                targetInterruptTimer -= Time.deltaTime;

                if (useNavMeshPathfinding && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.nextPosition = transform.position;
                    navMeshAgent.SetDestination(characterMaster.combatTarget.transform.position);
                }

                if (currentTarget != null)
                {
                    currentTarget.targetTime += Time.deltaTime;
                }

                age += Time.deltaTime;

#if UNITY_EDITOR
                if (Keyboard.current[Key.Pause].wasPressedThisFrame)
                {
                    activated = !activated;
                }
#endif
                if (activated)
                {
                    inputBank.moveAxis = GetTargetDirection();
                    EvaluateActions();
                    PerformChosenAction();
                }
            }
        }
    }

    private void EvaluateActions()
    {
        potentialActions.Clear();
        chosenAction = ActionLocator.ActionType.None;
        float distanceToTarget = Vector3.Distance(transform.position.XZPlane(), characterMaster.combatTarget.transform.position.XZPlane());
        Vector3 direction = (characterMaster.combatTarget.transform.transform.position - transform.position).XZPlane().normalized;
        float angle = Vector3.Angle(characterMaster.modelPivot.forward, direction);
        float healthPercent = characterMaster.characterHealth.health / characterMaster.characterHealth.maxHealth * 100f;
        bool isIdle = characterMaster.actionStateMachine.currentState.GetType() == typeof(Idle);

        foreach (AIActionDef actionDef in actions)
        {
            int actionType = (int)actionDef.actionType;
            if (actionLocator.orderedActions[actionType].currentCooldown > 0)
            {
                continue;
            }
            if (!actionDef.canInterrupt && !isIdle)
            {
                continue;
            }

            if (distanceToTarget < actionDef.minDistanceToTarget)
            {
                continue;
            }

            if (distanceToTarget > actionDef.maxDistanceToTarget && actionDef.maxDistanceToTarget > 0)
            {
                continue;
            }

            if (angle < actionDef.minAngleFromTarget)
            {
                continue;
            }

            if (angle > actionDef.maxAngleFromTarget && actionDef.maxAngleFromTarget > 0)
            {
                continue;
            }

            if (healthPercent < actionDef.minHealthPercent && actionDef.minHealthPercent > 0)
            {
                continue;
            }

            if (healthPercent > actionDef.maxHealthPercent && actionDef.maxHealthPercent > 0)
            {
                continue;
            }

            if (age < actionDef.minCharacterAge)
            {
                continue;
            }

            if (actionDef.overrideContest)
            {
                potentialActions.Clear();
            }

            potentialActions.Add(actionDef);

            if (actionDef.overrideContest)
            {
                break;
            }
        }

        if (potentialActions.Count > 0)
        {
            AIActionDef selectedAction = potentialActions[UnityEngine.Random.Range(0, potentialActions.Count)];
            chosenAction = selectedAction.actionType;
        }
    }

    private void PerformChosenAction()
    {
        for (int i = 0; i < inputBank.actionInputs.Length; i++)
        {
            inputBank.actionInputs[i] = false;
        }
        if (chosenAction != ActionLocator.ActionType.None)
        {
            inputBank.actionInputs[(int)chosenAction] = true;
        }

    }

    public void SelectTarget()
    {
        if (!enabled)
        {
            return;
        }
        if (targetDictionary.Count == 0)
        {
            SetTarget(null);
            return;
        }

        PlayerTargetingInfo[] targets = new PlayerTargetingInfo[targetDictionary.Count];

        int i = 0;
        foreach(KeyValuePair<int, PlayerTargetingInfo> infoPair in targetDictionary)
        {
            PlayerTargetingInfo info = infoPair.Value;
            targets[i] = info;
            i++;
        }

        CalculateWeights(targets);

        PlayerTargetingInfo newTarget = RandomWeighted(targets);
        SetTarget(newTarget);

    }

    public void SetTarget(PlayerTargetingInfo newTarget)
    {
        if (newTarget != null)
        {
            characterMaster.combatTarget = new CharacterMaster.CombatTargetInfo
            {
                transform = newTarget.characterMaster.bodyCenter,
                characterMaster = newTarget.characterMaster,
            };
        }
        else
        {
            characterMaster.combatTarget = null;
        }

        if (currentTarget != null)
        {
            currentTarget.targetTime = 0f;
        }

        currentTarget = newTarget;
    }

    public PlayerTargetingInfo RandomWeighted(PlayerTargetingInfo[] targets)
    {
        int weightTotal = 0;
        foreach (PlayerTargetingInfo target in targets)
        {
            weightTotal += target.weight;
        }

        // Shuffle array
        PlayerTargetingInfo tempInfo;
        for (int i = 0; i < targets.Length - 1; i++)
        {
            int rnd = UnityEngine.Random.Range(i, targets.Length);
            tempInfo = targets[rnd];
            targets[rnd] = targets[i];
            targets[i] = tempInfo;
        }

        int result = 0, total = 0;
        int randVal = UnityEngine.Random.Range(0, weightTotal);
        for (result = 0; result < targets.Length; result++)
        {
            total += targets[result].weight;
            if (total >= randVal) break;
        }
        return targets[result];
    }

    public void CalculateWeights(PlayerTargetingInfo[] targets)
    {
        float minDamage = Mathf.Infinity;
        float maxDamage = -Mathf.Infinity;
        foreach(PlayerTargetingInfo target in targets)
        {
            if (target.damageDealt < minDamage)
            {
                minDamage = target.damageDealt;
            }

            if (target.damageDealt > maxDamage)
            {
                maxDamage = target.damageDealt;
            }
        }

        foreach (PlayerTargetingInfo target in targets)
        {
            int weight = 100;
            weight += Mathf.CeilToInt(Mathf.InverseLerp(minDamage, maxDamage, target.damageDealt) * 100f);
            target.damageDealt = 0f;

            weight -= Mathf.CeilToInt(target.targetTime * 10f);

            float distance = Vector3.Distance(target.identity.transform.position, transform.position);
            weight -= Mathf.CeilToInt(distance * 2f);
            weight = Mathf.Max(0, weight);
            target.weight = weight;
        }
    }

    protected virtual Vector3 GetTargetDirection()
    {
        if (!useNavMeshPathfinding)
        {
            Vector3 directDirection = (characterMaster.combatTarget.transform.position - transform.position).normalized;
            return directDirection.XZPlane();
        }

        Vector3 steeringDirection = navMeshAgent.steeringTarget - transform.position;
        steeringDirection = steeringDirection.normalized;
        return steeringDirection.XZPlane();
    }
}

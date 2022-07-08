using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using UnityEngine.Serialization;
using UnityEngine.Profiling;
using UnityEngine.InputSystem;

public class CharacterMaster : NetworkBehaviour
{
    [System.NonSerialized] public CharacterMovement characterMovement;
    [System.NonSerialized] public CharacterHealth characterHealth;
    [System.NonSerialized] public ActionStateMachine actionStateMachine;
    [System.NonSerialized] public InputBank inputBank;
    [System.NonSerialized] public ChildLocator childLocator;
    [System.NonSerialized] public NetworkIdentity networkIdentity;

    public Animator animator;

    public int teamIndex;

    public UnityEvent<DamageInfo> onHit = new UnityEvent<DamageInfo>();
    [System.NonSerialized] public UnityEvent onBuffsChangedEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent<DamageInfo, CharacterMaster> onDamageDealtConfirmedEvent = new UnityEvent<DamageInfo, CharacterMaster>();

    public class SpawnOnDeathInfo
        {
        public CharacterMaster prefab;
        public Transform position;
        }

    public bool isAuthority
    {
        get
        {
            return FracturedUtility.HasEffectiveAuthority(netIdentity);
        }
    }

    [Header("Death")]
    public List<SpawnOnDeathInfo> spawnOnDeath;
    public ActionState deathState;

    public List<SkinnedMeshRenderer> skinnedMaterialRenderers;
    public List<SkinnedMeshRenderer> skinnedMaterialRenderertemp;
    [System.NonSerialized] public List<Material> characterMaterials;

    public ActionState startingState;


    [System.NonSerialized]
    public bool godMode = false;

    [System.NonSerialized]
    public bool godModeNoStagger = false;

    [System.NonSerialized]
    public float damageModifier = 1f;

    public class CombatTargetInfo
    {
        public CharacterMaster characterMaster;
        public Transform transform;
    }

    [System.NonSerialized]
    public CombatTargetInfo combatTarget;

    // Combat soft-target
    public CombatTargetInfo softCombatTarget;
    private readonly float softCombatTargetRefreshTime = 0.1f;
    private float softCombatTargetRefreshTimer = 0f;

    [SyncVar]
    [System.NonSerialized] public bool killOnRoomExit = false;
    public static List<CharacterMaster> charactersToKillOnRoomExit = new List<CharacterMaster>();

    // Returns locked or soft combat target
    public CombatTargetInfo generalCombatTarget
    {
        get
        {
            if (combatTarget != null)
            {
                return combatTarget;
            }
            else return softCombatTarget;
        }
    }

    [System.NonSerialized]
    public Transform softTarget;

    [SerializeField] private Transform lockOnOverride;
    [SerializeField] private Transform losPointOverride;

    [System.NonSerialized]
    public bool isComboing;

    // Buffs
    private class TimedBuff
    {
        public int stacks;
        public BuffDef buff;
        public float timer;
    }

    private int[] buffs;
    private Dictionary<int, List<TimedBuff>> timedBuffStacks = new Dictionary<int, List<TimedBuff>>();
    [System.NonSerialized] public List<BuffDef> timedBuffsToClear = new List<BuffDef>();
    [System.NonSerialized] public GameObject[] buffEffects;
    public float buffEffectScalar = 1f;

    private int deathStateOverride;
    private bool deathStateSet;
    private bool giveExperience;

    // Damage Over Time
    private class Dot
    {
        public DotDef dotDef;
        public float timer;
        public CharacterMaster inflictor;
        public DamageInfo damage;
        public int stacks;
    }

    private class DotStack
    {
        public List<Dot> dots = new List<Dot>();
        public float tickTimer;
        public DotDef dotDef;
    }

    private Dictionary<int, DotStack> dotStacks = new Dictionary<int, DotStack>();

    // Hit Stop
    [System.NonSerialized] public float hitStopMultiplier = 1f;
    [System.NonSerialized] public int targetsHitThisAttack = 0;
    private float hitStopTimer;
    private float hitStopModifiedRecoveryTime;

    //Items
    private List<float> stackingProtectionTimers = new List<float>();

    public class StoredBlockedDamage
    {
        public DamageInfo damageInfo;
        public float timer;
    }
    [System.NonSerialized]
    public List<StoredBlockedDamage> storedBlockedDamageInstances = new List<StoredBlockedDamage>();


    private readonly float lowHealthThreshold = 0.4f;

    public bool scaleFromGlobalLevel = true;
    private float damageScalar = 1f;

    public bool killOnMapFall = true;

    [Header("Child References")]
    public Transform modelPivot;

    public Vector3 modelForward
    {
        get
        {
            return modelPivot.forward;
        }
    }

    public Vector3 directionToTarget
    {
        get
        {
            return (combatTarget.transform.position - bodyCenter.position).normalized;
        }
    }

    public Quaternion modelRotation
    {
        get
        {
            return modelPivot.rotation;
        }
    }

    private GameObject characterBody;

    [System.NonSerialized] public bool cheatInstaKill = false;
    [System.NonSerialized] public float expOnDeath = 0;

    [System.NonSerialized]
    public int projectileBlades = 0;
    public bool bladeSlash = false;

    [System.NonSerialized] float hyperArmourTimer = 0f;

    // Stats
    [System.NonSerialized] public float criticalChance;
    [System.NonSerialized] public float criticalDamage;
    [System.NonSerialized] public float baseCriticalDamage = 1.5f;

    [System.NonSerialized] public float meleeDamageMultiplier = 1f;
    [System.NonSerialized] public float magicDamageMultiplier = 1f;
    [System.NonSerialized] public float genericDamageMultiplier = 1f;

    [System.NonSerialized] public bool dead = false;

    // Staggering
    [System.Serializable]
    public struct StaggerThreshold
    {
        public StaggerState staggerState;
        public float threshold;
    };

    [Header("Staggering")]
    public bool canFlinch = true;
    public bool canLaunch = false;
    [FormerlySerializedAs("staggerState")]
    public StaggerState defaultStaggerState;
    public List<StaggerThreshold> additionalStaggerThresholds;
    public float hyperArmourSecondsOnStagger = 0f;

    // Knockdowns (Buildup primarily on bosses)
    [Header("Knockdowns")]
    public ActionState knockdownState;

    [FormerlySerializedAs("stability")]
    public float knockdownStability = 0;
    private float currentKnockdownStability;
    [System.NonSerialized] public bool knockdownImmunity = false;

    // Effects
    [Header("Effects")]
    public Effect[] onHitEffects;
    private List<Mesh> meshes = new List<Mesh>();

    [System.NonSerialized] public UnityEvent<NetworkConnection> onDisable = new UnityEvent<NetworkConnection>();
    [System.NonSerialized] public UnityEvent<NetworkConnection> onEnable = new UnityEvent<NetworkConnection>();

    [Header("HitFlash")]
    public HitFlashData hitFlashData;

    private float comboLightningTimer = 0;

    [SyncVar]
    [System.NonSerialized] public bool isPlayer;

    [Header("Other")]
    public bool isBoss = false;

    [System.NonSerialized] public float blockPower;
    [System.NonSerialized] public float weaponCritChance = 0f;
    [System.NonSerialized] public float weaponDropChance = 0f;

    public SyncList<NetworkIdentity> unparentOnDeath = new SyncList<NetworkIdentity>();

    public Transform losPoint
    {
        get
        {
            if (losPointOverride != null)
            {
                return losPointOverride;
            }
            return bodyCenter;
        }
    }

    public Transform bodyCenter
    {
        get
        {
            if (lockOnOverride != null)
            {
                return lockOnOverride;
            }
            return transform;
        }
    }

    public Transform aerialLockOn
    {
        get
        {
            if (aerialLockOnOverride != null)
            {
                return aerialLockOnOverride;
            }
            return bodyCenter;
        }
    }

    public Transform aerialLockOnOverride;

    public bool hasHyperArmour
    {
        get
        {
            return hyperArmourTimer > 0;
        }
    }


    public bool isEnemy
    {
        get
        {
            return teamIndex != 0;
        }
    }

    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();
        characterHealth = GetComponent<CharacterHealth>();
        actionStateMachine = GetComponent<ActionStateMachine>();
        inputBank = GetComponent<InputBank>();
        childLocator = GetComponent<ChildLocator>();
        networkIdentity = GetComponent<NetworkIdentity>();

        

        currentKnockdownStability = knockdownStability;
        characterBody = modelPivot.GetChild(0).gameObject;

        skinnedMaterialRenderertemp = new List<SkinnedMeshRenderer>();

        characterMaterials = new List<Material>();
        buffs = BuffManager.GetEmptyBuffArray();

        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedMaterialRenderers)
        {
            for (int i = 0; i < skinnedRenderer.materials.Length; i++)
            {
                characterMaterials.Add(skinnedRenderer.materials[i]);
            }
        }

        if (isEnemy && GameManager.instance != null)
        {
            GameManager.instance.RegisterEnemy(this);
        }

        buffEffects = new GameObject[BuffManager.buffs.Length];
    }

    public void Start()
    {
        if (isLocalPlayer)
        {
            GameManager.instance.localPlayerCharacter = this;
        }

        if (startingState != null)
        {
            actionStateMachine.SetNextState(Instantiate(startingState), true);
        }
    }

    public void OnEnable()
    {
        if (isEnemy && GameManager.instance != null)
        {
            GameManager.instance.RegisterEnemy(this);
        }

        NetworkConnection connectionToClient = networkIdentity.connectionToClient;
        if (NetworkServer.active && connectionToClient != null)
        {
            GameManager.instance.AddLivingPlayer(networkIdentity, networkIdentity.connectionToClient.connectionId);
        }

        onEnable.Invoke(connectionToClient);

        CharacterSearcher.searchTargets.Add(this);
    }

    public void OnDisable()
    {
        if (isEnemy && GameManager.instance != null)
        {
            GameManager.instance.DeregisterEnemy(this);
        }

        // Check if this character is controlled by a client (is player)
        NetworkConnection connectionToClient = networkIdentity.connectionToClient;
        if (NetworkServer.active && connectionToClient != null)
        {
            GameManager.instance.RemoveLivingPlayer(networkIdentity.connectionToClient.connectionId);
        }

        onDisable.Invoke(connectionToClient);

        CharacterSearcher.searchTargets.Remove(this);

        for (int i = 0; i < buffEffects.Length; i++)
        {
            if (buffEffects[i] != null)
            {
                buffEffects[i].transform.SetParent(null, true);
                Destroy(buffEffects[i]);
            }
        }
    }

    public override void OnStartServer()
    {
        if (killOnRoomExit)
        {
            charactersToKillOnRoomExit.Add(this);
        }

        if (netIdentity.connectionToClient != null)
        {
            FracturedNetworkManager.instance.OnPlayerSpawned(netIdentity);
            isPlayer = true;
            SetSyncVarDirtyBit(1U);
        }

        if (scaleFromGlobalLevel)
        {
            float healthScalar = (1 + GameVariables.instance.baseEnemyHealthScalar * Mathf.Pow(GameManager.instance.globalLevel, GameVariables.instance.enemyHealthScalingExponent)) * (1 + GameManager.instance.fractureLevel * 0.1f);

            damageScalar = (1 + GameVariables.instance.baseEnemyDamageScalar * Mathf.Pow(GameManager.instance.globalLevel, GameVariables.instance.enemyHealthScalingExponent)) * (1 + GameManager.instance.fractureLevel * 0.1f);

            characterHealth.baseMaxHealth *= healthScalar;
            characterHealth.maxHealth = characterHealth.baseMaxHealth;
            characterHealth.health = characterHealth.maxHealth;
        }

        if (networkIdentity.connectionToClient != null)
        {
            GameManager.instance.AddLivingPlayer(networkIdentity, networkIdentity.connectionToClient.connectionId);
        }

        knockdownStability *= FracturedNetworkManager.playerCount;

        RecalculateAllStats();
    }

    public override void OnStartLocalPlayer()
    {
    }

    [ClientRpc]
    public void ClientRpcQueueForDeath(int deathStateOverride)
    {
        QueueForDeath(deathStateOverride);
    }

    public void QueueForDeath(int deathStateOverride)
    {
        dead = true;
        this.deathStateOverride = deathStateOverride;
        for (int i = 0; i < unparentOnDeath.Count; i++)
        {
            if (unparentOnDeath[i] != null)
            {
                unparentOnDeath[i].transform.SetParent(null, true);
            }
        }
    }

    public void LateUpdate()
    {
        if (dead && !deathStateSet)
        {
            deathStateSet = true;

            if (FracturedUtility.HasEffectiveAuthority(networkIdentity))
            {
                SetDeathState(deathStateOverride);
            }

            if (NetworkServer.active)
            {
                characterHealth.PostDeath();
                //if (expOnDeath > 0 && giveExperience)
                //{
                //    GameManager.instance.GainExperience(expOnDeath);
                //}
                //if (giveExperience && weaponDropChance > 0)
                //{
                //    if (Random.value < weaponDropChance)
                //    {
                //        GameObject weaponDrop = Instantiate(GameVariables.instance.weaponPickUpPrefab, bodyCenter.position, Quaternion.identity);
                //        NetworkServer.Spawn(weaponDrop);
                //    }
                //}
            }
        }
    }

    [Server]
    public void ServerDeath(ActionState deathStateOverride = null, bool giveExperience = true)
    {
        int deathStateOverrideInt = deathStateOverride == null ? -1 : deathStateOverride.assetID;

        ClientRpcQueueForDeath(deathStateOverrideInt);
        QueueForDeath(deathStateOverrideInt);
        this.giveExperience = giveExperience;
    }

    public void SetDeathState(int deathStateOverride)
    {
        ActionState nextDeathState = deathState;
        if (deathStateOverride >= 0)
        {
            nextDeathState = FracturedAssets.actionStateInstances[deathStateOverride];
        }
        if (deathState != null)
        {
            actionStateMachine.SetState(Instantiate(nextDeathState));
        }
    }

    private void FixedUpdate()
    {
        if (!NetworkServer.active)
        {
            return;
        }

        if (transform.position.y < GameManager.instance.killPlaneYPosition)
        {
            HitFallPlane();
        }
    }

    public void HitFallPlane()
    {
        if (!NetworkServer.active)
        {
            return;
        }
        if (killOnMapFall)
        {
            characterHealth.health = 0f;
        }
        else
        {
            characterMovement.Teleport(characterMovement.lastStablePosition);
            characterHealth.health = Mathf.Max(characterHealth.health - characterHealth.maxHealth * 0.5f, 1f);
        }
    }

    public void Update()
    {
        UpdateHyperArmour();
        UpdateHitStop();

        if (isAuthority || NetworkServer.active)
        {
            UpdateSoftCombatTarget();
        }

        if (NetworkServer.active)
        {
            UpdateBuffs();
            UpdateBuffEffects();
            UpdateDots();


#if UNITY_EDITOR
            // DEBUG
            if (isLocalPlayer)
            {
                if (Keyboard.current[Key.U].wasPressedThisFrame)
                {
                    foreach (CharacterMaster enemy in GameManager.instance.enemies.Values)
                    {
                        enemy.AddTimedBuff(Buffs.Burning, this, 5);
                    }
                }

                if (Keyboard.current[Key.O].wasPressedThisFrame)
                {
                    Stagger(new DamageInfo(), 0);
                }

                if (Keyboard.current[Key.Numpad3].wasPressedThisFrame)
                {
                    GameObject weaponDrop = Instantiate(GameVariables.instance.weaponPickUpPrefab, bodyCenter.position, Quaternion.identity);
                    NetworkServer.Spawn(weaponDrop);
                }

                Vector3 aimPoint = FracturedUtility.GetAimPoint(this);
                Debug.DrawRay(aimPoint, Camera.main.transform.up, Color.red);
                Debug.DrawRay(aimPoint, Camera.main.transform.right, Color.blue);
                Debug.DrawRay(aimPoint, Camera.main.transform.forward, Color.green);

            }
#endif
        }
    }

    public void BakeSkinnedMeshes()
    {
        Profiler.BeginSample("SkinnedMeshBaking");
        meshes.Clear();

        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedMaterialRenderers)
        {
            Mesh mesh = new Mesh();
            skinnedRenderer.BakeMesh(mesh);
            meshes.Add(mesh);
        }
        Profiler.EndSample();
    }

    public void UpdateBuffEffects()
    {
        
    }

    public void UpdateSoftCombatTarget()
    {
        if (isPlayer)
        {
            softCombatTargetRefreshTimer -= Time.deltaTime;
            if (softCombatTargetRefreshTimer < 0f)
            {
                softCombatTargetRefreshTimer = softCombatTargetRefreshTime;
            }
        }
    }

    public void OnKilledEnemy(DamageInfo damageInfo, CharacterMaster victim)
    {
    }

    public void AddHyperArmour()
    {
        hyperArmourTimer = Mathf.Max(hyperArmourSecondsOnStagger, hyperArmourTimer);
    }

    public void AddHyperArmour(float time)
    {
        hyperArmourTimer = Mathf.Max(time, hyperArmourTimer);
    }

    [Server]
    public void ServerRevive()
    {
        characterHealth.destroyed = false;
        characterHealth.health = characterHealth.maxHealth;
        
        Revive();
        RpcRevive();
    }

    [ClientRpc]
    private void RpcRevive()
    {
        if (!NetworkServer.active)
        {
            Revive();
        }
    }

    private void Revive()
    {
        dead = false;
        deathStateSet = false;
        if (isLocalPlayer)
        {
            actionStateMachine.SetState(ActionStateMachine.InstantiateState(typeof(Idle)));
        }
        gameObject.SetActive(true);
    }

    public void UpdateHyperArmour()
    {
        if (hasHyperArmour)
        {
            hyperArmourTimer -= Time.deltaTime;
        }
    }

    public enum RecalculateStatsTrigger
    {
        HealthChanged,
        StatCardsChanged,
        ItemsChanged,
        BuffsChanged,
        All
    }

    public void RecalculateAllStats()
    {
        RecalculateStats(RecalculateStatsTrigger.All);
    }

    public void RecalculateStatsInventory()
    {
        RecalculateStats(RecalculateStatsTrigger.ItemsChanged);
    }

    public void RecalculateStatsStatCards()
    {
        RecalculateStats(RecalculateStatsTrigger.StatCardsChanged);
    }

    public void RecalculateStatsHealth()
    {
        RecalculateStats(RecalculateStatsTrigger.HealthChanged);
    }

    public void RecalculateStatsBuffs()
    {
        RecalculateStats(RecalculateStatsTrigger.BuffsChanged);
    }

    public void RecalculateStats(RecalculateStatsTrigger trigger)
    {
      
        damageModifier = 1f;
        damageModifier *= damageScalar;

        if (cheatInstaKill)
        {
            damageModifier = 99999;
        }

        if (isPlayer)
        {
            genericDamageMultiplier = 1 + GameManager.instance.playerLevel * 0.1f;
        }

       

        // If items were changed
        if (trigger == RecalculateStatsTrigger.ItemsChanged || trigger == RecalculateStatsTrigger.All)
        {

        }

        // if stat cards were changed
        if (trigger == RecalculateStatsTrigger.StatCardsChanged || trigger == RecalculateStatsTrigger.All)
        {

        }
    }

    public void OnDamageDealtAttempted(DamageInfo damageInfo, CharacterMaster victim)
    {
        if (damageInfo.damageType == DamageType.Melee)
        {
            if (isPlayer)
            {
                ApplySyncedHitstop(damageInfo.force, targetsHitThisAttack);
                UIManager.instance.tutorialManager.attackConnectedEvent.Invoke();
                if (damageInfo.launchType == DamageInfo.LaunchType.FixedLaunchUp || damageInfo.launchType == DamageInfo.LaunchType.Slam)
                {
                    UIManager.instance.tutorialManager.enemyLaunchedEvent.Invoke();
                }
            }

            storedBlockedDamageInstances.Clear();
        }
    }

    public void ApplyHitStop(float force, int targetsHitThisAttack)
    {
        // Calculate hit stop force from attack force and number of targets already hit this attack
        float forceMultiplier = (1f + (force - GameVariables.instance.hitStopBaseForceScaling) * (
            GameVariables.instance.hitStopAdditionalForceMultiplier / GameVariables.instance.hitStopBaseForceScaling))
            / Mathf.Max(targetsHitThisAttack, 1);

        hitStopMultiplier = GameVariables.instance.hitStopMinMultiplier;
        hitStopTimer = GameVariables.instance.hitStopTime * forceMultiplier;
        hitStopModifiedRecoveryTime = GameVariables.instance.hitStopRecoveryTime * forceMultiplier;
    }

    public void ApplySyncedHitstop(float force, int targetsHitThisAttack)
    {
        ApplyHitStop(force, targetsHitThisAttack);

        if (NetworkServer.active)
        {
            ClientRpcApplyHitstop(force, targetsHitThisAttack);
        }
        else
        {
            CmdApplyHitStop(force, targetsHitThisAttack);
        }
    }

    [Command]
    public void CmdApplyHitStop(float force, int targetsHitThisAttack)
    {
        ClientRpcApplyHitstop(force, targetsHitThisAttack);
    }

    [ClientRpc]
    public void ClientRpcApplyHitstop(float force, int targetsHitThisAttack)
    {
        if (!isLocalPlayer)
        {
            ApplyHitStop(force, targetsHitThisAttack);
        }
    }

    public void UpdateHitStop()
    {
        if (hitStopMultiplier < 1f)
        {
            if (hitStopTimer > 0)
            {
                hitStopTimer -= Time.deltaTime;
            }
            else
            {
                hitStopMultiplier += (1f - GameVariables.instance.hitStopMinMultiplier) / hitStopModifiedRecoveryTime * Time.deltaTime;
                hitStopMultiplier = Mathf.Min(1f, hitStopMultiplier);
            }
        }
    }

    public void OnDamageDealtConfirmed(DamageInfo damageInfo, CharacterMaster victim)
    {

        if (damageInfo.isPassiveDamage)
        {
            return;
        }
        onDamageDealtConfirmedEvent.Invoke(damageInfo, victim);
    }

    [Server]
    public void ModifyDamageTaken(DamageInfo damageInfo)
    {
        
    }

    [TargetRpc]
    public void TargetOnDamageTaken(NetworkConnection conn)
    {
        UIManager.instance.tutorialManager.hurtEvent.Invoke();

        if (isLocalPlayer && !characterHealth.invincible)
        {
            GameManager.instance.ScreenShake(0.1f, 1, 5);
            GameManager.instance.VignetteFlicker();
        }
    }

    [Server]
    public void OnDamageTaken(DamageInfo damageInfo)
    {
        if (isPlayer)
        {
            TargetOnDamageTaken(netIdentity.connectionToClient);
        }

        // Create damage number
        if (characterHealth.createDamageNumbers)
        {
            characterHealth.CmdCreateDamageNumber(damageInfo.WriteDamageInfo());
        }

        // Spawn all on hit effects
        for (int i = 0; i < onHitEffects.Length; i++)
        {
            EffectManager.CreateSimpleEffect(onHitEffects[i], damageInfo.hitLocation);
        }

        // Flash on hit
        RpcHitFlash();

        // Invoke on hit event - Doesn't work on clients
        onHit.Invoke(damageInfo);

        if (dead)
        {
            return;
        }

        if (damageInfo.attacker != null)
        {
            CharacterMaster attacker = damageInfo.attacker.GetComponent<CharacterMaster>();
        }

        if (damageInfo.isPassiveDamage)
        {
            return;
        }

        if (!knockdownImmunity)
        {
            // Decrease knockdown stability
            currentKnockdownStability -= damageInfo.force;
        }

        // If character has no stability left knock down
        if (currentKnockdownStability <= 0 && knockdownState != null)
        {
            // Check to set state via client or server
            NetworkConnection connectionToClient = netIdentity.connectionToClient;
            if (connectionToClient != null)
            {
                TargetKnockdown(netIdentity.connectionToClient);
            }
            else Knockdown();
            currentKnockdownStability = knockdownStability;
        }
        else // Check for stagger if not knocked down
        {
            // Set to default stagger state
            StaggerState resultingStagger = defaultStaggerState;
            int staggerIndex = -1;

            if (!hasHyperArmour)
            {
                // Check for additional staggers based on attack force
                for (int i = 0; i < additionalStaggerThresholds.Count; i++)
                {
                    if (damageInfo.force >= additionalStaggerThresholds[i].threshold)
                    {
                        resultingStagger = additionalStaggerThresholds[i].staggerState;
                        staggerIndex = i;
                        break;
                    }
                }
            }

            // If there is a valid stagger state stagger character
            if (resultingStagger != null && !hasHyperArmour)
            {
                // Check to set state via client or server
                NetworkConnection connectionToClient = netIdentity.connectionToClient;
                if (connectionToClient != null)
                {
                    TargetStagger(netIdentity.connectionToClient, damageInfo, staggerIndex);
                }
                else Stagger(damageInfo, staggerIndex);
            }
            else
            {
                // Play flinch animation if no stagger
                if (canFlinch)
                {
                    PlayAnimation("Flinch", 0f, "Flinch", 0f);
                }
            }
        }
    }

    [TargetRpc]
    public void TargetKnockdown(NetworkConnection target)
    {
        Knockdown();
    }

    public void Knockdown()
    {
        ActionState newState = Instantiate(knockdownState);
        actionStateMachine.SetNextState(newState);
    }

    [TargetRpc]
    public void TargetStagger(NetworkConnection target, DamageInfo damageInfo, int index)
    {
        Stagger(damageInfo, index);
    }

    public void Stagger(DamageInfo damageInfo, int index)
    {
        StaggerState stateToInstantiate = index >= 0 ? additionalStaggerThresholds[index].staggerState : defaultStaggerState;
        StaggerState newState = Instantiate(stateToInstantiate) as StaggerState;
        newState.damageInfo = damageInfo;
        actionStateMachine.SetNextState(newState);
    }

    public bool RollCrit()
    {
        return RollProc(weaponCritChance + criticalChance);
    }

    public bool RollProc(float chance, DamageInfo damageInfo = null)
    {
        if (damageInfo != null)
        {
            chance *= damageInfo.procMultiplier;
        }
        return Random.value < chance;
    }

    public void PlayAnimation(string animToPlay, float transitionLength, string layer, float offset)
    {
        if (animator == null)
        {
            return;
        }
        PlayAnimation(animToPlay, transitionLength, animator.GetLayerIndex(layer), offset);
    }

    //Plays animations
    public void PlayAnimation(string animToPlay, float transitionLength, int layer, float offset)
    {
        if (animator == null)
        {
            return;
        }

        //transitions between curently playing anims
        if (layer >= 0)
        {
            animator.CrossFade(animToPlay, transitionLength, layer, offset);
            CmdPlayAnimationSynced(animToPlay, transitionLength, layer, offset);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPlayAnimationSynced(string animtoplay, float transitionlength, int layer, float offset)
    {
        RpcPlayAnimationSynced(animtoplay, transitionlength, layer, offset);
    }

    [ClientRpc]
    public void RpcPlayAnimationSynced(string animtoplay, float transitionlength, int layer, float offset)
    {
        if (!isLocalPlayer)
        {
            animator.CrossFade(animtoplay, transitionlength, layer, offset);
        }
    }   

    // Automatically sorts the stagger thresholds
    public void OnValidate()
    {
        if (additionalStaggerThresholds.Count >= 2)
        {
            additionalStaggerThresholds.Sort((p1, p2) => p1.threshold.CompareTo(p2.threshold));
            additionalStaggerThresholds.Reverse();
        }
    }

    [ClientRpc]
    public void RpcHitFlash()
    {
        HitFlash();
    }

    public void HitFlash()
    {
        if (enabled)
        {
            StopAllCoroutines();
            StartCoroutine(HitFlashLerp());
        }
    }

    private IEnumerator HitFlashLerp()
    {
        float timer = 0;
        while (timer < hitFlashData.hitFlashLength)
        {
            timer += Time.deltaTime;
            timer = Mathf.Min(timer, hitFlashData.hitFlashLength);
            foreach (Material mat in characterMaterials)
            {
                mat.SetFloat("hitFlash", hitFlashData.hitFlashCurve.Evaluate(Mathf.InverseLerp(0, hitFlashData.hitFlashLength, timer)));
            }
            yield return null;
        }
    }

    // Gets number of buff type
    public int BuffCount(BuffDef buff)
    {
        return buffs[buff.assetID];
    }

    public bool HasBuff(BuffDef buff)
    {
        return BuffCount(buff) > 0;
    }

    public void AddBuff(BuffDef buff, int count = 1)
    {
        CmdAddBuff(buff.assetID, count);
    }

    public void RemoveBuff(BuffDef buff, int count = 1)
    {
        CmdRemoveBuff(buff.assetID, count);
    }

    // Decreases time on timed buffs

    [Server]
    private void UpdateBuffs()
    {
        foreach(int timedBuffStackKey in timedBuffStacks.Keys)
        {
            List<TimedBuff> timedBuffStack = timedBuffStacks[timedBuffStackKey];
            for (int i = 0; i < timedBuffStack.Count; i++)
            {
                timedBuffStack[i].timer -= Time.deltaTime;
                if (timedBuffStack[i].timer <= 0)
                {
                    RemoveBuff(timedBuffStack[i].buff, timedBuffStack[i].stacks);
                    timedBuffStack.RemoveAt(i);
                    i--;
                }
            }

            if (timedBuffStack.Count == 0)
            {
                timedBuffsToClear.Add(BuffManager.buffs[timedBuffStackKey]);
            }
        }

        foreach (BuffDef buffToClear in timedBuffsToClear)
        {
            ClearTimedBuffStacks(buffToClear);
        }
        timedBuffsToClear.Clear();

    }

    [Server]
    private void UpdateDots()
    {
        List<DotStack> stacksToDelete = null;
        foreach(DotStack dotStack in dotStacks.Values)
        {
            dotStack.tickTimer -= Time.deltaTime;
            if (dotStack.tickTimer <= 0)
            {
                DealDotStackDamage(dotStack);
                dotStack.tickTimer = dotStack.dotDef.tickInterval;
            }

            for (int i = 0; i < dotStack.dots.Count; i++)
            {
                Dot currentDot = dotStack.dots[i];
                currentDot.timer -= Time.deltaTime;
                if (currentDot.timer <= 0)
                {
                    RemoveBuff(dotStack.dotDef.associatedBuff, dotStack.dots[i].stacks);
                    dotStack.dots.RemoveAt(i);
                    i--;
                }


            }

            // Queue stack for deletion if there are none left
            if (dotStack.dots.Count == 0)
            {
                if (stacksToDelete == null)
                {
                    stacksToDelete = new List<DotStack>();
                }
                stacksToDelete.Add(dotStack);
            }

        }

        // Remove empty stacks
        if (stacksToDelete != null)
        {
            for (int i = 0; i < stacksToDelete.Count; i++)
            {
                dotStacks.Remove(stacksToDelete[i].dotDef.assetID);
            }
        }

    }

    [Server]
    private void DealDotStackDamage(DotStack dotStack)
    {
        Dictionary<int, List<Dot>> characterGroups = new Dictionary<int, List<Dot>>();
        for (int i = 0; i < dotStack.dots.Count; i++)
        {
            Dot currentDot = dotStack.dots[i];

            int key = -1;
            if (currentDot.inflictor != null)
            {
                key = currentDot.inflictor.GetInstanceID();
            }

            List<Dot> characterGroup;
            if (characterGroups.TryGetValue(key, out characterGroup))
            {
                characterGroup.Add(currentDot);
            }
            else
            {
                characterGroup = new List<Dot>();
                characterGroup.Add(currentDot);
                characterGroups.Add(key, characterGroup);
            }

        }

        foreach(List<Dot> characterGroup in characterGroups.Values)
        {
            float totalDamage = 0;
            DamageInfo damageInfo = null;
            for (int i = 0; i < characterGroup.Count; i++)
            {
                if (damageInfo == null)
                {
                    damageInfo = characterGroup[i].damage.Clone();
                }
                totalDamage += characterGroup[i].damage.damage * characterGroup[i].stacks;
            }
            damageInfo.damage = totalDamage;
            damageInfo.hitLocation = bodyCenter.position;
            characterHealth.DealDamage(damageInfo);
        }
    }

    [Command]
    public void CmdAddTimedBuff(int buffID, int stacks, float time, NetworkIdentity inflictor)
    {
        ServerAddTimedBuff(buffID, stacks, time, inflictor);
    }

    [Server]
    public void ServerAddTimedBuff(int buffID, int stacks, float time, NetworkIdentity inflictor)
    {
        BuffDef buff = BuffManager.buffs[buffID];
        
        if (buff.associatedDot == null)
        {
            TimedBuff newTimedBuff = new TimedBuff();
            newTimedBuff.buff = BuffManager.buffs[buffID];
            newTimedBuff.timer = time;
            newTimedBuff.stacks = stacks;

            if (!buff.stackable)
            {
                ClearTimedBuffStacks(buff, true);
            }

            List<TimedBuff> timedBuffStack;
            if (!timedBuffStacks.TryGetValue(buff.assetID, out timedBuffStack))
            {
                timedBuffStack = new List<TimedBuff>();
                timedBuffStacks.Add(buff.assetID, timedBuffStack);
            }



            if (buff.refreshAllTimersOnAddition && timedBuffStack.Count > 0)
            {
                timedBuffStack[0].stacks += stacks;
                timedBuffStack[0].timer = time;
            }
            else
            {
                timedBuffStack.Add(newTimedBuff);
            }


            AddBuff(BuffManager.buffs[buffID], stacks);
        }
        else
        {
            InflictDot(buff.associatedDot, time, inflictor == null ? null : inflictor.GetComponent<CharacterMaster>(), stacks);
        }
    }

    [Server]
    public void RefreshBuffTimers(BuffDef buffDef)
    {
        List<TimedBuff> timedBuffStacksList;
            
        if (timedBuffStacks.TryGetValue(buffDef.assetID, out timedBuffStacksList))
        {
            for (int i = 0; i < timedBuffStacksList.Count; i++)
            {
                timedBuffStacksList[i].timer = timedBuffStacksList[i].buff.defaultTime;
            }
        }
    }

    // Adds buff for duration
    public void AddTimedBuff(BuffDef buff, CharacterMaster inflictor = null, int stacks = 1, float? timeOverride = null)
    {
        float time = timeOverride == null ? buff.defaultTime : (float)timeOverride;

        NetworkIdentity inflictorId = null;
        if (inflictor != null)
        {
            inflictorId = inflictor.netIdentity;
        }

        if (NetworkServer.active)
        {
            ServerAddTimedBuff(buff.assetID, stacks, time, inflictorId);
        }
        else
        {
            CmdAddTimedBuff(buff.assetID, stacks, time, inflictorId);
        }
    }

    [Server]
    private void ServerBuffsChanged(BuffDef buff)
    {
        buff.onBuffChanged.Invoke(this);
        onBuffsChangedEvent.Invoke();
        RecalculateStatsBuffs();
    }

    // This stuff is handled automatically, just use the stuff above
    [Command(requiresAuthority = false)]
    private void CmdAddBuff(int buffID, int count)
    {
        if (BuffManager.buffs[buffID].stackable || !HasBuff(BuffManager.buffs[buffID]))
        {
            buffs[buffID] += count;
            SetSyncVarDirtyBit(1U);
            ServerBuffsChanged(BuffManager.buffs[buffID]);

            if (BuffManager.buffs[buffID].buffEffect != null)
            {
                RpcSpawnBuffEffect(buffID);
            }
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdRemoveBuff(int buffID, int count)
    {
        if (HasBuff(BuffManager.buffs[buffID]))
        {
            buffs[buffID] -= Mathf.Min(count, buffs[buffID]);
            SetSyncVarDirtyBit(1U);
            ServerBuffsChanged(BuffManager.buffs[buffID]);

            if (BuffManager.buffs[buffID].buffEffect != null && buffs[buffID] == 0)
            {
                RpcRemoveBuffEffect(buffID);
            }
        }
    }

    [Server]
    public void ClearTimedBuffStacks(BuffDef buff, bool keepList = false)
    {
        List<TimedBuff> timedBuffStack;
        if (timedBuffStacks.TryGetValue(buff.assetID, out timedBuffStack))
        {
            if (keepList)
            {
                timedBuffStack.Clear();
            }
            else
            {
                timedBuffStacks.Remove(buff.assetID);
            }
        }
    }

    [Server]
    private void InflictDot(DotDef dotDef, float time, CharacterMaster inflictor = null, int stacks = 1)
    {

        Dot dot = new Dot();
        dot.dotDef = dotDef;
        dot.timer = time;
        dot.inflictor = inflictor;
        dot.stacks = stacks;

        float damage = dot.dotDef.damagePerSecond * dot.dotDef.tickInterval;

        NetworkIdentity inflictorIdentity = null;
        if (inflictor != null)
        {

            inflictorIdentity = inflictor.netIdentity;
        }

        DamageInfo stackDamageInfo = new DamageInfo
        {
            damage = damage,
            isPassiveDamage = true,
            hitLocation = bodyCenter.position,
            damageType = dotDef.damageType,
            attacker = inflictorIdentity,
            colorFlags = dotDef.color,
            canCrit = false,
        };

        if (inflictor != null)
        {
            DamageComponent.CalculateDamageValues(ref stackDamageInfo, inflictor);
        }

        stackDamageInfo.damage += dot.dotDef.percent * characterHealth.maxHealth * dot.dotDef.tickInterval;


        dot.damage = stackDamageInfo;

        DotStack currentStack;
        if (!dotStacks.TryGetValue(dotDef.assetID, out currentStack))
        {
            currentStack = new DotStack();
            currentStack.dotDef = dotDef;
            dotStacks.Add(dotDef.assetID, currentStack);
        }

        if (!dotDef.associatedBuff.stackable)
        {
            currentStack.dots.Clear();
        }

        currentStack.dots.Add(dot);
        AddBuff(dotDef.associatedBuff, stacks);
    }

    [ClientRpc]
    public void RpcSpawnBuffEffect(int buffID)
    {
        if (buffEffects[buffID] == null)
        {
            GameObject buffEffect = Instantiate(BuffManager.buffs[buffID].buffEffect, bodyCenter.position, Quaternion.identity);
            buffEffect.transform.localScale = buffEffect.transform.localScale * buffEffectScalar;
            buffEffect.transform.SetParent(bodyCenter, true);
            buffEffects[buffID] = buffEffect;
        }
    }

    [ClientRpc]
    public void RpcRemoveBuffEffect(int buffID)
    {
        if (buffEffects[buffID] != null)
        {
            buffEffects[buffID].transform.SetParent(null, true);
            var particleDetacher = buffEffects[buffID].GetComponent<ParticleDetacher>();
            if (particleDetacher != null)
            {
                particleDetacher.Detach();
            }
            Destroy(buffEffects[buffID]);
            buffEffects[buffID] = null;
        }
    }


    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        base.OnSerialize(writer, initialState);
        writer.WriteArray(buffs);
        return true;
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        base.OnDeserialize(reader, initialState);
        buffs = reader.ReadArray<int>();
        onBuffsChangedEvent.Invoke();
    }

    public void SetUpRightLerp(float a, float b, float time)
    {
        StartCoroutine(UprightLerp(a, b, time));
    }

    IEnumerator UprightLerp(float a, float b, float totalTime)
    {
        float timer = 0f;
        while (timer < totalTime)
        {
            float lerp = Mathf.Lerp(a, b, timer / totalTime);
            animator.SetFloat("Upright", lerp);
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
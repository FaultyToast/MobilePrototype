using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cinemachine;
using Mirror;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
using System;

public class GameManager : NetworkBehaviour
{
    private static GameManager _instance;

    public static GameManager instance
    {
        get
        {
            return _instance;
        }
    }

    // Component References
    public Camera mainCamera;
    //public CinemachineFreeLook cineMachineCamera;
    private ScreenShaker screenShaker;
    private bool gameExiting = false;

    [System.NonSerialized] public GameObject playerObject;
    [System.NonSerialized] public CharacterMaster localPlayerCharacter;

    [System.NonSerialized] public bool isInCombat;

    // Global game variables
    public GameVariables gameVariables;

    public PostProcessingController postProcessingController;

    [SyncVar]
    public float killPlaneYPosition = -Mathf.Infinity;

    public Dictionary<int, CharacterMaster> enemies = new Dictionary<int, CharacterMaster>();

    private Dictionary<int, NetworkIdentity> livingPlayersDictionary = new Dictionary<int, NetworkIdentity>();
    public List<NetworkIdentity> livingPlayers = new List<NetworkIdentity>();

    [System.NonSerialized] public UnityEvent onPlayerDeath = new UnityEvent();
    [System.NonSerialized] public UnityEvent onLivingPlayerListChanged = new UnityEvent();
    public static UnityEvent onGameInitialized = new UnityEvent();

    // The floor def that the player is currently in, not the one recognized by the roombuilder
    [SyncVar]
    [System.NonSerialized] public string currentFloorName;

    // Experience

    //[SyncVar(hook = nameof(UpdateExperience))]
    [System.NonSerialized] public float playerExperience = 0;

    [System.NonSerialized]
    public int fractureLevel = 0;

    // DEBUG
    private int debugBonusSkillPoints = 0;

    [System.NonSerialized] public readonly float expPerLevel = 750f;
    private int _skillPointsSpent = 0;
    public int skillPointsSpent
    {
        get
        {
            return _skillPointsSpent;
        }
        set
        {
            _skillPointsSpent = value;
            //UpdateSkillPointCounter();
        }
    }
    public int totalSkillPoints
    {
        get
        {
            return Mathf.FloorToInt(playerExperience / expPerLevel) + debugBonusSkillPoints;
        }
    }

    public int playerLevel
    {
        get
        {
            return skillPointsSpent;
        }
    }

    [SyncVar(hook = nameof(SetGlobalLevel))]
    private int _globalLevel = 1;
    public int globalLevel
    {
        get
        {
            return _globalLevel;
        }
        set
        {
            _globalLevel = value;
        }
    }

    public void SetGlobalLevel(int oldGlobalLevel, int newGlobalLevel)
    {
        UIManager.instance.objectiveTracker.globalLevelTracker.text = globalLevel.ToString();
    }

    public bool inMenu = false;
    public bool cursorUsed
    {
        get
        {
            return InputManager.instance.isInTabMode || inMenu;
        }
    }


    public List<Menu> openMenus = new List<Menu>();


    void Awake()
    {
        _instance = this;

        if (gameVariables == null)
        {
            Debug.LogError("No GameVariables has been assigned to the GameManager");
        }
        GameVariables.instance = gameVariables;


    }

    private void Start()
    {
        globalLevel = globalLevel;
        //screenShaker = cineMachineCamera.GetComponent<ScreenShaker>();
    }

    private void OnDestroy()
    {
        onGameInitialized.RemoveAllListeners();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartCoroutine(DelayedInitialize());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!NetworkServer.active)
        {
            StartCoroutine(DelayedInitialize());
        }

        //UpdateExperience(0, 0);
        //SetGlobalLevel(0, 0);
    }

    private bool gameInitialized = false;
    public IEnumerator DelayedInitialize()
    {
        yield return null;
        gameInitialized = true;
        onGameInitialized.Invoke();
    }

    public void MenuOpened(Menu menu)
    {
        openMenus.Add(menu);
        UpdateMenuCount();
    }

    public void PostClientSetup()
    {
    }

    public void MenuClosed(Menu menu)
    {
        openMenus.Remove(menu);
        UpdateMenuCount();
    }

    public void UpdateMenuCount()
    {
        if (openMenus.Count > 0)
        {
            inMenu = true;
        }
        else
        {
            inMenu = false;
        }

        UIManager.instance.toolTip.HideToolTip();

        InputManager.instance.UpdateCursorVisibility();
    }

    public void QueueForGameInitialize(UnityAction action)
    {
        if (gameInitialized)
        {
            action.Invoke();
        }
        else onGameInitialized.AddListener(action);
    }

    public void Update()
    {
        if (InputManager.playerControls.Player.Interact.WasPressedThisFrame())
        {
            if (currentInteractables.Count > 0)
            {
                currentInteractables[0].Interact();
            }
        }

        // Update managers
        HomingOrbManager.instance.Update();
    }

    [System.NonSerialized] public bool destroyingAllEnemies;
    public void KillAllEnemies()
    {
        destroyingAllEnemies = true;
        foreach (CharacterMaster enemy in enemies.Values)
        {
            enemy.ServerDeath(null, false);
        }
        enemies.Clear();
        destroyingAllEnemies = false;
    }

    //public float EXPForLevel(int level)
    //{
    //    return level * expPerLevel;
    //}
    //
    //[Server]
    //public void GainExperience(float experience)
    //{
    //    float addedExperience = experience / (1 + gameVariables.baseExpScalar * Mathf.Pow(totalSkillPoints, gameVariables.expScalingExponent));
    //    playerExperience += addedExperience;
    //}
    //
    //public void UpdateExperience(float oldExperience, float newExperience)
    //{
    //    if (Mathf.FloorToInt(newExperience / expPerLevel) > Mathf.FloorToInt(oldExperience / expPerLevel))
    //    {
    //        UIManager.instance.tutorialManager.levelUpEvent.Invoke();
    //    }
    //    float percentToNextLevel = (playerExperience % expPerLevel) / expPerLevel;
    //    UIManager.instance.expBar.SetPercent(percentToNextLevel, percentToNextLevel, null);
    //    UpdateSkillPointCounter();
    //}

    //public void UpdateSkillPointCounter()
    //{
    //    UIManager.instance.skillPointCounter.text = (totalSkillPoints - skillPointsSpent).ToString();
    //}
    //
    //[ClientRpc]
    //public void RpcRoomEntered()
    //{
    //    UIManager.instance.tutorialManager.roomEnteredEvent.Invoke();
    //}

    [HideInInspector] public List<Interactable> currentInteractables = new List<Interactable>();
    public void InteractablesChanged()
    {
        if (UIManager.instance.interactPrompt != null)
        {
            if (currentInteractables.Count > 0)
            {
                UIManager.instance.interactPrompt.text.enabled = true;
                UIManager.instance.interactPrompt.promptText = currentInteractables[0].interactText;
            }
            else
            {
                UIManager.instance.interactPrompt.text.enabled = false;
            }
        }
    }

    public void RegisterEnemy(CharacterMaster characterMaster)
    {
        enemies.TryAdd(characterMaster.GetInstanceID(), characterMaster);
    }


    public void DeregisterEnemy(CharacterMaster characterMaster)
    {
        if (!destroyingAllEnemies)
        {
            enemies.Remove(characterMaster.GetInstanceID());
        }
    }

    public void SpawnProjectile(ProjectileSpawnInfo spawnInfo)
    {
        // Command on client, direct on server
        if (!NetworkServer.active)
        {
            CmdSpawnProjectile(spawnInfo);
        }
        else
        {
            ServerSpawnReferencedProjectile(spawnInfo);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnProjectile(ProjectileSpawnInfo spawnInfo)
    {
        ServerSpawnReferencedProjectile(spawnInfo);
    }

    [Server]
    public GameObject ServerSpawnReferencedProjectile(ProjectileSpawnInfo spawnInfo)
    {
        if (spawnInfo.owner == null)
        {
            /*
            Debug.LogWarning("Trying to spawn a projectile with no owner : " + ProjectileManager.GetProjectileFromID(spawnInfo.projectileID) + "\n" +
                "Make sure you set OwnerOverride if you are not spawning from an ActionState");
            */
        }
        Vector3 spawnPosition = spawnInfo.spawnPosition;
        if (spawnInfo.isLocal)
        {
            Transform parentPoint;
            if (spawnInfo.parentTransformLocator == "ROOT")
            {
                parentPoint = (spawnInfo.parentOverride == null ? spawnInfo.owner.transform : spawnInfo.parentOverride.transform);
            }
            else
            {
                parentPoint = (spawnInfo.parentOverride == null ? spawnInfo.owner : spawnInfo.parentOverride).GetComponent<ChildLocator>().GetChild(spawnInfo.parentTransformLocator);
            }

            spawnPosition += parentPoint.transform.position;
        }
        ProjectileMaster projectile = Instantiate(ProjectileManager.GetProjectileFromID(spawnInfo.projectileID), spawnPosition, spawnInfo.spawnRotation);
        projectile.owner = spawnInfo.owner;
        projectile.parentLocator = spawnInfo.parentTransformLocator;
        projectile.isLocal = spawnInfo.isLocal;
        projectile.localOffset = spawnInfo.spawnPosition;
        NetworkIdentity whatTheFuck = spawnInfo.parentOverride;
        projectile.genericFloat = spawnInfo.genericFloat;
        projectile.parentOverride = whatTheFuck;
        NetworkServer.Spawn(projectile.gameObject, spawnInfo.owner == null ? null : spawnInfo.owner.connectionToClient);
        return projectile.gameObject;
    }

    [Command(requiresAuthority = false)]
    public void UnspawnProjectile(GameObject projectile)
    {
        NetworkServer.Destroy(projectile);
    }

    // Gets the first living player with no particular order
    public CharacterMaster GetLivingPlayer()
    {
        if (livingPlayers.Count > 0)
        {
            foreach (NetworkIdentity player in livingPlayers)
            {
                return player.GetComponent<CharacterMaster>();
            }
        }
        return null;
    }


    [Server]
    public void AddLivingPlayer(NetworkIdentity playerIdentity, int connectionId)
    {
        if (livingPlayersDictionary.TryAdd(connectionId, playerIdentity))
        {
            SyncLivingPlayers();
        }
    }

    [Server]
    public void RemoveLivingPlayer(int connectionId)
    {
        if (livingPlayersDictionary.Remove(connectionId))
        {
            SyncLivingPlayers(true);
        }
    }

    [Server]
    public void SyncLivingPlayers(bool playerDied = false)
    {
        NetworkIdentity[] livingPlayersArray = new NetworkIdentity[livingPlayersDictionary.Count];

        int i = 0;
        foreach (NetworkIdentity player in livingPlayersDictionary.Values)
        {
            livingPlayersArray[i] = player;
            i++;
        }

        if (livingPlayersDictionary.Count == 0)
        {
            GameOver();
        }

        RpcSyncLivingPlayers(livingPlayersArray, playerDied);
    }

    [ClientRpc]
    public void RpcSyncLivingPlayers(NetworkIdentity[] livingPlayers, bool playerDied)
    {
        if (!NetworkServer.active)
        {
            if (livingPlayers.Length == 0)
            {
                GameOver();
            }
        }
        this.livingPlayers = new List<NetworkIdentity>(livingPlayers);
        if (playerDied)
        {
            onPlayerDeath.Invoke();
        }
        onLivingPlayerListChanged.Invoke();
    }

    [Server]
    public void ReviveAllPlayers()
    {
        foreach (NetworkConnectionToClient playerConnection in NetworkServer.connections.Values)
        {
            if (!playerConnection.identity.gameObject.activeSelf)
            {
                RevivePlayer(playerConnection.identity);
            }
        }
    }

    public void GameOver()
    {
        FracturedNetworkManager.instance.ExitGame();
    }


    [Server]
    public void RevivePlayer(NetworkIdentity playerIdentity)
    {
        CharacterMaster characterMaster = playerIdentity.GetComponent<CharacterMaster>();
        characterMaster.characterMovement.TargetTeleport(playerIdentity.connectionToClient, NetworkManager.singleton.GetStartPosition().position);
        characterMaster.ServerRevive();
    }

    public void ScreenShake(float time, float amplitude, float frequency)
    {
        screenShaker.StartScreenShake(time, amplitude, frequency);
    }


    public void ScreenShake(float time, float amplitude, float frequency, Vector3 source, float distance)
    {
        float DistanceToSource = Vector3.Distance(NetworkClient.localPlayer.transform.position, source);
        if (DistanceToSource < distance)
        {
            screenShaker.StartScreenShake(time, amplitude, frequency);
        }
    }


    //[ClientRpc]
    //public void RpcSetGlobalLevel(int globalLevel)
    //{
    //    UIManager.instance.objectiveTracker.globalLevelTracker.text = globalLevel.ToString();
    //}

    [ClientRpc]
    public void RpcSetSubObjective(string objective)
    {
        UIManager.instance.objectiveTracker.SetSubObjective(objective);
    }

    [ClientRpc]
    public void RpcSetObjective(string objective)
    {
        UIManager.instance.objectiveTracker.SetObjective(objective);
    }

    [ClientRpc]
    public void RpcClearObjective()
    {
        UIManager.instance.objectiveTracker.ClearObjective();
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnDebugEnemy()
    {
        EnemySpawner.instance.DebugSpawnEnemy();
    }

    public void VignetteFlicker()
    {
        postProcessingController.DamageVignette();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FracturedNetworkManager : NetworkManager
{
    public static FracturedNetworkManager instance
    {
        get
        {
            return singleton as FracturedNetworkManager;
        }
    }

    public static int playerCount
    {
        get
        {
            return Mathf.Max(1, singleton.numPlayers);
            
        }
    }

    public static UnityEvent onStartClient = new UnityEvent();
    public static UnityEvent onStartServer = new UnityEvent();

    public static UnityEvent<NetworkConnection> onClientConnect = new UnityEvent<NetworkConnection>();
    public static UnityEvent<NetworkIdentity> onPlayerSpawned = new UnityEvent<NetworkIdentity>();

    public List<Color> playerColors;
    private Color defaultColor;

    [Header("Lobby info")]
    private bool isLobby;

    [Scene]
    public string runScene;

    public GameObject lobbyPlayer;

    public Dictionary<int, PlayerInfo> assignedPlayerInfo = new Dictionary<int, PlayerInfo>();

    public static List<GameObject> prefabsToRegister;
    [System.NonSerialized] public List<CharacterMaster> playerMasters = new List<CharacterMaster>();

    public bool initialSceneChangeFade = true;

    [System.NonSerialized] public bool fadeScenes = true;

    private bool gameExiting = false;
    private bool disconnecting = false;


    public override void Awake()
    {
        base.Awake();
#if UNITY_EDITOR
        {
            GetComponent<kcp2k.KcpTransport>().Timeout = 99999999;
        }
#endif
    }

    public override void Start()
    {
        base.Start();
        NetworkServer.dontListen = false;
    }


    public override void OnStartClient()
    {
        base.OnStartClient();
        RegisterAllPrefabsToNetwork();
        NetworkClient.RegisterPrefab(lobbyPlayer);
        onStartClient.Invoke();
        NetworkClient.RegisterHandler<ShutdownMessage>(RecieveShutdownMessage);

        DontDestroyOnLoad(gameObject);
    }

    public void Disconnect()
    {
        disconnecting = true;
        // stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            StopHost();
        }
        // stop client if client-only
        else if (NetworkClient.active)
        {
            StopClient();
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            StopServer();
        }
    }

    public struct ShutdownMessage : NetworkMessage { };

    public void ExitGame()
    {
        if (gameExiting)
        {
            return;
        }
        gameExiting = true;

        fadeScenes = false;
        if (NetworkServer.active)
        {
            foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
            {
                connection.Value.Send(new ShutdownMessage());
            }
        }

        ScreenFader.instance.FadeOut(Disconnect);
    }

    public void RecieveShutdownMessage(ShutdownMessage message)
    {
        if (gameExiting)
        {
            return;
        }
        fadeScenes = false;
        gameExiting = true;

        ScreenFader.instance.FadeOut(Disconnect);
    }

    public void RegisterAllPrefabsToNetwork()
    {
        for (int i = 0; i < prefabsToRegister.Count; i++)
        {
            NetworkClient.RegisterPrefab(prefabsToRegister[i]);
        }
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        GameObject player;
        if (!IsInLobby())
        {
            Transform startPos = GetStartPosition();

            player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);

        }
        else
        {
            player = Instantiate(lobbyPlayer);
        }

        OnPlayerAdded(player, conn);

        // instantiating a "Player" prefab gives it the name "Player(clone)"
        // => appending the connectionId is WAY more useful for debugging!
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public bool IsInLobby()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.name == "Lobby";
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        DontDestroyOnLoad(gameObject);

        if (playerColors.Count > 0)
        {
            defaultColor = playerColors[0];
        }

        onStartServer.Invoke();
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        AddPlayerInfo(conn);
        if (playerColors.Count > 0)
        {
            AssignPlayerColor(conn, playerColors[0]);
            playerColors.RemoveAt(0);
        }
        else
        {
            AssignPlayerColor(conn, defaultColor);
        }
        onClientConnect.Invoke(conn);
    }

    public void AddPlayerInfo(NetworkConnection conn)
    {
        PlayerInfo newPlayerInfo = new PlayerInfo();
        newPlayerInfo.joinTime = Time.realtimeSinceStartup;
        assignedPlayerInfo.TryAdd(conn.connectionId, newPlayerInfo);
        AssignPlayerIndexes();
    }

    public void RemovePlayerInfo(NetworkConnection conn)
    {
        PlayerInfo playerInfoToRemove = assignedPlayerInfo[conn.connectionId];
        playerColors.Insert(0, playerInfoToRemove.color);
        assignedPlayerInfo.Remove(conn.connectionId);
        AssignPlayerIndexes();
    }
    public void AssignPlayerIndexes()
    {
        if (disconnecting)
        {
            return;
        }
        List<PlayerInfo> playerInfoList = assignedPlayerInfo.Values.ToList();

        playerInfoList.Sort((p1, p2) => p1.joinTime.CompareTo(p2.joinTime));

        for (int i = 0; i < playerInfoList.Count; i++)
        {
            playerInfoList[i].index = i + 1;
        }
    }

    public Color GetPlayerColor(NetworkConnection conn)
    {
        return assignedPlayerInfo[conn.connectionId].color;
    }

    public void AssignPlayerColor(NetworkConnection conn, Color color)
    {
        assignedPlayerInfo[conn.connectionId].color = color;
    }

    public override void OnPlayerAdded(GameObject player, NetworkConnection conn)
    {
        
    }

    public void OnPlayerSpawned(NetworkIdentity playerIdentity)
    {
        CharacterMaster playerMaster = playerIdentity.GetComponent<CharacterMaster>();
        if (playerMaster != null)
        {
            playerMasters.Add(playerMaster);
            if (playerIdentity.isLocalPlayer)
            {
                GameManager.instance.localPlayerCharacter = playerMaster;
            }
        }
        onPlayerSpawned.Invoke(playerIdentity);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        RemovePlayerInfo(conn);
        if (conn.identity == null)
        {
            return;
        }
        CharacterMaster playerMaster = conn.identity.GetComponent<CharacterMaster>();
        if (playerMaster != null)
        {
            playerMasters.Remove(playerMaster);
        }
    }

    public static void ReplaceStartPosition(Transform newStartPosition)
    {
        startPositions.Clear();
        RegisterStartPosition(newStartPosition);
    }

    public static void TeleportPlayers(Vector3 position, uint[] excludes)
    {
        List<uint> excludeList = new List<uint>(excludes);
        foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
        {
            NetworkIdentity identity = connection.Value.identity;
            if (!excludeList.Contains(identity.netId))
            {
                identity.GetComponent<CharacterMovement>().TargetTeleport(connection.Value, position);
            }
        }
    }

    public void SpawnAsset()
    {

    }

    public override void OnServerChangeScene(string newSceneName)
    {
        OnChangeScene(newSceneName);
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        OnChangeScene(newSceneName);
    }

    public void OnChangeScene(string newSceneName)
    {
        if (newSceneName == "MainMenu")
        {
            DestroyOnLoad();
        }
    }

    public override void ServerChangeScene(string newSceneName)
    {
        if (string.IsNullOrWhiteSpace(newSceneName))
        {
            Debug.LogError("ServerChangeScene empty scene name");
            return;
        }

        if (NetworkServer.isLoadingScene && newSceneName == networkSceneName)
        {
            Debug.LogError($"Scene change is already in progress for {newSceneName}");
            return;
        }

        // Debug.Log($"ServerChangeScene {newSceneName}");
        NetworkServer.SetAllClientsNotReady();
        networkSceneName = newSceneName;

        // Let server prepare for scene change
        OnServerChangeScene(newSceneName);

        // set server flag to stop processing messages while changing scenes
        // it will be re-enabled in FinishLoadScene.
        NetworkServer.isLoadingScene = true;

        isLoadingScene = true;

        if (fadeScenes)
        {
            ScreenFader.instance.FadeToSceneAsync(newSceneName, async => {
                loadingSceneAsync = async;
            });
        }
        else
        {
            loadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
        }

        // ServerChangeScene can be called when stopping the server
        // when this happens the server is not active so does not need to tell clients about the change
        if (NetworkServer.active)
        {
            // notify all clients about the new scene
            NetworkServer.SendToAll(new SceneMessage { sceneName = newSceneName });
        }

        startPositionIndex = 0;
        startPositions.Clear();
    }

    public override void ClientLoadSceneOperation(string newSceneName)
    {
        isLoadingScene = true;
        if (fadeScenes)
        {
            ScreenFader.instance.FadeToSceneAsync(newSceneName, async => {
                loadingSceneAsync = async;
            });
        }
        else
        {
            loadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
        }
    }

    public void StartSinglePlayer()
    {
        NetworkServer.dontListen = true;
        onlineScene = runScene;
        StartHost();
    }

    public void DestroyOnLoad()
    {
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
    }
}

public class PlayerInfo
{
    public Color color;
    public int index;
    public float joinTime;
}

public static class PlayerInfoExtensions
{
    public static void WritePlayerInfo(this NetworkWriter writer, PlayerInfo value)
    {
        writer.WriteBool(value == null);
        if (value == null)
        {
            return;
        }
        writer.WriteColor(value.color);
        writer.WriteInt(value.index);
    }

    public static PlayerInfo ReadPlayerInfo(this NetworkReader reader)
    {
        if (reader.ReadBool())
        {
            return null;
        }
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.color = reader.ReadColor();
        playerInfo.index = reader.ReadInt();
        return playerInfo;
    }
}

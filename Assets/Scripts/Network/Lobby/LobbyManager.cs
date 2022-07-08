using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LobbyManager : NetworkBehaviour
{
    [Scene]
    public string runScene;

    public static LobbyManager instance;

    [SerializeField] private List<Transform> playerDisplayPoints;
    [System.NonSerialized] public List<Transform> availablePlayerDisplayPoints;

    public ControllerMenu clientMenu;
    public ControllerMenu hostMenu;

    public GameObject startGameButton;

    private ControllerMenu menu;

    public void Awake()
    {
        instance = this;

        availablePlayerDisplayPoints = new List<Transform>(playerDisplayPoints);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!NetworkServer.active)
        {
            startGameButton.SetActive(false);
            menu = clientMenu;
            clientMenu.gameObject.SetActive(true);
        }
        else
        {
            menu = hostMenu;
            hostMenu.gameObject.SetActive(true);
        }
    }

    public void StartGame()
    {
        menu.gameObject.SetActive(false);
        NetworkManager.singleton.ServerChangeScene(runScene);
    }

    public void Leave()
    {

        FracturedNetworkManager.instance.ExitGame();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class MainMenu : MonoBehaviour
{
    public ControllerMenu mainControllerMenu;
    public MainMenuCameraLocation mainMenuCameraLocation;

    public MainMenuCameraLocation settingsCameraLocation;
    public MainMenuCameraLocation multiplayerCameraLocation;

    private MainMenuCameraLocation currentCameraLocation;

    public GameObject multiplayerSubMenu;
    public GameObject connectingSubMenu;

    private bool waitingOnConnection = false;
    public TextMeshProUGUI ipDisplay;

    public TMP_InputField inputFieldTest;

    public GameObject pauseMenu;

    [System.Serializable]
    public class MainMenuCameraLocation
    {
        public GameObject menuObject;
        public Transform cameraTransform;
    }

    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.localPosition = Vector3.zero;
        Camera.main.transform.localRotation = Quaternion.identity;
        SetCameraLocation(mainMenuCameraLocation);

        HandleResultsScreen();
    }

    public void HandleResultsScreen()
    {
        /*
        List<PlayerStatTracker.PlayerStats> fakeStats = new List<PlayerStatTracker.PlayerStats>();
        fakeStats.Add(new PlayerStatTracker.PlayerStats());
        fakeStats.Add(new PlayerStatTracker.PlayerStats());
        fakeStats.Add(new PlayerStatTracker.PlayerStats());

        ResultsScreen.ResultsScreenInfo fakeInfo = new ResultsScreen.ResultsScreenInfo { teamStats = fakeStats, localStats = new PlayerStatTracker.PlayerStats() };

        ResultsScreen.queuedResultsScreenInfo = fakeInfo;
        */

       
    }

    public void LeaveResultsScreen()
    {
        mainMenuCameraLocation.menuObject.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        mainControllerMenu.Deactivate();
        ScreenFader.instance.FadeToSceneAsync("SinglePlayerLobby", null);
    }

    public void FadeToSettings()
    {
        FadeToCameraLocation(settingsCameraLocation);
    }

    public void FadeToMultiplayer()
    {
        FadeToCameraLocation(multiplayerCameraLocation);
    }
    
    public void FadeToCameraLocation(MainMenuCameraLocation cameraLocation)
    {
        ScreenFader.instance.FadeInOut(delegate { SetCameraLocation(cameraLocation); });
    }

    public void FadeToTitleScreen()
    {
        FadeToCameraLocation(mainMenuCameraLocation);
    }

    public void StartSinglePlayer()
    {
        FracturedNetworkManager.instance.StartSinglePlayer();
    }

    public void SetCameraLocation(MainMenuCameraLocation cameraLocation)
    {
        if (currentCameraLocation != null)
        {
            currentCameraLocation.menuObject.SetActive(false);
        }
        currentCameraLocation = cameraLocation;
        currentCameraLocation.menuObject.SetActive(true);
        Camera.main.transform.SetParent(currentCameraLocation.cameraTransform, false);
    }

    public void JoinGame()
    {
        string text = inputFieldTest.text;

        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
        {
            NetworkManager.singleton.networkAddress = "localhost";
        }
        else
        {
            NetworkManager.singleton.networkAddress = inputFieldTest.text;
        }

        NetworkManager.singleton.StartClient();
        ipDisplay.text = FracturedNetworkManager.singleton.networkAddress;
        connectingSubMenu.SetActive(true);
        multiplayerSubMenu.SetActive(false);
        waitingOnConnection = true;
    }

    public void StartHost()
    {
        FracturedNetworkManager.singleton.StartHost();
    }

    public void CancelConnection()
    {
        FracturedNetworkManager.singleton.StopClient();
    }

    public void Update()
    {
        if (waitingOnConnection)
        {
            if (!NetworkClient.active)
            {
                connectingSubMenu.SetActive(false);
                multiplayerSubMenu.SetActive(true);
                waitingOnConnection = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMenu.GetComponent<PauseMenu>().Toggle();
        }
    }
}

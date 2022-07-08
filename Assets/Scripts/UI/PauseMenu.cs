using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PauseMenu : Menu
{
    public GameObject settingsMenu;
    public GameObject mainMenu;
    private bool open = false;

    protected override void MenuOpened()
    {
        open = true;
        gameObject.SetActive(true);
        ReturnToMain();
        if (NetworkServer.active && NetworkServer.dontListen)
        {
            Time.timeScale = 0f;
        }
    }

    protected override void MenuClosed()
    {
        open = false;
        ReturnToMain();
        gameObject.SetActive(false);
        if (NetworkServer.active && NetworkServer.dontListen)
        {
            Time.timeScale = 1f;
        }
    }

    public void ReturnToMain()
    {
        mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
    }

    public void GoToSettings()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
        Canvas.ForceUpdateCanvases();
    }

    public void Leave()
    {
        openable = false;
        Close();

        FracturedNetworkManager.instance.ExitGame();
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCamController : MonoBehaviour
{
    public GameObject cameraPrefab;
    public Camera mainCamera;
    public Camera freeCamera;

    bool freeCamActive = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if(!freeCamActive)
            {
                if (freeCamera == null)
                    CreateFreeCam();
                else
                    ActivateFreeCam();
            }
            else
            {
                DeactivateFreeCam();
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            PauseThings();
        }
    }

    public void CreateFreeCam()
    {
        //Create new free cam and set position
        mainCamera = Camera.main;
        freeCamera = Instantiate(cameraPrefab).GetComponent<Camera>();
        ActivateFreeCam();
    }

    public void ActivateFreeCam()
    {
        freeCamActive = true;
        freeCamera.transform.position = mainCamera.transform.position;
        freeCamera.transform.rotation = mainCamera.transform.rotation;

        //Set Freecam As Active Camera
        freeCamera.targetDisplay = 0;
        mainCamera.targetDisplay = 1;

        //GameManager.instance.inFreeCam = true;
        //InputManager.instance.UpdateCursorVisibility();
        //Cursor.visible = false;
    }

    public void DeactivateFreeCam()
    {
        freeCamActive = false;
        freeCamera.targetDisplay = 1;
        mainCamera.targetDisplay = 0;

        //GameManager.instance.inFreeCam = false;
        //InputManager.instance.UpdateCursorVisibility();
    }

    public void PauseThings()
    {
        if(Time.timeScale == 1)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Menu : MonoBehaviour
{
    public GameObject menuObject;
    public bool closeWithBackButton = true;
    [System.NonSerialized] public bool isOpen = false;

    public UnityEvent onMenuOpened = new UnityEvent();
    public UnityEvent onMenuClosed = new UnityEvent();

    private bool wasOpenedThisFrame = false;
    private bool wasClosedThisFrame = false;
    protected bool openable = true;

    public virtual void Awake()
    {
        if (!isOpen)
        {
            menuObject.SetActive(false);
        }
    }

    public virtual void Start()
    {
        
    }

    public void OnDisable()
    {
        wasOpenedThisFrame = false;
        wasClosedThisFrame = false;
    }

    public void Open()
    {
        if (!openable)
        {
            return;
        }
        if (!isOpen && (!wasClosedThisFrame || !enabled))
        {
            isOpen = true;
            wasOpenedThisFrame = true;

            if (menuObject != null)
            {
                menuObject.SetActive(true);
            }

            MenuOpened();
            GameManager.instance.MenuOpened(this);
            onMenuOpened.Invoke();

        }

    }

    public void LateUpdate()
    {
        wasOpenedThisFrame = false;
        wasClosedThisFrame = false;
    }

    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else Open();
    }

    public void Close()
    {
        if (isOpen && (!wasOpenedThisFrame || !enabled))
        {
            isOpen = false;
            wasClosedThisFrame = true;
            if (menuObject != null)
            {
                menuObject.SetActive(false);
            }



            MenuClosed();
            GameManager.instance.MenuClosed(this);
            onMenuClosed.Invoke();
        }
    }

    public virtual void Update()
    {
        if (closeWithBackButton && InputManager.playerControls.Player.Cancel.WasPressedThisFrame())
        {
            Close();
        }
    }

    protected virtual void MenuOpened()
    {

    }

    protected virtual void MenuClosed()
    {

    }
}

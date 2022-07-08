using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    public UnityEvent onInteracted;
    public string interactText;

    private bool registered = false;

    private bool _active = true;
    public bool active
    {
        set
        {
            _active = value;
            if (!_active)
            {
                DeRegister();
            }
        }
        get
        {
            return _active;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (NetworkClient.isConnected)
        {
            if (active && !registered)
            {
                if (ReferenceEquals(other.gameObject, NetworkClient.localPlayer.gameObject))
                {
                    Register();
                }
            }
        }

    }

    public void OnTriggerExit(Collider other)
    {
        if (NetworkClient.isConnected)
        {
            if (ReferenceEquals(other.gameObject, NetworkClient.localPlayer.gameObject))
            {
                DeRegister();
            }
        }

    }

    public void OnDisable()
    {
        DeRegister();
    }

    public void Register()
    {
        if (GameManager.instance == null)
        {
            return;
        }
        GameManager.instance.currentInteractables.Add(this);
        GameManager.instance.InteractablesChanged();
        registered = true;
    }

    public void DeRegister()
    {
        if (GameManager.instance == null)
        {
            return;
        }
        GameManager.instance.currentInteractables.Remove(this);
        GameManager.instance.InteractablesChanged();
        registered = false;
    }

    public void Interact()
    {
        onInteracted.Invoke();
    }
}
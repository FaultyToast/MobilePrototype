using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class MenuTickBox : MenuButton
{
    public TextMeshProUGUI text;
    public string label;

    [System.NonSerialized] public bool value;

    public UnityAction<bool> OnValueChanged;

    public GameObject selectionObject;

    public void Start()
    {
        selectionObject.SetActive(false);
        ValueUpdated();
    }

    public override void NavigateLeft()
    {
        base.NavigateLeft();
        OnPress();
    }

    public override void NavigateRight()
    {
        base.NavigateRight();
        OnPress();
    }

    public override void OnPress()
    {
        base.OnPress();
        value = !value;

        OnValueChanged.Invoke(value);
        ValueUpdated();
    }

    public void ValueUpdated()
    {
        selectionObject.SetActive(value);
    }
}

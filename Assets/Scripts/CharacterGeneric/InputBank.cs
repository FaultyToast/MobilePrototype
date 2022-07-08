using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class InputBank : MonoBehaviour
{
    [NonSerialized]
    public Vector3 moveAxis;

    [NonSerialized]
    public bool[] actionInputs = new bool[10];

    [NonSerialized]
    public bool[] heldInputs = new bool[10];

    public bool IsActionHeld(ActionLocator.ActionType action)
    {
        return heldInputs[(int)action];
    }
}

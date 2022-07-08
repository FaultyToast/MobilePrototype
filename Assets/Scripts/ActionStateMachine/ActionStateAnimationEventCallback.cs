using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionStateAnimationEventCallback : MonoBehaviour
{
    [System.NonSerialized] public ActionStateMachine actionStateMachine;
    [System.NonSerialized] public bool outerObject = false;
    [System.NonSerialized] public ActionState targetState;

    public void Callback(string name)
    {
        if (actionStateMachine == null)
        {
            return;
        }

        if (outerObject)
        {
            if (targetState == null || !ReferenceEquals(targetState, actionStateMachine.currentState))
            {
                return;
            }
        }

        actionStateMachine.AnimationCallback(name);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetNormalizedTime : StateMachineBehaviour
{
    [SerializeField] private string targetParameter = "NormalizedTime";
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetFloat(targetParameter, stateInfo.normalizedTime);
    }
}

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StaggerState", menuName = "ActionStates/StaggerState", order = 1)]
public class StaggerState : ActionState
{
    public DamageInfo damageInfo;
    public AnimationCurve forwardSpeedAnimationCurve;
    public float forwardSpeedMultiplier;
    public AnimationCurve upSpeedAnimationCurve;
    public float upSpeedMultiplier;

    public string animationName;
    public string layerName = "Body";
    public bool setBodyToIdle = false;

    [Header("Timing")]
    public bool overrideLength = false;
    public float overrideTime = 0f;

    private Vector3 motionDirection;


    public override void OnEnter()
    {
        base.OnEnter();

        PlayAnimationCrossFade(layerName, animationName, 0.1f);
        if (setBodyToIdle)
        {
            PlayAnimationCrossFade("Body", "Idle", 0.1f);
        }

        if (isAuthority)
        {
            characterMovement.overrideRotation = true;
            characterMovement.targetRotation = characterMaster.modelPivot.rotation;
            characterMovement.velocity = Vector3.zero;
            if (damageInfo.attacker != null)
                motionDirection = -(damageInfo.attacker.transform.position.XZPlane() - outer.transform.position.XZPlane()).normalized;
            isOccupied = true;
        }

        characterMaster.knockdownImmunity = true;
    }

    public override void Update()
    {
        if (isAuthority)
        {
            characterMovement.rootMotion += motionDirection *
    forwardSpeedAnimationCurve.Evaluate(GetAnimationFrameFloat()) *
            forwardSpeedMultiplier *
            Time.deltaTime;

            characterMovement.rootMotion += Vector3.up *
                upSpeedAnimationCurve.Evaluate(GetAnimationFrameFloat()) *
                    upSpeedMultiplier * Time.deltaTime;
        }


    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isAuthority)
        {
            if (overrideLength)
            {
                if (fixedAge > overrideTime)
                {
                    ExitState();
                }
            }
            else
            {
                if (animator == null)
                {
                    ExitState();
                    return;
                }

                int bodyIndex = animator.GetLayerIndex(layerName);
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(bodyIndex);
                AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(bodyIndex);

                bool transitioningOut = stateInfo.IsName(animationName) && (!nextStateInfo.IsName(animationName) && nextStateInfo.fullPathHash != 0);

                if (!stateInfo.IsName(animationName) && !nextStateInfo.IsName(animationName) || transitioningOut)
                {
                    ExitState();
                }
            }
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        characterMaster.knockdownImmunity = false;
        characterMaster.AddHyperArmour();
    }
}

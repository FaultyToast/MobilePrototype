using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

[Serializable]
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/AnimatedAttack", order = 1)]
public class AnimatedAttack : ActionState
{

    // The duration of the fixed crossfade
    protected float crossFadeDuration = 0.1f;

    // Lock on target at the start of the attack
    protected CharacterMaster targetOverride = null;

    protected CharacterMaster softTargetOverride = null;
    protected CharacterMaster generalTarget
    {
        get
        {
            if (targetOverride != null)
            {
                return targetOverride;
            }
            else if (attack == null || attack.moveTowards == ComboAttack.MoveTowardsMode.SoftTargetDirection)
            {
                return softTargetOverride;
            }
            return null;
        }
    }

    protected CharacterMaster trueGeneralTarget
    {
        get
        {
            if (targetOverride != null)
            {
                return targetOverride;
            }
            else
            {
                return softTargetOverride;
            }
        }
    }

    // Rotation the player is trying to move to clamped to the maxRotationDegrees
    protected Quaternion inputRotationClamped;

    // Maximum amount the player will rotate per attack (if not locked on)
    protected float maxRotationDegrees = 90f;

    // Distance from the lock-on target (if any)
    protected float targetDistance = Mathf.Infinity;

    // Used by inheritors
    protected float forwardSpeedMultiplier = 1f;

    // Current combo attack
    public ComboAttack attack;

    private DamageComponent damageComponent;

    private float maxTargetRotationDegrees = 360f;

    private float ySlowSpeed = 100f;
    protected float attackSpeedMultiplier = 1f;

    protected float moveForwardValue;

    protected bool hasHit = false;

    protected float movementSpeedMultiplier;

    private bool _isAntiGravity = false;
    protected bool airStagger = false;
    protected int attackReference;

    List<ComboAttack.ComboProjectileSpawnInfo> objectList = new List<ComboAttack.ComboProjectileSpawnInfo>();
    protected bool isAntiGravity
    {
        set
        {
            if (value == _isAntiGravity)
            {
                return;
            }

            _isAntiGravity = value;

            if (value)
            {
                characterMovement.AddAntiGravity();
                characterMovement.motor.SetGroundSolvingActivation(false);
            }
            else
            {
                characterMovement.RemoveAntiGravity();
                characterMovement.motor.SetGroundSolvingActivation(true);
            }
        }
        get
        {
            return _isAntiGravity;
        }
    }

    // Definitions for types of combo
    public enum AttackType
    {
        Ground,
        Air,
        None
    }

    // Current combo type
    protected AttackType attackType = AttackType.None;

    public override void PreSync()
    {
        base.PreSync();
        attackReference = attack.GetInstanceID();

        if (attack.projectiles.Count > 0)
        {
            objectList = new List<ComboAttack.ComboProjectileSpawnInfo>(attack.projectiles);
        }
    }

    public override void OnEnter()
    {
        base.OnEnter();

        animator.Play("EmptyBuffer", animator.GetLayerIndex("Gesture, Override"));

        if (isAuthority)
        {
            characterMaster.targetsHitThisAttack = 0;

            try
            {
                EvaluateLockOn();
            }
            catch
            {
                Debug.Log("The combat target was probably null");
            }

            if (!attack.trackConstantly)
            {
                if (generalTarget == null)
                {
                    inputRotationClamped = Quaternion.RotateTowards(characterMaster.modelPivot.rotation, outer.GetComponent<CharacterInput>().GetInputRotation(), attack.maxTrackingAngle);
                    characterMovement.targetRotation = inputRotationClamped;
                }
                else
                {
                    inputRotationClamped = Quaternion.RotateTowards(characterMaster.modelPivot.rotation, Quaternion.LookRotation((generalTarget.bodyCenter.position - characterMaster.bodyCenter.position).XZPlane()), attack.maxTrackingAngle);
                    characterMovement.targetRotation = inputRotationClamped;
                }
            }


            characterMovement.rotationMultiplier = attack.rotationSpeedMultiplier;

            characterMovement.velocity.x = 0;
            characterMovement.velocity.z = 0;
            isOccupied = true;
            characterMovement.overrideRotation = true;

            moveForwardValue = attack.moveForwardValue;

            isAntiGravity = !attack.enableGravity;

            if (!attack.enableGravity)
            {
                attackType = AttackType.Air;
            }
        }

        if (!string.IsNullOrEmpty(attack.damageComponentName))
        {
            Transform damageComponentTransform = characterMaster.childLocator.GetChild(attack.damageComponentName);
            if (damageComponentTransform != null)
            {
                damageComponent = damageComponentTransform.GetComponent<DamageComponent>();
                if (damageComponent != null)
                {
                    damageComponent.damageInfo.damage = attack.damage;
                    damageComponent.damageInfo.attackReference = attack.GetInstanceID();
                    damageComponent.damageInfo.launchType = attack.launchType;
                    damageComponent.damageInfo.launchConditions = attack.launchConditions;
                    damageComponent.damageInfo.launchForceThreshold = attack.launchForceThreshold;
                }
            }
        }

        if (NetworkServer.active)
        {
            characterMaster.onDamageDealtConfirmedEvent.AddListener(OnHit);
        }

        PlayAnimationCrossFade("Body", attack.clipName, crossFadeDuration);
    }

    protected void EvaluateLockOn()
    {
        if (characterMaster.combatTarget != null && characterMaster.combatTarget.characterMaster != null)
        {
            targetOverride = outer.characterMaster.combatTarget.characterMaster;
            targetDistance = Vector3.Distance(characterMaster.bodyCenter.position, targetOverride.transform.position);
        }
        else if (characterMaster.generalCombatTarget != null)
        {
            softTargetOverride = characterMaster.generalCombatTarget.characterMaster;
            targetDistance = Vector3.Distance(characterMaster.bodyCenter.position, softTargetOverride.transform.position);
        }
    }

    public override void Update()
    {
        base.Update();

        if (!isAuthority)
        {
            return;
        }

        if (attack.trackConstantly)
        {
            characterMovement.rotationMultiplier = attack.rotationSpeedMultiplier * attack.trackingCurve.Evaluate(GetAnimationFrameFloat());

            if (generalTarget != null)
            {
                characterMovement.targetRotation = Quaternion.LookRotation((generalTarget.transform.position - characterMaster.bodyCenter.position).XZPlane());
            }

        }
    }

    private void UpdateAir(float animationFrameFloat, float dotAngle)
    {
        if (isAntiGravity && attack.aerialHoming)
        {
            characterMovement.velocity.y = Mathf.MoveTowards(characterMovement.velocity.y, 0, ySlowSpeed * Time.fixedDeltaTime);

            // Aerial attack homing
            if (trueGeneralTarget != null && dotAngle > 0.5f)
            {

                float targetYDifference = trueGeneralTarget.aerialLockOn.position.y - characterMaster.bodyCenter.position.y;
                float targetYDifferenceMagnitude = Mathf.Abs(targetYDifference);

                float maxHomingSpeed = attack.moveUpValue * attack.upSpeedAnimationCurve.Evaluate(animationFrameFloat) * movementSpeedMultiplier * cachedAnimationSpeed;
                float homingSpeedLerp = Mathf.InverseLerp(0, 2f, targetYDifferenceMagnitude);
                float homingSpeed = Mathf.Lerp(0f, maxHomingSpeed, homingSpeedLerp);

                float targetYSpeedMagnitude = homingSpeed * Time.fixedDeltaTime;
                float targetYMagnitude = Mathf.Min(targetYDifferenceMagnitude, targetYSpeedMagnitude);
                float targetYSign = Mathf.Sign(targetYDifference);

                if (targetYMagnitude / Time.fixedDeltaTime > 0.01f)
                {
                    characterMovement.rootMotion.y += targetYSign * targetYMagnitude;
                }
            }
        }
    }

    public void UpdateAttackSpeed(bool useHitStop = true)
    {
        attackSpeedMultiplier = 1;
        ModifyAttackSpeed();
        movementSpeedMultiplier = attackSpeedMultiplier;

        if (useHitStop)
        {
            attackSpeedMultiplier *= characterMaster.hitStopMultiplier;
        }

        if (attack.hitStopAffectsMovement)
        {
            movementSpeedMultiplier = attackSpeedMultiplier;
        }

        animator.SetFloat("AttackMultiplier", attackSpeedMultiplier);
    }

    public virtual void ModifyAttackSpeed()
    {

    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        UpdateAttackSpeed();

        if (!isAuthority)
        {
            return;
        }


        Vector3 moveDirection = Vector3.zero;

        switch (attack.moveTowards)
        {
            case ComboAttack.MoveTowardsMode.ModelRotation:
                {
                    moveDirection = characterMaster.modelPivot.forward;
                    break;
                }
            case ComboAttack.MoveTowardsMode.TargetDirection:
                {
                    moveDirection = characterMovement.targetRotation * Vector3.forward;
                    break;
                }
            case ComboAttack.MoveTowardsMode.SoftTargetDirection:
                {
                    moveDirection = characterMovement.targetRotation * Vector3.forward;
                    break;
                }
        }
        float dotAngle = 0;
        if (trueGeneralTarget != null)
        {
            dotAngle = Vector3.Dot(moveDirection, (trueGeneralTarget.transform.position.XZPlane() - characterMaster.bodyCenter.position.XZPlane()).normalized);
        }

        if (NetworkServer.active)
        {
            for (int i = 0; i < objectList.Count; i++)
            {
                ComboAttack.ComboProjectileSpawnInfo spawnInfo = objectList[i];
                if (GetAnimationFrame(false) >= spawnInfo.fireFrame)
                {
                    Transform spawnTransform = characterMaster.childLocator.GetChild(spawnInfo.spawnPointChildName);
                    Vector3 position = spawnTransform.position;
                    Quaternion rotation = Quaternion.identity;
                    string parent = "";

                    if (spawnInfo.matchPointRotation)
                    {
                        rotation = spawnTransform.rotation;
                    }
                    if (spawnInfo.ChildToParentObject)
                    {
                        position = Vector3.zero;
                        parent = spawnInfo.spawnPointChildName;
                    }

                    GameObject spawnedObject = ServerSpawnReferencedProjectile(spawnInfo.prefabToSpawn, position, rotation, parent, spawnInfo.ChildToParentObject);

                    if (spawnInfo.assignDamageComponent)
                    {
                        spawnedObject.GetComponent<DamageComponent>().ownerMaster = outer.characterMaster;
                    }
                    objectList.RemoveAt(i);
                    i--;
                }
            }
        }

        float animationFrameFloat = GetAnimationFrameFloat(false);

        if (animationFrameFloat >= 0)
        {
            float moveForwardMultiplier = 1f;

            if (attack.haltOnClosedDistance && trueGeneralTarget != null)
            {
                float minDotAngle = 0.75f;
                if (dotAngle > minDotAngle)
                {
                    float distance = Vector3.Distance(outer.transform.position.XZPlane(), trueGeneralTarget.transform.position.XZPlane());
                    float minDistance = trueGeneralTarget.characterMovement.motor.Capsule.radius + characterMovement.motor.Capsule.radius;
                    float difference = distance - minDistance;

                    moveForwardMultiplier = Mathf.InverseLerp(0.2f, 0.5f, difference);
                }
            }

            characterMovement.rootMotion += moveDirection *
            attack.forwardSpeedAnimationCurve.Evaluate(animationFrameFloat) *
            moveForwardValue *
            forwardSpeedMultiplier *
            movementSpeedMultiplier *
            cachedAnimationSpeed *
            moveForwardMultiplier *
            Time.fixedDeltaTime;

            if (!attack.aerialHoming)
            {
                characterMovement.rootMotion += Vector3.up *
                attack.upSpeedAnimationCurve.Evaluate(animationFrameFloat) *
                movementSpeedMultiplier *
                cachedAnimationSpeed *
                attack.moveUpValue * Time.fixedDeltaTime;
            }



            switch (attackType)
            {
                case AttackType.Air:
                    UpdateAir(animationFrameFloat, dotAngle);
                    break;
            }

            if (isAntiGravity && attack.enableGravityFrame > 0 && animationFrameFloat >= attack.enableGravityFrame && !stateEnded)
            {
                isAntiGravity = false;
            }
        }
        else
        {
            SoftExit();
        }

        if (GetAnimationFrame(false) >= attack.exitFrame)
        {
            SoftExit();
        }

        //Allow next animation to be played
        if (minimumInterruptPriority != 3 && GetAnimationFrame(false) > attack.minAttackFrames)
        {
            minimumInterruptPriority = 3;
        }
    }

    public void SoftExit()
    {
        if (airStagger && !hasHit)
        {
            outer.SetState(Instantiate(GameVariables.instance.aerialAttackCooldownState));
            return;
        }
        
        ExitState();
    }

    public override void OnExit()
    {
        base.OnExit();
        UpdateAttackSpeed(false);

        if (NetworkServer.active)
        {
            characterMaster.onDamageDealtConfirmedEvent.RemoveListener(OnHit);
        }

        if (!isAuthority)
        {
            return;
        }

        if (damageComponent)
        {
            damageComponent.damageInfo.attackReference = -1;
        }

        characterMovement.rotationMultiplier = 1f;
        characterMovement.targetRotation = characterMaster.modelPivot.rotation;
        isOccupied = false;
        characterMovement.overrideRotation = false;

        isAntiGravity = false;
    }

    public void OnHit(DamageInfo damageInfo, CharacterMaster victim)
    {
        if (damageInfo.attackReference == attackReference)
        {
            characterMaster.targetsHitThisAttack++;

            if (!NetworkServer.active)
            {
                return;
            }

            hasHit = true;
        }
    }

    public override void Serialize(NetworkWriter writer)
    {
        base.Serialize(writer);
        writer.WriteComboAttack(attack);
        writer.WriteInt(attackReference);
    }

    public override void Deserialize(NetworkReader reader)
    {
        base.Deserialize(reader);
        attack = reader.ReadComboAttack();
        attackReference = reader.ReadInt();
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;
using KinematicCharacterController;
using System;
using UnityEngine.Serialization;
using Mirror;

public class CharacterMovement : NetworkBehaviour, ICharacterController
{
    // Serializable
    [Header("Movement Values")]

    // Movement
    public float moveSpeed;
    public float acceleration;
    public float decceleration;
    [System.NonSerialized] public float moveSpeedModifier = 1f;

    public float maxSpeed
    {
        get
        {
            return moveSpeed * moveSpeedModifier;
        }
    }

    // Aerial
    public float jumpSpeed;
    public float gravityScale;
    public float airControlMultiplier = 0.5f;
    [System.NonSerialized] public bool preventDownwardsGravity = false;

    // Rotation
    public float rotationSpeed;
    public AnimationCurve rotationSpeedCurve;
    public bool projectRotationOnSpeed = false;

    public TargetLookType targetLookType = TargetLookType.None;

    [Header("Component References")]
    [SerializeField] public KinematicCharacterMotor motor;
    [System.NonSerialized] public CharacterMaster characterMaster;
    protected InputBank inputBank;
    protected ActionStateMachine movementASM;

    [Header("Debug")]
    public Vector3 velocity;

    // Non Serialized

    // Movement
    protected bool moving = false;
    protected Vector3 targetVelocity;
    protected Vector3 lastMoveVelocity;
    protected float accelerationBuildUp = 0;
    protected Vector3 estimatedRootMotionVelocity;
    [System.NonSerialized] public Vector3 rootMotion;
    [System.NonSerialized] public bool jumpingAllowed = true;
    [System.NonSerialized] public Vector3? inputOverride;

    // Aerial
    protected int antiGravitySources = 0;
    public bool isJumping = false;
    [System.NonSerialized] public bool flightControl = false;

    // Grounding
    protected float lastGroundedTime;
    protected float jumpRememberTime = 0.2f;
    [System.NonSerialized] public Vector3 lastStablePosition;

    // Rotation
    [System.NonSerialized] public float rotationMultiplier = 1f;
    [System.NonSerialized] public bool overrideRotation = false;
    [System.NonSerialized] public Quaternion targetRotation;
    [System.NonSerialized] public Quaternion? rotationOverride = null;

    // Input
    [NonSerialized] public bool jumpRequested;

    [Header("Root Motion")]
    //Root Motion - Run Animation Data
    public string runClipName = "Run";
    public AnimationCurve forwardRootMotion;
    public AnimationCurve rightRootMotion;
    protected float cachedRunAnimFrameRate;
    protected float cachedRunAnimClipLength;
    public float rootMotionForwardSpeed;
    public float rootMotionRightSpeed;
    [System.NonSerialized] public bool attemptingMovement;

    [Header("Animation")]
    public bool setMovingOnTurn = false;
    public bool useStrafeAnims = false;

    public List<Vector3> queuedLaunches = new List<Vector3>();
    [System.NonSerialized] public Vector3 trueVelocity;
    private Vector3 lastPosition;

    //Test cause other gave errors
    public Animator animator;

    private LayerMask walkableLayers;
    private float strafeValue;
    public struct AnimatorParams
    {
        public bool isGrounded;
        public bool attemptingMovement;
        public bool moving;
        public float strafeValue;
        public float moveSpeedModifier;
    }

    public enum TargetLookType
    {
        None,
        Strafe2D,
        Strafe3D
    }

    public Vector3? rotateTowardsOverride = null;
    public Vector3? cameraDirection = null;

    public AnimatorParams animatorParams;



    // Variable Functions

    // Acceleration depending on grounding status
    public float currentAcceleration
    {
        get
        {
            float multiplier = 1f;
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                multiplier *= airControlMultiplier;
            }
            return acceleration * multiplier;
        }
    }

    // Deceleration depending on grounding status
    public float currentDeceleration
    {
        get
        {
            float multiplier = 1f;
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                multiplier *= airControlMultiplier;
            }
            return decceleration * multiplier;
        }
    }

    public bool isAntiGravity
    {
        get
        {
            return antiGravitySources > 0;
        }
    }


    //Coyote time jump condition - allows player to jump slightly after leaving the ground
    protected bool canJump
    {
        get
        {
            return (Time.time - lastGroundedTime < jumpRememberTime) && canMove && !isJumping && jumpingAllowed;
        }
    }

    //Cyote time jump condition - allows player to jump slightly after leaving the ground
    public bool canMove
    {
        get
        {
            return !movementASM.isOccupied;
        }
    }

    public bool isGrounded
    {
        get
        {
            return motor.GroundingStatus.IsStableOnGround;
        }
    }

    public bool isAuthority
    {
        get
        {
            return FracturedUtility.HasEffectiveAuthority(characterMaster.networkIdentity);
        }
    }

    private static int _NPCCollisionLayer = -1;
    public static int NPCCollisionLayer
    {
        get
        {
            if (_NPCCollisionLayer == -1)
            {
                _NPCCollisionLayer = LayerMask.NameToLayer("NPCOnly");
            }
            return _NPCCollisionLayer;
        }
    }

    private void Awake()
    {
        walkableLayers = LayerMask.GetMask("Default", "Stairs", "Ground", "Pillar", "Terrain", "IKStairs");
        float maxStepHeight = 0.8f;
        float maxSlopeAngle = 40f;

        motor.StableGroundLayers = walkableLayers;
        motor.MaxStepHeight = maxStepHeight;
        motor.MaxStableSlopeAngle = maxSlopeAngle;
        motor.MaxStableDistanceFromLedge = motor.Capsule.radius;
        motor.MaxVelocityForLedgeSnap = 999f;

        // Assign the characterController to the motor
        motor.CharacterController = this;

        //controller = GetComponent<CharacterController>();
        characterMaster = GetComponent<CharacterMaster>();
        movementASM = GetComponent<ActionStateMachine>();
        inputBank = GetComponent<InputBank>();
    }

    // Start is called before the first frame update
    public virtual void Start()
    {
        //Set inital values and components
        velocity = Vector3.zero;

        if (!isAuthority)
        {
            motor.enabled = false;
        }

        if (!characterMaster.isPlayer)
        {
            motor.CollidableLayers |= (1 << NPCCollisionLayer);
        }
    }

    [TargetRpc]
    public void TargetTeleport(NetworkConnection target, Vector3 position)
    {
        Teleport(position);
    }

    public void Teleport(Vector3 position)
    {
        motor.SetPosition(position);
        velocity = Vector3.zero;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (!isAuthority)
        {
            return;
        }

        RotateTowardsTarget();
        HandleInputs(inputBank.moveAxis);
    }

    protected virtual void JumpFailed()
    {

    }

    void FixedUpdate()
    {
        if (!isAuthority)
        {
            return;
        }

        if (characterMaster != null && characterMaster.animator != null)
        {
            MoveAnim();
        }

        RecordLastGroundedTime();
        if (animatorParams.attemptingMovement)
        {
            RunRootMotion();
        }
    }

    private void RecordLastGroundedTime()
    {
        if (isGrounded)
        {
            lastStablePosition = transform.position;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSyncAnimatorParams(AnimatorParams animatorParams)
    {
        RpcSyncAnimatorParams(animatorParams);
    }

    [ClientRpc]
    public void RpcSyncAnimatorParams(AnimatorParams animatorParams)
    {
        if (!isAuthority)
        {
            characterMaster.animator.SetBool("IsGrounded", animatorParams.isGrounded);
            characterMaster.animator.SetBool("Moving", animatorParams.moving);
            characterMaster.animator.SetBool("AttemptingMovement", animatorParams.attemptingMovement);
            characterMaster.animator.SetFloat("MoveSpeedModifier", animatorParams.moveSpeedModifier);

            if (useStrafeAnims)
            {
                characterMaster.animator.SetFloat("StrafeValue", animatorParams.strafeValue);
            }
        }
    }

    public void RotateTowardsTarget()
    {
        if (rotationOverride != null)
        {
            Quaternion flatRotation = Quaternion.Euler(0f, rotationOverride.Value.eulerAngles.y, 0f);
            characterMaster.modelPivot.rotation = flatRotation;
            targetRotation = flatRotation;

            return;
        }

        if (rotateTowardsOverride != null && cameraDirection != null)
        {
            Vector3 targetDirection = (rotateTowardsOverride.Value - characterMaster.modelPivot.position).XZPlane();
            targetDirection = targetDirection.normalized;
            targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        }
        else if (characterMaster.combatTarget != null)
        {
            switch (targetLookType)
            {
                case TargetLookType.Strafe2D:
                    {
                        targetRotation = Quaternion.LookRotation((characterMaster.combatTarget.transform.position - characterMaster.bodyCenter.position).XZPlane().normalized);
                        break;
                    }
                case TargetLookType.Strafe3D:
                    {
                        Vector3 lookForward = (characterMaster.combatTarget.transform.position - characterMaster.bodyCenter.position).normalized;
                        targetRotation = Quaternion.LookRotation(lookForward);
                        break;
                    }
            }
        }

        float localRotationMultiplier = 1;
        if (!overrideRotation && !motor.GroundingStatus.IsStableOnGround)
        {
            localRotationMultiplier *= airControlMultiplier;
        }

        // jesus christ this line is long
        characterMaster.modelPivot.rotation = Quaternion.RotateTowards(characterMaster.modelPivot.rotation, targetRotation, rotationSpeedCurve.Evaluate(Quaternion.Angle(characterMaster.modelPivot.rotation, targetRotation)) * rotationSpeed * Time.deltaTime * rotationMultiplier * localRotationMultiplier);
    }

    public void AddAntiGravity()
    {
        antiGravitySources++;
    }

    public void RemoveAntiGravity()
    {
        antiGravitySources--;
        antiGravitySources = Mathf.Max(antiGravitySources, 0);
    }

    // Handles movement using a global direction (For AI)
    public void HandleInputs(Vector3 inputs)
    {
        inputBank.moveAxis = Vector3.zero;

        if (inputOverride != null)
        {
            inputs = inputOverride.Value;
        }

        attemptingMovement = inputs.sqrMagnitude > 0.5f;

        // Don't perform the move if the action state machine disallows it
        if (characterMaster.actionStateMachine.isOccupied)
        {
            //lastMoveVelocity = Vector3.zero;
            inputs = Vector3.zero;
        }

        bool moving = inputs.sqrMagnitude > 0.1f;

        if (moving && characterMaster.isPlayer)
        {
            UIManager.instance.tutorialManager.moveEvent.Invoke();
        }

        Vector3 direction = new Vector3(inputs.x, inputs.y, inputs.z);

        direction = Vector3.ClampMagnitude(direction, 1f);

        targetVelocity = direction * maxSpeed;

        // Add root motion from the last frame to our velocity, useful for leaving dashes that use root motion

        Vector3 bonusFromRootMotion = Vector3.zero;

        bool isSliding = !motor.GroundingStatus.IsStableOnGround && motor.GroundingStatus.FoundAnyGround;

        float velocityMagnitude = velocity.XZPlane().magnitude;

        Vector3 velocityBase = velocity.XZPlane().magnitude > lastMoveVelocity.magnitude || isSliding || flightControl ? velocity.XZPlane() : lastMoveVelocity;

        //Get the current horrizontal velocity
        Vector3 startingVelocity = ClampMagnitude(velocityBase + bonusFromRootMotion, Mathf.Max(velocityMagnitude, maxSpeed), Mathf.Lerp(0, maxSpeed, accelerationBuildUp));

        Vector3 newVelocity = startingVelocity;

        if (targetVelocity.sqrMagnitude > 0.1f)
        {
            if (targetLookType == TargetLookType.None)
            {
                targetRotation = Quaternion.LookRotation(targetVelocity, Vector3.up);
            }

            if (projectRotationOnSpeed)
            {
                float dot = Vector3.Dot(direction, characterMaster.modelPivot.forward);
                dot = Mathf.Max(0, dot);
                targetVelocity *= dot;
            }
        }

        //Moving - accelerate towards target velocity
        if ((Vector3.Dot(targetVelocity, startingVelocity) > 0 || startingVelocity.magnitude < 0.1f) && targetVelocity.sqrMagnitude > 0.01f)
        {
            if (startingVelocity.magnitude <= maxSpeed)
            {
                newVelocity = Vector3.MoveTowards(startingVelocity, targetVelocity, currentAcceleration * Time.deltaTime);
            }
            else
            {
                newVelocity = Vector3.MoveTowards(startingVelocity, targetVelocity, currentDeceleration * Time.deltaTime);
            }
        }

        else
        {
            //Decelerate
            lastMoveVelocity = Vector3.zero;
            newVelocity = Vector3.MoveTowards(velocity.XZPlane(), Vector3.zero, currentDeceleration * Time.deltaTime);
        }

        Vector3 addedVelocity = newVelocity - startingVelocity;

        if (isSliding)
        {
            Vector3 left = Vector3.Cross(motor.GroundingStatus.GroundNormal, -motor.CharacterUp);
            Vector3 slope = Vector3.Cross(motor.GroundingStatus.GroundNormal, left);
            Vector3 flatSlope = slope.XZPlane().normalized;

            float upDistance = Vector3.Dot(addedVelocity, flatSlope);

            if (upDistance > 0f)
            {
                addedVelocity -= flatSlope * upDistance;
            }
        }

        Vector3 finalVelocity = startingVelocity + addedVelocity;


        //Set the player velocity vector
        velocity = new Vector3(finalVelocity.x, flightControl ? finalVelocity.y : velocity.y, finalVelocity.z);
        lastMoveVelocity = velocity.XZPlane();

        bool decayStrafe = false;
        if (targetLookType == TargetLookType.Strafe2D && characterMaster.combatTarget != null)
        {
            Vector3 targetDirection = characterMaster.modelForward;
            Vector3 rightDirection = characterMaster.modelPivot.right;
            if (targetVelocity.sqrMagnitude > 0.1f && Vector3.Dot(targetVelocity, targetDirection) < 0.2f)
            {
                if (Vector3.Dot(targetVelocity, rightDirection) > 0)
                {
                    strafeValue = 1f;
                }
                else
                {
                    strafeValue = -1f;
                }
            }
            else
            {
                decayStrafe = true;
            }
        }
        else
        {
            decayStrafe = true;
        }

        if (decayStrafe)
        {
            strafeValue = Mathf.MoveTowards(strafeValue, 0, 10 * Time.deltaTime);
        }
    }

    public static Vector3 ClampMagnitude(Vector3 v, float max, float min)
    {
        double sm = v.sqrMagnitude;
        if (sm > (double)max * (double)max) return v.normalized * max;
        else if (sm < (double)min * (double)min) return v.normalized * min;
        return v;
    }

    // Checks if player is grounded calculates gravity
    void DoGravity()
    {
        //Not Grounded
        if (motor.GroundingStatus.IsStableOnGround)
        {
            //General downward force to keep player grounded
            velocity.y = -1f;
            lastGroundedTime = Time.time;
        }
        else if (!isAntiGravity)
        {
            //Accelerate player towards the ground
            velocity.y -= gravityScale * Time.deltaTime;

            if (preventDownwardsGravity)
            {
                velocity.y = Mathf.Max(velocity.y, 0f);
            }
        }
    }

    // Jumping
    protected void AttemptJump()
    {
        //If jump input is pressed
        if (jumpRequested)
        {
            jumpRequested = false;
            if (canJump)
            {
                Jump(jumpSpeed, "JumpLaunch");
                if (characterMaster.HasBuff(Buffs.DamageOnJumps))
                {
                    DamageInfo damageinfo = new DamageInfo();
                    damageinfo.isPassiveDamage = true;
                    damageinfo.damage = characterMaster.characterHealth.maxHealth * 0.05f;
                    characterMaster.characterHealth.DealDamage(damageinfo);
                }

                if (characterMaster.isPlayer)
                {
                    UIManager.instance.tutorialManager.jumpEvent.Invoke();
                }
            }
            else
            {
                JumpFailed();
            }
        }
    }

    public void Jump(float force, string animation)
    {
        characterMaster.PlayAnimation(animation, 0.1f, 0, 0f);
        velocity.y = force;
        isJumping = true;
        motor.ForceUnground();
    }

    void MoveAnim()
    {
        if (characterMaster.animator == null)
        {
            return;
        }

        Vector3 moveVelocity = new Vector3(velocity.x, 0, velocity.z);
        float speedMag = moveVelocity.magnitude;

        Vector3 modelDirection = characterMaster.modelPivot.forward.normalized;
        Vector3 targetDirection = targetVelocity.normalized;

        animatorParams.moving = speedMag > 0 && !movementASM.isOccupied;
        characterMaster.animator.SetBool("Moving", animatorParams.moving);

        animatorParams.isGrounded = motor.GroundingStatus.IsStableOnGround;
        characterMaster.animator.SetBool("IsGrounded", animatorParams.isGrounded);

        animatorParams.attemptingMovement = attemptingMovement;
        characterMaster.animator.SetBool("AttemptingMovement", animatorParams.attemptingMovement);

        animatorParams.moveSpeedModifier = moveSpeedModifier;
        characterMaster.animator.SetFloat("MoveSpeedModifier", animatorParams.moveSpeedModifier);

        if (useStrafeAnims)
        {
            animatorParams.strafeValue = strafeValue;
            characterMaster.animator.SetFloat("StrafeValue", animatorParams.strafeValue);
        }

        CmdSyncAnimatorParams(animatorParams);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        lastPosition = motor.TransientPosition;

        DoGravity();
        AttemptJump();
        ProcessLaunches();


        if (rootMotion != Vector3.zero)
        {
            Vector3 moveAmount = rootMotion;
            rootMotion = Vector3.zero;
            motor.MoveCharacter(transform.position + moveAmount);
        }
    }

    public void Launch(Vector3 velocity)
    {
        queuedLaunches.Add(velocity);
    }

    public void ProcessLaunches()
    {
        if (queuedLaunches.Count > 0)
        {
            motor.ForceUnground();
            for (int i = 0; i < queuedLaunches.Count; i++)
            {
                velocity += queuedLaunches[i];

            }
            queuedLaunches.Clear();
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = velocity;
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        velocity = motor.BaseVelocity;

        trueVelocity = (motor.TransientPosition - motor.InitialSimulationPosition) / Time.fixedDeltaTime;
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        // Handle landing and leaving ground
        if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLanded();
        }
        else if (!motor.GroundingStatus.IsStableOnGround && motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLeaveStableGround();
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    protected virtual void OnLanded()
    {
        isJumping = false;
    }

    protected void OnLeaveStableGround()
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    protected float GetRunAnimationFrameFloat(AnimatorStateInfo state)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);


        return (stateInfo.normalizedTime % 1f) * cachedRunAnimClipLength * cachedRunAnimFrameRate;
    }

    private void RunRootMotion()
    {
        if (animator != null)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName(runClipName))
            {
                if (cachedRunAnimFrameRate == 0)
                {
                    AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
                    if (clipInfos.Length == 0)
                    {
                        return;
                    }
                    AnimatorClipInfo clipInfo = clipInfos[0];

                    cachedRunAnimFrameRate = clipInfo.clip.frameRate * currentState.speed;
                    cachedRunAnimClipLength = clipInfos[0].clip.length;
                }
                float runAnimationFrame = GetRunAnimationFrameFloat(currentState);

                float forwardValue = forwardRootMotion.Evaluate(runAnimationFrame) * rootMotionForwardSpeed * Time.deltaTime;
                float rightValue = rightRootMotion.Evaluate(runAnimationFrame) * rootMotionRightSpeed * Time.deltaTime;


                if (useStrafeAnims && Mathf.Abs(strafeValue) > 0.1f)
                {
                    if (rootMotionRightSpeed == 0)
                    {
                        rightValue = rightRootMotion.Evaluate(runAnimationFrame) * 5f * Time.deltaTime;
                    }
                    rootMotion += targetRotation * Vector3.right * strafeValue * (rightValue);
                }
                else
                {
                    //Forward Root Motion
                    rootMotion += targetRotation * Vector3.forward * forwardValue;
                    //Right Root Motion
                    rootMotion += targetRotation * Vector3.right * rightValue;
                }

            }
        }


    }
}

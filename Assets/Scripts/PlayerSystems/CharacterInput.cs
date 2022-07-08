using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;
using UnityEngine.InputSystem;

public class CharacterInput : NetworkBehaviour
{
    private CharacterMovement movement;
    private CharacterMaster characterMaster;
    private ActionLocator actionLocator;
    private ActionStateMachine actionStateMachine;

    [NonSerialized] public Camera playerCamera;
    private InputBank inputBank;

    private const string MouseXInput = "Mouse X";
    private const string MouseYInput = "Mouse Y";
    private const string MouseScrollInput = "Mouse ScrollWheel";
    private const string HorizontalInput = "Right";
    private const string VerticalInput = "Forward";
    public float movementDeadZone = 0.25f;

    //[EnumNamedArray(typeof(ActionLocator.ActionType))]
    //public string[] actionInputNames = new string[System.Enum.GetValues(typeof(ActionLocator.ActionType)).Length];

    public InputDef[] inputDefs;

    public enum ButtonType
    {
        Button,
        Axis
    }

    [Serializable]
    public class InputDef
    {
        public string inputName;

        public ActionLocator.ActionType inputType;
        public ButtonType buttonType = ButtonType.Button;

        [Header("Button")]
        public string secondaryInputName;
        public string exclusionInputName;

        [Header("Axis")]
        public bool usePositiveAxis = true;
        public float axisReq = 0.5f;
        public bool useNegativeAxis = false;
        public float negativeAxisReq = 0f;
        public bool useRawAxis = true;
        public bool debugPrintAxis = false;

        [System.NonSerialized] public bool wasAxisPressed = false;
        [System.NonSerialized] public InputAction inputAction;
        [System.NonSerialized] public InputAction secondaryInputAction;
        [System.NonSerialized] public InputAction exclusionInputAction;
    }

    [System.NonSerialized] public Vector2 moveInput;

    // Start is called before the first frame update

    void Start()
    {
        movement = GetComponent<CharacterMovement>();
        actionLocator = GetComponent<ActionLocator>();
        actionStateMachine = GetComponent<ActionStateMachine>();
        playerCamera = Camera.main;
        inputBank = GetComponent<InputBank>();
        characterMaster = movement.characterMaster;

        if (hasAuthority)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (isLocalPlayer)
        {
            foreach(InputDef inputDef in inputDefs)
            {
                inputDef.inputAction = InputManager.playerControls.FindAction(inputDef.inputName, true);

                if (!String.IsNullOrEmpty(inputDef.secondaryInputName))
                {
                    inputDef.secondaryInputAction = InputManager.playerControls.FindAction(inputDef.secondaryInputName, true);
                }

                if (!String.IsNullOrEmpty(inputDef.exclusionInputName))
                {
                    inputDef.exclusionInputAction = InputManager.playerControls.FindAction(inputDef.exclusionInputName, true);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            moveInput = InputManager.playerControls.Player.Move.ReadValue<Vector2>();

            for (int i = 0; i < inputBank.actionInputs.Length; i++)
            {
                inputBank.actionInputs[i] = false;
                inputBank.heldInputs[i] = false;
            }
            if (!GameManager.instance.cursorUsed)
            {
                foreach(InputDef inputDef in inputDefs)
                {
                    switch(inputDef.buttonType)
                    {
                        case ButtonType.Button:
                            {
                                if (inputDef.exclusionInputAction != null && inputDef.exclusionInputAction.IsPressed())
                                {
                                    continue;
                                }

                                bool secondaryHeld = inputDef.secondaryInputAction != null ? inputDef.secondaryInputAction.IsPressed() : true;

                                inputBank.actionInputs[(int)inputDef.inputType] = (inputDef.inputAction.WasPressedThisFrame() && secondaryHeld) || inputBank.actionInputs[(int)inputDef.inputType];
                                inputBank.heldInputs[(int)inputDef.inputType] = (inputDef.inputAction.IsPressed() && secondaryHeld) || inputBank.heldInputs[(int)inputDef.inputType];
                                break;
                            }
                        case ButtonType.Axis:
                            {
                                /*
                                float axis;
                                if (inputDef.useRawAxis)
                                {
                                    axis = Input.GetAxisRaw(inputDef.inputName);
                                }
                                else
                                {
                                    axis = Input.GetAxis(inputDef.inputName);
                                }

                                if (inputDef.debugPrintAxis)
                                {
                                    Debug.Log(axis);
                                }

                                bool isHeld = (inputDef.usePositiveAxis && axis > inputDef.axisReq) || (inputDef.useNegativeAxis && axis < inputDef.negativeAxisReq);
                                bool pressed = isHeld && !inputDef.wasAxisPressed;
                                inputDef.wasAxisPressed = isHeld;

                                inputBank.actionInputs[(int)inputDef.inputType] = pressed || inputBank.actionInputs[(int)inputDef.inputType];
                                inputBank.heldInputs[(int)inputDef.inputType] = isHeld || inputBank.heldInputs[(int)inputDef.inputType];
                                                                */
                                break;
                            }
                    }

                }
            }

            if (!GameManager.instance.inMenu)
            {
                //Player Movement
                Vector2 input = moveInput;
                if (input.magnitude > movementDeadZone)
                {
                    Vector3 axis = GetCameraMovementAxis(input);
                    inputBank.moveAxis = new Vector3(axis.y, 0, axis.x);
                }
                else
                {
                    inputBank.moveAxis = Vector3.zero;
                }

                
                if (InputManager.playerControls.Player.Jump.WasPressedThisFrame())
                {
                    movement.jumpRequested = true;
                }
            }

            movement.cameraDirection = playerCamera.transform.forward;
        }
    }

    // Handles movement using an input and camera direction
    public Vector2 GetCameraMovementAxis(Vector2 Axis)
    {
        //magnitutde of player input
        float mag = Axis.magnitude;

        //Get relevate camera transfroms for camera relative movement
        Vector3 camForward = playerCamera.transform.forward;
        Vector3 camRight = playerCamera.transform.right;
        camForward.y = 0; // Cancel out vertical 
        camRight.y = 0;// Cancel out vertical 
        camForward = Vector3.Normalize(camForward);
        camRight = Vector3.Normalize(camRight);

        //Normalize player input
        Axis = Vector3.Normalize(Axis);

        Vector3 direction = camForward * Axis.y + camRight * Axis.x;

        return new Vector2(direction.z, direction.x);
    }

    public Vector3 GetInputVector()
    {
        //Normalize player input
        Vector3 axis = Vector3.Normalize(new Vector3(moveInput.x, 0f, moveInput.y));

        if (axis.magnitude <= 0.01f)
        {
            return characterMaster.modelPivot.forward;
        }

        //Get relevate camera transfroms for camera relative movement
        Vector3 camForward = playerCamera.transform.forward;
        Vector3 camRight = playerCamera.transform.right;
        camForward.y = 0; // Cancel out vertical 
        camRight.y = 0;// Cancel out vertical 
        camForward = Vector3.Normalize(camForward);
        camRight = Vector3.Normalize(camRight);

        return camForward * axis.z + camRight * axis.x;
    }

    public Quaternion GetInputRotation()
    {
        Vector3 axis = GetInputVector();

        return Quaternion.LookRotation(axis);
    }

    public Quaternion GetCameraYaw()
    {
        return Quaternion.Euler(0f, playerCamera.transform.rotation.eulerAngles.y, 0f);
    }
}

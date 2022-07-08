using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class ActionLocator : MonoBehaviour
{
    public enum ActionType
    {
        None,
        PrimaryAction,
        SecondaryAction,
        TertiaryAction,
        UtilityAction,
        MiscAction1,
        MiscAction2,
        MiscAction3,
        MiscAction4,
    }

    public enum CanBeUsed
    {
        OnGround = (1 << 0),
        InAir = (1 << 1)
    }

    public enum StartCooldown
    {
        Beginning,
        End
    }

    [System.Serializable]
    public class ActionTypeDef
    {
        public ActionState action;
        public float cooldown;
        public StartCooldown startCooldown = StartCooldown.Beginning;
        [System.NonSerialized] public float currentCooldown;
        [System.NonSerialized] public bool awaitingEnd = false;
        public ActionType actionType;
        [EnumFlag]
        public CanBeUsed canBeUsedFlags = (CanBeUsed)~0;
        public bool activateOnHold = false;
    }

    private InputBank inputBank;
    private ActionStateMachine actionStateMachine;
    public bool queueInputs = false;

    public ActionTypeDef[] actions;
    [System.NonSerialized] public ActionTypeDef[] orderedActions;

    [System.NonSerialized] public UnityEvent actionEnded = new UnityEvent();

    public void Awake()
    {
        orderedActions = new ActionTypeDef[System.Enum.GetValues(typeof(ActionType)).Length];
        foreach(ActionTypeDef action in actions)
        {
            orderedActions[(int)action.actionType] = action;
        }
    }

    private void Start()
    {
        inputBank = GetComponent<InputBank>();
        actionStateMachine = GetComponent<ActionStateMachine>();

    }

    public void LateUpdate()
    {
        UpdateCooldowns();
        HandleInputs();
    }

    public void UpdateCooldowns()
    {
        foreach(ActionTypeDef action in actions)
        {
            if (action.currentCooldown > 0)
            {
                action.currentCooldown -= Time.deltaTime;
            }
        }
    }

    public void HandleInputs()
    {
        for(int i = 0; i < actions.Length; i++)
        {
            if (actions[i].action == null)
            {
                Debug.LogError("An action is listed in " + gameObject.name + " without a valid action state");
                continue;
            }
            if (actions[i].currentCooldown <= 0 && !actions[i].awaitingEnd)
            {
                bool actionFulfilled;
                if (actions[i].activateOnHold)
                {
                    actionFulfilled = inputBank.IsActionHeld(actions[i].actionType);
                }
                else
                {
                    actionFulfilled = inputBank.actionInputs[(int)actions[i].actionType];
                }
                if (actionFulfilled)
                {
                    if (UseConditionFulfilled(actions[i]))
                    {
                        ActionState newState = Instantiate(actions[i].action);

                        if (actions[i].cooldown > 0)
                        {
                            int index = i;
                            switch (actions[i].startCooldown)
                            {
                                case StartCooldown.Beginning:
                                    {
                                        newState.actionStarted.AddListener(delegate { SetCooldown(index); });
                                        break;
                                    }
                                case StartCooldown.End:
                                    {
                                        newState.actionStarted.AddListener(delegate { SetAwaitingEnd(index); });
                                        newState.actionEnded.AddListener(delegate { SetCooldown(index); });
                                        break;
                                    }
                            }

                        }
                        newState.actionEnded.AddListener(delegate { actionEnded.Invoke(); });
                        newState.actionID = i;

                        if (queueInputs && !actions[i].activateOnHold)
                        {
                            actionStateMachine.QueueState(newState);
                        }
                        else
                        {
                            actionStateMachine.SetNextState(newState);
                        }
                    }
                }
            }
        }
    }

    public bool UseConditionFulfilled(ActionTypeDef actionDef)
    {
        if (actionDef.canBeUsedFlags.HasFlag(CanBeUsed.OnGround))
        {
            if (actionStateMachine.characterMovement.isGrounded)
            {
                return true;
            }
        }

        if (actionDef.canBeUsedFlags.HasFlag(CanBeUsed.InAir))
        {
            if (!actionStateMachine.characterMovement.isGrounded)
            {
                return true;
            }
        }

        return false;
    }

    public void SetCooldown(int id, float? timeOverride = null)
    {
        actions[id].awaitingEnd = false;
        actions[id].currentCooldown = timeOverride != null ? timeOverride.Value : actions[id].cooldown;
    }

    public void SetAwaitingEnd(int id)
    {
        actions[id].awaitingEnd = true;
    }
}

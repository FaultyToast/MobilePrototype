using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ControllerMenu : MonoBehaviour
{
    public static MenuButton selectedButton;
    public static bool pressedThisFrame = false;

    public List<ButtonRow> rows = new List<ButtonRow>();
    public Menu associatedMenu;

    [System.Serializable]
    public class ButtonRow
    {
        public List<MenuButton> elements = new List<MenuButton>();
    }

    private int selectedX;
    private int selectedY;
    private bool active = false;
    private float deadzone = 0.1f;
    private bool activatedThisFrame = false;

    private bool holdingRight = false;
    private bool wasHoldingRight = false;

    private bool holdingUp = false;
    private bool wasHoldingUp = false;
    private bool ignoreInputThisFrame = false;

    private bool isControlling;

    public bool activeWithMouse = true;
    public bool activateOnStart = false;
    public bool alwaysActivated = false;

    public void Awake()
    {
        if (associatedMenu != null)
        {
            associatedMenu.onMenuOpened.AddListener(Activate);
            associatedMenu.onMenuClosed.AddListener(Deactivate);
        }
        InputManager.onControlModeSwitched.AddListener(UpdateControlMode);

        if (activateOnStart || alwaysActivated)
        {
            Activate();
        }
        UpdateControlMode(InputManager.controlMode);
    }

    public void OnDestroy()
    {
        InputManager.onControlModeSwitched.RemoveListener(UpdateControlMode);
    }

    public void UpdateControlMode(InputManager.ControlMode controlMode)
    {
        if (activeWithMouse)
        {
            isControlling = controlMode == InputManager.ControlMode.Controller;
            ShowControls();

            if (!isControlling)
            {
                Deselect();
            }
            return;
        }

        switch (controlMode)
        {
            case InputManager.ControlMode.Controller:
                {
                    isControlling = true;
                    ignoreInputThisFrame = true;
                    ShowControls();
                    break;
                }
            case InputManager.ControlMode.KeyboardMouse:
                {
                    isControlling = false;
                    HideControls();
                    break;
                }
        }
    }

    public void ResetSelectionColours()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            for (int j = 0; j < rows[i].elements.Count; j++)
            {
                MenuButton currentButton = rows[i].elements[j];
                currentButton.firstSelection = true;
            }
        }
    }

    public void Update()
    {
        if (active && isControlling)
        {
            Vector2 input = InputManager.playerControls.Player.MenuNavigation.ReadValue<Vector2>();
            input.y *= -1;
            if (input.sqrMagnitude < 0.1f)
            {
                wasHoldingRight = false;
                wasHoldingUp = false;
            }
            else
            {
                NavigateMenu(input);
            }

            if (InputManager.playerControls.Player.Submit.WasPressedThisFrame() && !pressedThisFrame)
            {
                if (selectedButton != null)
                {
                    pressedThisFrame = true;
                    selectedButton.Press();
                }
            }
            else
            {
                pressedThisFrame = false;
            }
        }
    }

    public void NavigateMenu(Vector2 input)
    {
        bool inputDirty = false;
        int oldSelectedY = selectedY;
        int oldSelectedX = selectedX;

        holdingUp = Mathf.Abs(input.y) > 0.5f;
        holdingRight = Mathf.Abs(input.x) > 0.5f;

        if (holdingUp || holdingRight)
        {
            if (selectedButton == null)
            {
                selectedX = 0;
                selectedY = 0;
                wasHoldingUp = holdingUp;
                wasHoldingRight = holdingRight;
                SelectFirst();
                return;
            }
        }

        if (ignoreInputThisFrame)
        {
            ignoreInputThisFrame = false;
            wasHoldingUp = holdingUp;
            wasHoldingRight = holdingRight;
            return;
        }

        if (holdingUp)
        {
            if (!wasHoldingUp)
            {
                int up = holdingUp ? (int)Mathf.Sign(input.y) : 0;
                inputDirty = true;

                if (up > 0)
                {
                    selectedButton.NavigateDown();
                }
                else
                {
                    selectedButton.NavigateUp();
                }
            }

            if (input.y > 0)
            {
                selectedButton.NavigateDownHold();
            }
            else
            {
                selectedButton.NavigateUpHold();
            }
        }

        wasHoldingUp = holdingUp;

        if (holdingRight && !wasHoldingRight)
        {
            if (!wasHoldingRight)
            {
                int right = holdingRight ? (int)Mathf.Sign(input.x) : 0;
                inputDirty = true;

                if (right > 0)
                {
                    selectedButton.NavigateRight();
                }
                else
                {
                    selectedButton.NavigateLeft();
                }
            }

            if (input.x > 0)
            {
                selectedButton.NavigateRightHold();
            }
            else
            {
                selectedButton.NavigateLeftHold();
            }
        }

        wasHoldingRight = holdingRight;

        if (inputDirty)
        {
            if (selectedButton == null)
            {
                selectedX = 0;
                selectedY = 0;
                SelectFirst();
            }
        }
    }

    public void Deselect()
    {
        SelectButton(null);
    }

    public void SelectMenuButton(int x, int y)
    {
        if (selectedButton != null && selectedButton.forceSelect)
        {
            return;
        }
        selectedX = x;
        selectedY = y;
        SelectButton(rows[selectedY].elements[selectedX]);
    }

    public void OnDisable()
    {
        if (active)
        {
            Deactivate();
        }
    }

    public void OnEnable()
    {
        if (alwaysActivated && !active)
        {
            Activate();
        }
    }

    public void Activate()
    {
        active = true;

        OnActivate();

        for (int i = 0; i < rows.Count; i++)
        {
            for (int j = 0; j < rows[i].elements.Count; j++)
            {
                MenuButton currentButton = rows[i].elements[j];
                currentButton.activeMenu = this;
                currentButton.positionY = i;
                currentButton.positionX = j;
            }
        }
    }

    public void Deactivate()
    {
        active = false;

        OnDeactivate();

        for (int i = 0; i < rows.Count; i++)
        {
            for (int j = 0; j < rows[i].elements.Count; j++)
            {
                MenuButton currentButton = rows[i].elements[j];
                currentButton.activeMenu = null;
            }
        }
    }

    public void MouseOverButton(int x, int y)
    {
        if (activeWithMouse && InputManager.controlMode == InputManager.ControlMode.KeyboardMouse)
        {
            SelectMenuButton(x, y);
        }
    }

    public void MouseExitButton(MenuButton menuButton)
    {
        if (InputManager.controlMode == InputManager.ControlMode.KeyboardMouse && activeWithMouse && menuButton == selectedButton)
        {
            Deselect();
        }
    }

    public void MouseClickButton(MenuButton menuButton)
    {
        if (activeWithMouse)
        {
            menuButton.Press();
        }
    }

    public void OnActivate()
    {
        if (active)
        {
            activatedThisFrame = true;
        }
    }

    public void LateUpdate()
    {
        if (activatedThisFrame && isControlling && active)
        {
            activatedThisFrame = false;
            SelectFirst();
        }

        if (activatedThisFrame && active)
        {
            activatedThisFrame = false;
        }
    }

    public void HideControls()
    {
        OnDeactivate();
    }

    public void ShowControls()
    {
        OnActivate();
    }

    public void OnDeactivate()
    {
        SelectButton(null);
    }

    public static void SelectButton(MenuButton button)
    {
        if (selectedButton != null)
        {
            selectedButton.Deselect();
        }
        if (button != null)
        {
            button.Select();
        }
    }

    public void SelectFirst()
    {
        if (rows.Count > 0 && rows[0].elements.Count > 0)
        {
            SelectMenuButton(0, 0);
        }
    }

    public void ClearButtons()
    {
        foreach (ButtonRow row in rows)
        {
            row.elements.Clear();
        }
        rows.Clear();
    }

    public void NavigateLeft()
    {
        NavigateHorizontal(-1);
    }

    public void NavigateRight()
    {
        NavigateHorizontal(1);
    }

    public void NavigateUp()
    {
        NavigateVertical(-1);
    }

    public void NavigateDown()
    {
        NavigateVertical(1);
    }

    public void NavigateVertical(int direction)
    {
        selectedY += direction;

        ValidateY();
        ValidateX();

        SelectMenuButton(selectedX, selectedY);
    }

    public void NavigateHorizontal(int direction)
    {
        selectedX += direction;

        ValidateX();

        SelectMenuButton(selectedX, selectedY);
    }

    public void ValidateX()
    {
        if (selectedX >= rows[selectedY].elements.Count)
        {
            selectedX = 0;
        }
        if (selectedX < 0)
        {
            selectedX = rows[selectedY].elements.Count - 1;
        }
    }

    public void ValidateY()
    {
        if (selectedY >= rows.Count)
        {
            selectedY = 0;
        }
        if (selectedY < 0)
        {
            selectedY = rows.Count - 1;
        }
    }
}

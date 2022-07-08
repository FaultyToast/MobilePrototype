using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
using System;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class InputManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Initialize()
    {
        GameObject newInputManager = new GameObject("InputManager");
        newInputManager.AddComponent<InputManager>();
    }

    private static InputManager _instance;
    public static InputManager instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject newInputManager = new GameObject("InputManager");
                newInputManager.AddComponent<InputManager>();
            }
            return _instance;
        }
    }

    public void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        playerControls = new PlayerControls();
        playerControls.Enable();
        SceneManager.sceneLoaded += RemoveControlModeListeners;
    }

    public enum ControlMode
    {
        KeyboardMouse,
        Controller
    }

    // Input
    public static ControlMode controlMode = ControlMode.KeyboardMouse;
    private static string currentLayoutLocale = "kbm";
    public static PlayerControls playerControls;
    private InputDevice lastDevice;
    public bool isInTabMode = false;

    public void Start()
    {
        InputSystem.onEvent += OnInputSystemEvent;

        SwitchControlMode(controlMode, currentLayoutLocale);
    }

    public void RemoveControlModeListeners(Scene scene, LoadSceneMode loadSceneMode)
    {
        //onControlModeSwitched.RemoveAllListeners();
    }

    public void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (lastDevice == device)
            return;

        // Some devices like to spam events like crazy.
        // Example: PS4 controller on PC keeps triggering events without meaningful change.
        var eventType = eventPtr.type;
        if (eventType == StateEvent.Type)
        {
            // Go through the changed controls in the event and look for ones actuated
            // above a magnitude of a little above zero.
            bool valid = false;
            foreach (var control in eventPtr.EnumerateChangedControls(device: device, magnitudeThreshold: 0.0001f))
            {
                valid = true;
                break;
            }

            if (!valid)
            {
                return;
            }
        }


        lastDevice = device;

        if (device is Keyboard || device is Mouse)
        {
            SwitchControlMode(ControlMode.KeyboardMouse, "kbm");
        }
        if (device is Gamepad)
        {
            if (device is DualShockGamepad)
            {
                SwitchControlMode(ControlMode.Controller, "ps4");
            }
            else
            {
                SwitchControlMode(ControlMode.Controller, "xbox");
            }
        }
    }

    public static UnityEvent<ControlMode> onControlModeSwitched = new UnityEvent<ControlMode>();

    public void SwitchControlMode(ControlMode newControlMode, string layoutLocale)
    {
        if (newControlMode != controlMode)
        {
            controlMode = newControlMode;
            onControlModeSwitched.Invoke(newControlMode);
            UpdateCursorVisibility();
        }

        switch (newControlMode)
        {
            case ControlMode.Controller:
                {
                    playerControls.bindingMask = InputBinding.MaskByGroup(playerControls.GamepadScheme.bindingGroup);
                    break;
                }
            case ControlMode.KeyboardMouse:
                {
                    playerControls.bindingMask = InputBinding.MaskByGroup(playerControls.KeyboardMouseScheme.bindingGroup);
                    break;
                }
        }

        if (layoutLocale == "kbm")
        {
            PromptStringTable.instance.UpdatePromptType(PromptStringTable.PromptType.KeyboardMouse);
        }
        if (layoutLocale == "xbox")
        {
            PromptStringTable.instance.UpdatePromptType(PromptStringTable.PromptType.Xbox);
        }
        if (layoutLocale == "ps4")
        {
            PromptStringTable.instance.UpdatePromptType(PromptStringTable.PromptType.Playstation);
        }

        SetLayoutLocale(layoutLocale);
    }

    public void UpdateCursorVisibility()
    {
        if ((isInTabMode || GameManager.instance == null || (GameManager.instance != null && GameManager.instance.cursorUsed)) && controlMode != ControlMode.Controller)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void Update()
    {
        //Cursor Mode
        if (playerControls.Player.FreeCursor.IsPressed() && !isInTabMode)
        {
            isInTabMode = true;
            UpdateCursorVisibility();
        }

        if (!playerControls.Player.FreeCursor.IsPressed() && isInTabMode)
        {
            isInTabMode = false;
            UpdateCursorVisibility();
        }
    }

    public void SetLayoutLocale(string locale)
    {
        if (locale != currentLayoutLocale)
        {
            LoadLocale(locale);
        }
        currentLayoutLocale = locale;
    }

    public void LoadLocale(string languageIdentifier)
    {
        LocalizationSettings settings = LocalizationSettings.Instance;
        LocaleIdentifier localeCode = new LocaleIdentifier(languageIdentifier);//can be "en" "de" "ja" etc.
        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
        {
            Locale aLocale = LocalizationSettings.AvailableLocales.Locales[i];
            LocaleIdentifier anIdentifier = aLocale.Identifier;
            if (anIdentifier == localeCode)
            {
                LocalizationSettings.SelectedLocale = aLocale;
            }
        }
    }
}

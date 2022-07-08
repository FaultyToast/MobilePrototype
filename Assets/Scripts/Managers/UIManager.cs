using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    public static UIManager instance
    {
        get
        {
            return _instance;
        }
    }

    void Awake()
    {
        _instance = this;

        HideCrosshair();
    }

    public float bossCount = 0;

    void Start()
    {
        interactPrompt.text.enabled = false;
        runUIEnabled = true;
        spectatingText.enabled = false;
        objectiveText.enabled = false;

        if (!showBossHealthBar)
        {
            bossHealthbar.canvasGroup.alpha = 0;
        }
    }

    private bool _runUIEnabled = false;
    public bool runUIEnabled
    {
        get
        {
            return _runUIEnabled;
        }
        set
        {
            _runUIEnabled = value;
            if (fadeRunUICoroutine != null)
            {
                StopCoroutine(fadeRunUICoroutine);
            }

            //fadeRunUICoroutine = StartCoroutine(FadeCanvasGroup(0.5f, runUIEnabled, runUIGroup));

        }
    }

    private bool showBossHealthBar = false;

    public void ShowBossHealthbar()
    {
        showBossHealthBar = true;
        StartCoroutine(FadeCanvasGroup(0.25f, true, bossHealthbar.canvasGroup));
    }

    public void HideBossHealthBar()
    {
        bossCount -= 1;

        if(bossCount <= 0)
        {
            showBossHealthBar = false;
            StartCoroutine(FadeCanvasGroup(0.25f, false, bossHealthbar.canvasGroup));
        }
    }

    public void Update()
    {
        if (InputManager.playerControls.Player.StatCardMenu.WasPressedThisFrame())
        {
            if (tabMenu.isOpen)
            {
                tabMenu.Close();
            }
            else if (GameManager.instance.openMenus.Count == 0)
            {
                tabMenu.Open();
            }
        }

        if (InputManager.playerControls.Player.Pause.WasPressedThisFrame())
        {
            if (pauseMenu.isOpen)
            {
                pauseMenu.Close();
            }
            else if (GameManager.instance.openMenus.Count == 0)
            {
                pauseMenu.Open();
            }
        }
    }

    public Healthbar bossHealthbar;
    public Healthbar playerHealthbar;
    public Healthbar expBar;

    public InteractPrompt interactPrompt;
    public TextMeshProUGUI globalLevelCounter;
    public ToolTip toolTip;
    public CanvasGroup runUIGroup;
    public Menu tabMenu;
    public TextMeshProUGUI skillPointCounter;
    public TextMeshProUGUI spectatingText;
    public TextMeshProUGUI objectiveText;
    public Image crosshairImage;
    public Camera UICamera;
    private Coroutine fadeRunUICoroutine;
    public TutorialManager tutorialManager;
    public ObjectiveTracker objectiveTracker;
    public Menu pauseMenu;

    public void ShowCrosshair()
    {
        crosshairImage.enabled = true;
    }

    public void HideCrosshair()
    {
        crosshairImage.enabled = false;
    }


    public IEnumerator FadeCanvasGroup(float time, bool fadeIn, CanvasGroup group)
    {
        if (fadeIn)
        {
            
            while (group.alpha < 1)
            {
                group.alpha += Time.deltaTime / time;
                yield return null;
            }
        }
        else
        {
            while (group.alpha > 0)
            {
                group.alpha -= Time.deltaTime / time;
                yield return null;
            }
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialTriggerType
    {
        OnStart,
        OnMove,
        OnRoomEntered,
        OnHurt,
        OnSpellCollected,
        OnItemCollected,
        OnLevelUp,
        OnJump,
        OnHeal,
        OnDodge,
        OnBlock,
        OnAttackConnected,
        EnemyLaunchedEvent,
    }

    public enum EndPromptType
    {
        Trigger,
        Time
    }

    [System.Serializable]
    public class TutorialPrompt
    {
        public string name;
        public List<CanvasGroup> features;
        public TutorialTriggerType startTrigger = TutorialTriggerType.OnStart;
        public EndPromptType endPromptType = EndPromptType.Time;
        public List<TutorialTriggerType> endTriggers;
        public float fadeDelay;
        public LayoutGroup layoutGroup;

        [System.NonSerialized] public UnityAction startAction;
        [System.NonSerialized] public List<UnityAction> endActions = new List<UnityAction>();

        [System.NonSerialized] public bool triggered = false;
        [System.NonSerialized] public bool active = false;

        [System.NonSerialized] public bool activatedThisFrame = false;
    }

    [System.NonSerialized] public UnityEvent startEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent moveEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent roomEnteredEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent hurtEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent spellCollectedEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent itemCollectedEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent levelUpEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent jumpEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent healEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent dodgeEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent blockEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent attackConnectedEvent = new UnityEvent();
    [System.NonSerialized] public UnityEvent enemyLaunchedEvent = new UnityEvent();

    public List<TutorialPrompt> tutorialPrompts;

    public float fadeInTime = 0.25f;
    public float fadeOutTime = 1f;
    public CanvasGroup canvasGroup;
    private bool activatedThisFrame = false;

    public void Awake()
    {
        foreach(TutorialPrompt prompt in tutorialPrompts)
        {
            UnityEvent startEvent = GetTriggerEvent(prompt.startTrigger);
            prompt.startAction = delegate { ShowTutorialPrompt(prompt, startEvent); };
            startEvent.AddListener(prompt.startAction);

            if (prompt.endPromptType == EndPromptType.Trigger)
            {
                for (int i = 0; i < prompt.endTriggers.Count; i++)
                {
                    UnityEvent endEvent = GetTriggerEvent(prompt.endTriggers[i]);
                    int index = i;
                    UnityAction endAction = delegate { 
                        BeginDelayedFade(prompt, endEvent, index); 
                    };
                    prompt.endActions.Add(endAction);
                    endEvent.AddListener(endAction);
                }
            }

            foreach(CanvasGroup feature in prompt.features)
            {
                feature.alpha = 0;
                feature.gameObject.SetActive(false);
            }
        }
    }

    public UnityEvent GetTriggerEvent(TutorialTriggerType trigger)
    {
        switch(trigger)
        {
            case TutorialTriggerType.OnStart: //done
                {
                    return startEvent;
                }
            case TutorialTriggerType.OnMove: //done
                {
                    return moveEvent;
                }
            case TutorialTriggerType.OnRoomEntered: //done
                {
                    return roomEnteredEvent;
                }
            case TutorialTriggerType.OnHurt: //done
                {
                    return hurtEvent;
                }
            case TutorialTriggerType.OnSpellCollected: //done
                {
                    return spellCollectedEvent;
                }
            case TutorialTriggerType.OnItemCollected: //done
                {
                    return itemCollectedEvent;
                }
            case TutorialTriggerType.OnLevelUp: //done
                {
                    return levelUpEvent;
                }
            case TutorialTriggerType.OnJump: //done
                {
                    return jumpEvent;
                }
            case TutorialTriggerType.OnHeal: //done
                {
                    return healEvent;
                }
            case TutorialTriggerType.OnDodge: //done
                {
                    return dodgeEvent;
                }
            case TutorialTriggerType.OnBlock: //done
                {
                    return blockEvent;
                }
            case TutorialTriggerType.OnAttackConnected: //done
                {
                    return attackConnectedEvent;
                }
            case TutorialTriggerType.EnemyLaunchedEvent:
                {
                    return enemyLaunchedEvent;
                }


            default:
                {
                    return startEvent;
                }
        }
    }

    public void Update()
    {
        if (Settings.tutorialsEnabled)
        {
            canvasGroup.alpha = 1f;
        }
        else
        {
            canvasGroup.alpha = 0f;
        }
    }

    public void Start()
    {
        startEvent.Invoke();
    }

    public void LateUpdate()
    {
        if (activatedThisFrame)
        {
            foreach (TutorialPrompt prompt in tutorialPrompts)
            {
                prompt.activatedThisFrame = false;
            }
            activatedThisFrame = false;
        }
    }

    public void ShowTutorialPrompt(TutorialPrompt tutorialPrompt, UnityEvent eventToUnregister = null)
    {
        if (eventToUnregister != null)
        {
            eventToUnregister.RemoveListener(tutorialPrompt.startAction);
        }

        if (tutorialPrompt.triggered)
        {
            return;
        }

        tutorialPrompt.active = true;
        tutorialPrompt.triggered = true;
        activatedThisFrame = true;
        tutorialPrompt.activatedThisFrame = true;
        StartCoroutine(FadePromptIn(tutorialPrompt));

        if (tutorialPrompt.endPromptType == EndPromptType.Time)
        {
            BeginDelayedFade(tutorialPrompt, null, 0, true);
        }
    }

    public void HideTutorialPrompt(TutorialPrompt tutorialPrompt)
    {
        tutorialPrompt.active = false;
        StartCoroutine(FadePromptOut(tutorialPrompt));
    }

    public void BeginDelayedFade(TutorialPrompt tutorialPrompt, UnityEvent eventToUnregister = null, int index = 0, bool overrideActivationThisFrame = false)
    {
        if (tutorialPrompt.activatedThisFrame && !overrideActivationThisFrame)
        {
            return;
        }
        if (tutorialPrompt.triggered && eventToUnregister != null)
        {
            eventToUnregister.RemoveListener(tutorialPrompt.endActions[index]);
        }
        if (!tutorialPrompt.active)
        {
            return;
        }

        StartCoroutine(DelayedFadeOut(tutorialPrompt));
    }

    public IEnumerator DelayedFadeOut(TutorialPrompt tutorialPrompt)
    {
        yield return new WaitForSeconds(tutorialPrompt.fadeDelay);
        HideTutorialPrompt(tutorialPrompt);
    }

    public IEnumerator FadePromptIn(TutorialPrompt tutorialPrompt)
    {

        foreach (CanvasGroup feature in tutorialPrompt.features)
        {
            if (tutorialPrompt.layoutGroup != null)
            {
                tutorialPrompt.layoutGroup.gameObject.SetActive(true);
            }
            feature.gameObject.SetActive(true);
        }

        if (tutorialPrompt.layoutGroup != null)
        {
            tutorialPrompt.layoutGroup.CalculateLayoutInputHorizontal();
            tutorialPrompt.layoutGroup.CalculateLayoutInputVertical();
            tutorialPrompt.layoutGroup.SetLayoutHorizontal();
            tutorialPrompt.layoutGroup.SetLayoutVertical();
            Canvas.ForceUpdateCanvases();
            tutorialPrompt.layoutGroup.enabled = false;
            tutorialPrompt.layoutGroup.enabled = true;
            tutorialPrompt.layoutGroup.gameObject.SetActive(false);
            tutorialPrompt.layoutGroup.gameObject.SetActive(true);
        }

        while (tutorialPrompt.features[0].alpha < 1)
        {
            if (!tutorialPrompt.active)
            {
                yield break;
            }
            foreach (CanvasGroup feature in tutorialPrompt.features)
            {
                feature.alpha += Time.deltaTime / fadeInTime;
            }

            yield return null;
        }

        foreach (CanvasGroup feature in tutorialPrompt.features)
        {
            feature.alpha = 1f;
        }
    }

    public IEnumerator FadePromptOut(TutorialPrompt tutorialPrompt)
    {
        while (tutorialPrompt.features[0].alpha > 0)
        {
            if (tutorialPrompt.active)
            {
                yield break;
            }
            foreach (CanvasGroup feature in tutorialPrompt.features)
            {
                feature.alpha -= Time.deltaTime / fadeOutTime;
            }

            yield return null;
        }

        foreach (CanvasGroup feature in tutorialPrompt.features)
        {
            feature.alpha = 0f;
            feature.gameObject.SetActive(false);
        }

        if (tutorialPrompt.layoutGroup != null)
        {
            tutorialPrompt.layoutGroup.CalculateLayoutInputHorizontal();
            tutorialPrompt.layoutGroup.CalculateLayoutInputVertical();
            tutorialPrompt.layoutGroup.SetLayoutHorizontal();
            tutorialPrompt.layoutGroup.SetLayoutVertical();
            Canvas.ForceUpdateCanvases();
            tutorialPrompt.layoutGroup.enabled = false;
            tutorialPrompt.layoutGroup.enabled = true;
            tutorialPrompt.layoutGroup.gameObject.SetActive(false);
            tutorialPrompt.layoutGroup.gameObject.SetActive(true);
        }
    }
}

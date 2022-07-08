using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class Healthbar : MonoBehaviour
{
    public RectTransform fillTransform;
    public Graphic hurtFill;
    public TextMeshProUGUI amountText;

    private Vector2 emptyPos;
    private float barWidth;
    private float barHeight;
    private float barSize;
    private float percent;
    private float lastValue;

    float hurtBarFadeDelay = 0.5f;
    float hurtBarSlideSpeedPercent = 2f;

    [System.NonSerialized] public CanvasGroup canvasGroup;

    GameObject currentTarget;

    public UnityEvent<float> onHealthBarChanged;

    public enum FromDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public FromDirection fromDirection = FromDirection.Left;

    public void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Start()
    {
        ResetBarWidth();
    }

    public void ResetBarWidth()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        barWidth = fillTransform.rect.width;
        if (barWidth == 0)
        {
            Canvas.ForceUpdateCanvases();
            barWidth = rectTransform.rect.width;
        }

        barHeight = fillTransform.rect.height;
        if (barHeight == 0)
        {
            Canvas.ForceUpdateCanvases();
            barHeight = rectTransform.rect.height;
        }

        switch (fromDirection)
        {
            case FromDirection.Left:
                {
                    barSize = barWidth;
                    emptyPos.y = fillTransform.localPosition.y;
                    emptyPos.x = -barWidth - barWidth * 0.01f;
                    break;
                }
            case FromDirection.Down:
                {
                    barSize = barHeight;
                    emptyPos.y = -barHeight - barHeight * 0.01f;
                    emptyPos.x = fillTransform.localPosition.x;
                    break;
                }
        }


        SetPercent(percent, percent, currentTarget, true, true);

        if (hurtFill != null)
        {
            ResetHurtFill();
        }
    }

    public void ResetHurtFill()
    {
        StopAllCoroutines();
        hurtFill.enabled = false;
    }

    // Alternative that allows for setting value with healthbar text
    public void SetValue(float maxValue, float newValue, float lastPercent, GameObject target)
    {
        SetPercent(newValue / maxValue, lastPercent, target, false);

        // Prevent rounding to 0 when still alive
        if (newValue < 1 && newValue > 0)
        {
            newValue = 1f;
        }

        newValue = Mathf.Round(newValue);

        if (amountText != null)
        {
            amountText.text = Mathf.Round(newValue) + " / " + Mathf.Round(maxValue);
        }

        if (hurtFill != null && !hurtFill.enabled && gameObject.activeSelf && newValue < lastValue)
        {
            BeginShowHurtBar(lastPercent);
        }

        lastValue = newValue;
    }

    public void SetPercent(float percent, float lastPercent, GameObject target, bool handleHurtFill = true, bool forceOverride = false)
    {
        if (Mathf.Approximately(this.percent, percent) && !forceOverride)
        {
            return;
        }
        if (hurtFill != null && !ReferenceEquals(target, currentTarget))
        {
            ResetHurtFill();
        }
        currentTarget = target;

        if (hurtFill != null && !hurtFill.enabled && gameObject.activeSelf && handleHurtFill && percent < lastPercent)
        {
            BeginShowHurtBar(lastPercent);
        }

        this.percent = percent;

        Vector2 targetPos = Vector2.Lerp(emptyPos, Vector2.zero, percent);
        fillTransform.localPosition = targetPos;

        onHealthBarChanged.Invoke(percent);
    }

    public void BeginShowHurtBar(float lastPercent)
    {
        hurtFill.transform.localPosition = Vector2.Lerp(emptyPos, Vector2.zero, lastPercent);
        StartCoroutine(ShowHurtBar());
    }

    public IEnumerator ShowHurtBar()
    {
        hurtFill.enabled = true;


        yield return new WaitForSeconds(hurtBarFadeDelay);

        while (Vector3.Distance(hurtFill.transform.position, fillTransform.position) > hurtBarSlideSpeedPercent * barSize * Time.deltaTime)
        {
            hurtFill.transform.position = Vector3.MoveTowards(hurtFill.transform.position, fillTransform.position, hurtBarSlideSpeedPercent * barSize * Time.deltaTime);
            yield return null;
        }
        hurtFill.enabled = false;
    }
}

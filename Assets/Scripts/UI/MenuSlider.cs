using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MenuSlider : MenuButton
{
    public float percent = 100f;
    public string label;

    public RectTransform background;
    public RectTransform fill;

    public Settings.FloatSetting setting;

    [System.NonSerialized] public UnityAction<float> OnValueChanged;

    private float width;
    private bool grabbed;

    public RectTransform rectTransform;

    public void Start()
    {
        width = background.sizeDelta.x;
        SetPercent(percent);
        rectTransform = GetComponent<RectTransform>();
    }

    public override void NavigateLeft()
    {
        percent -= 10f;
        percent = Mathf.CeilToInt(percent / 10f) * 10f;
        percent = Mathf.Max(0f, percent);
        SetPercent(percent);
    }

    public override void NavigateRight()
    {
        percent += 10f;
        percent = Mathf.FloorToInt(percent / 10f) * 10f;
        percent = Mathf.Min(100f, percent);
        SetPercent(percent);
    }

    public void SetPercent(float percent)
    {
        this.percent = percent;
        OnValueChanged.Invoke(percent);
        Vector3 position = fill.transform.localPosition;
        position.x = Mathf.Lerp(-width * 1.01f, 0f, percent / 100f);

        fill.transform.localPosition = position;

    }

    public override void Update()
    {
        if (selected && Mouse.current.leftButton.isPressed)
        {
            forceSelect = true;
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            SetPercent(Mathf.InverseLerp(corners[0].x, corners[2].x, Input.mousePosition.x) * 100f);
        }
        else
        {
            forceSelect = false;
        }

        base.Update();
    }

    public override void OnPress()
    {
    }
}

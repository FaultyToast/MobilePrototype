using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolTip : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public LayoutGroup layoutGroup;
    [System.NonSerialized] public RectTransform pointOverride;

    public void Awake()
    {
        enabled = false;
    }

    public void LateUpdate()
    {
        if (enabled)
        {
            if (pointOverride == null)
            {
                var screenPoint = Input.mousePosition;
                screenPoint.z = 100.0f; //distance of the plane from the camera
                transform.position = UIManager.instance.UICamera.ScreenToWorldPoint(screenPoint);
            }
            else
            {
                transform.position = pointOverride.position;
            }
        }
    }

    public void OnEnable()
    {
        canvasGroup.alpha = 1;
    }

    public void OnDisable()
    {
        canvasGroup.alpha = 0;
    }       

    public void ShowToolTip(string title, string description)
    {
        if (pointOverride != null)
        {
            return;
        }
        titleText.text = title;
        descriptionText.text = description;
        enabled = true;
        layoutGroup.Refresh();
    }

    public void HideToolTip()
    {
        if (pointOverride != null)
        {
            return;
        }
        enabled = false;
    }


    public void OverridePoint(RectTransform pointOverride)
    {
        this.pointOverride = pointOverride;
    }

    public void ClearPointOverride()
    {
        this.pointOverride = null;
    }
}

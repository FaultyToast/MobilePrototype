using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class UIMatchChildSize : MonoBehaviour
{
    [System.NonSerialized] public RectTransform rectTransform;
    public RectTransform child;

    public void Update()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        if (child != null)
        {
            Vector2 rect = rectTransform.sizeDelta;
            float width = child.sizeDelta.x * rectTransform.localScale.x;
            float height = child.sizeDelta.y * rectTransform.localScale.y;
            float pivotX = 0.5f / (1f - rectTransform.localScale.x);
            //float pivotY = 0.5f / (rectTransform.localScale.y);
            //rectTransform.pivot = new Vector2(pivotX, rectTransform.pivot.y);
            rectTransform.sizeDelta = new Vector2(width, height);
        }
    }
}

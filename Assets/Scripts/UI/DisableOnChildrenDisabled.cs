using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableOnChildrenDisabled : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    public void OnEnable()
    {
        canvasGroup.alpha = 1;
        foreach(Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                return;
            }
        }
        canvasGroup.alpha = 0;
    }
}

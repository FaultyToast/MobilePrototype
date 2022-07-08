using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

static public class VectorExtensions
{
    static public Vector3 XZPlane(this Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}

static public class LayoutGroupExtensions
{
    static public void Refresh(this LayoutGroup layoutGroup)
    {
        layoutGroup.CalculateLayoutInputHorizontal();
        layoutGroup.CalculateLayoutInputVertical();
        layoutGroup.SetLayoutHorizontal();
        layoutGroup.SetLayoutVertical();
        Canvas.ForceUpdateCanvases();
        layoutGroup.enabled = false;
        layoutGroup.enabled = true;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "HitFlashData", menuName = "ScriptableObjects/HitFlashData", order = 1)]
public class HitFlashData : ScriptableObject
{
    public float hitFlashLength;
    public AnimationCurve hitFlashCurve;
}

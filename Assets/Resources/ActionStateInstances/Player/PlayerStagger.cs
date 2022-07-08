using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PlayerStagger", order = 1)]
public class PlayerStagger : StaggerState
{
    public AnimationCurve moveBackwardCurve;
    public float moveBackwardValue;
}

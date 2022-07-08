using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtility
{
    public static bool ConeCheck(Vector3 origin, Vector3 target, Vector3 direction, float areaDecimal, float maxDistance)
    {
        if (Vector3.Distance(origin, target) > maxDistance)
        {
            return false;
        }

        if (Vector3.Dot((target - origin).normalized, direction) < 1 - areaDecimal)
        {
            return false;
        }
        return true;
    }
}

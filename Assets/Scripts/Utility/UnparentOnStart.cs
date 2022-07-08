using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnparentOnStart : MonoBehaviour
{
    private void Start()
    {
        gameObject.transform.parent = null;
    }
}

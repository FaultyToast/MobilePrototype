using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasBillboard : MonoBehaviour
{
    private Transform camTransform;

    Quaternion originalRotation;

    void Start()
    {
        camTransform = GameManager.instance.mainCamera.transform;
        originalRotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = camTransform.rotation * originalRotation;
    }
}

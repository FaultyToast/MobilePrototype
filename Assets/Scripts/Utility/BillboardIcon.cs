using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class BillboardIcon : MonoBehaviour
{
    private Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = GameManager.instance.mainCamera;
    }

    // Update is called once per frame
    void Update()
    {

        transform.LookAt(mainCamera.transform, -Vector3.up);
    }
}

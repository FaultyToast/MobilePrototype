using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateConstantly : MonoBehaviour
{
    public float speed;
    public Vector3 axis;

    public void Update()
    {
        transform.Rotate(axis * speed * Time.deltaTime);
    }
}

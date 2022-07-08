using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bodyRotation : MonoBehaviour
{
    public float speed;

    private Vector2 direction;
    public Transform target;
    private void Update()
    {
        direction = (target.position - transform.position).normalized;
        Quaternion lookrot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookrot, speed * Time.deltaTime);
    }
}

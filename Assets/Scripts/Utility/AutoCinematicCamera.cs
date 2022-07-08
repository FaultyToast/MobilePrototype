using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoCinematicCamera : MonoBehaviour
{
    Transform playerTransform;
    public float repositionDistance = 20f;
    public float angleDeviance = 20f;
    public float radiusMin = 5f;
    public float radiusMax = 15f;
    public float behindRadiusMultiplier = 0.5f;

    public Vector3 lastPlayerPosition;

    private Camera camera;

    private float age;

    public void Start()
    {
        playerTransform = GameManager.instance.localPlayerCharacter.bodyCenter;
        ChangePosition();
        lastPlayerPosition = playerTransform.position;
        camera = GetComponent<Camera>();
    }

    public void Update()
    {
        age += Time.deltaTime;
        Vector2 playerPosition = camera.WorldToViewportPoint(playerTransform.position);
        if (Vector3.Distance(playerTransform.position, transform.position) > repositionDistance || OutOfBounds(playerPosition, 0.2f) || (OutOfBounds(playerPosition, -0.2f) && age > 1f) || age > 5f)
        {
            ChangePosition();
        }
        lastPlayerPosition = playerTransform.position;
    }

    public bool OutOfBounds(Vector2 position, float tolerance)
    {
        return position.x < -tolerance || position.x > 1f + tolerance || position.y < -tolerance || position.y > 1f + tolerance;
    }

    public void ChangePosition()
    {
        for (int i = 0; i < 50; i++)
        {
            Vector3 direction = Random.insideUnitSphere;
            float radius = Random.Range(radiusMin, radiusMax);
            Vector3 playerDirection = (playerTransform.position - lastPlayerPosition).XZPlane().normalized;

            if (direction.y < 0)
            {
                direction *= -1f;
            }
            direction.y *= 0.5f;
            direction = direction.normalized;
            transform.position = playerTransform.position + (direction * radius);
            transform.rotation = Quaternion.LookRotation(GetPointOnUnitSphereCap(-direction, angleDeviance));
            transform.position += playerDirection * Mathf.Lerp((radius + 1f) * behindRadiusMultiplier, (radius + 1f), Vector3.Dot(direction, playerDirection));
            if (Physics.Linecast(transform.position, playerTransform.position, FracturedUtility.terrainMask))
            {
                continue;
            }
            if (Physics.OverlapSphere(transform.position, 1f, FracturedUtility.terrainMask).Length == 0)
            {
                continue;
            }
        }

        age = 0f;
    }

    public static Vector3 GetPointOnUnitSphereCap(Quaternion targetDirection, float angle)
    {
        var angleInRad = Random.Range(0.0f, angle) * Mathf.Deg2Rad;
        var PointOnCircle = (Random.insideUnitCircle.normalized) * Mathf.Sin(angleInRad);
        var V = new Vector3(PointOnCircle.x, PointOnCircle.y, Mathf.Cos(angleInRad));
        return targetDirection * V;
    }
    public static Vector3 GetPointOnUnitSphereCap(Vector3 targetDirection, float angle)
    {
        return GetPointOnUnitSphereCap(Quaternion.LookRotation(targetDirection), angle);
    }
}

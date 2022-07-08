using System.Collections;
using UnityEngine;
using Mirror;
using Ara;

public class HomingOrbEffect : MonoBehaviour
{
    private float travelTime;
    public AnimationCurve movementCurve;
    public AraTrail trail;
    private float timer;
    [System.NonSerialized] public Transform target;

    private Vector3 startPosition;
    private Vector3 lastKnownTargetPosition;

    public Vector3 startVelocityMin = new Vector3(0, 0, 0);
    public Vector3 startVelocityMax = new Vector3(0, 0, 0);
    public Vector3 endVelocityMin = new Vector3(0, 0, 0);
    public Vector3 endVelocityMax = new Vector3(0, 0, 0);

    private Vector3 startVelocity;
    private Vector3 endVelocity;

    public void Start()
    {
        EffectData effectData = GetComponent<Effect>().effectData;
        target = effectData.genericCharacterReference.GetComponent<CharacterMaster>().bodyCenter;
        travelTime = effectData.genericFloat;
        startPosition = transform.position;

        startVelocity.x = Mathf.Lerp(startVelocityMin.x, startVelocityMax.x, UnityEngine.Random.value);
        startVelocity.y = Mathf.Lerp(startVelocityMin.y, startVelocityMax.y, UnityEngine.Random.value);
        startVelocity.z = Mathf.Lerp(startVelocityMin.z, startVelocityMax.z, UnityEngine.Random.value);
        endVelocity.x = Mathf.Lerp(endVelocityMin.x, endVelocityMax.x, UnityEngine.Random.value);
        endVelocity.y = Mathf.Lerp(endVelocityMin.y, endVelocityMax.y, UnityEngine.Random.value);
        endVelocity.z = Mathf.Lerp(endVelocityMin.z, endVelocityMax.z, UnityEngine.Random.value);
    }

    public void Update()
    {
        timer += Time.deltaTime;
        float lerp = Mathf.InverseLerp(0, travelTime, timer);
        float velocityLerp = movementCurve.Evaluate(lerp);
        if (target != null)
        {
            lastKnownTargetPosition = target.position; 
        }
        transform.position = Vector3.Lerp(startPosition + startVelocity * velocityLerp, lastKnownTargetPosition + endVelocity * (1 - velocityLerp), lerp);
        if (lerp >= 1)
        {
            DestroySelf();
        }

    }

    public void DestroySelf()
    {
        if (trail != null)
        {
            trail.emit = false; ;
            var destroyOnDelay = trail.gameObject.AddComponent<DestroyOnDelay>();
            destroyOnDelay.delay = 1f;
            destroyOnDelay.Activate();
            trail.transform.SetParent(null);
        }
        Destroy(gameObject);
    }
}
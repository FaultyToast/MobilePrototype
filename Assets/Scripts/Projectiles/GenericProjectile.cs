using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GenericProjectile : NetworkBehaviour
{
    private bool targetFound = false;
    private Vector3 velocity;

    public float maxSpeed = 100f;
    private float speed = 0f;

    public float rotationSpeed = 150f;

    // The maximum amount of time this projectile can rotate after activation
    public float maxRotationTime = 0f;

    private Transform target = null;
    [System.NonSerialized] public Transform idlePosition;

    public HitboxGroup hitboxGroup;
    public HitboxGroup terrainHitbox;
    public DamageComponent damageComponent;
    public Effect impactEffect;

    public AnimationCurve accelerationCurve;
    private float timeSinceActivation = 0f;

    private bool destroyed = false;

    public readonly bool startActivated = false;
    private bool activated = false;

    public void Awake()
    {
        hitboxGroup.enabled = false;
        terrainHitbox.enabled = false;
        activated = startActivated;

        if (!hasAuthority)
        {
            return;
        }
    }

    public void Start()
    {
        if (FracturedUtility.HasEffectiveAuthority(netIdentity))
        {
            terrainHitbox.AddTerrainListener(DestroyProjectile);
            damageComponent.damageAttemptedCallback.AddListener(DestroyProjectile);
        }
    }

    public void Update()
    {
        if (!FracturedUtility.HasEffectiveAuthority(netIdentity))
        {
            return;
        }
        if (activated)
        {
            timeSinceActivation += Time.deltaTime;
            speed = accelerationCurve.Evaluate(timeSinceActivation) * maxSpeed;
            if (target != null && (timeSinceActivation <= maxRotationTime || Mathf.Approximately(maxRotationTime, 0f)))
            {
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(directionToTarget), rotationSpeed * Time.deltaTime);
            }

            velocity = speed * transform.forward;

            transform.position += velocity * Time.deltaTime;
        }
    }

    public void Fire(Transform newTarget)
    {
        activated = true;
        target = newTarget;

        terrainHitbox.enabled = true;
        StartCoroutine(DestroyAfterTime(5f));

        ActivateHitbox();
        if (NetworkServer.active)
        {
            RpcActivateHitbox();
        }
        else CmdActivateHitbox();
    }

    [Command]
    public void CmdActivateHitbox()
    {
        RpcActivateHitbox();
    }

    [ClientRpc]
    public void RpcActivateHitbox()
    {
        if (!FracturedUtility.HasEffectiveAuthority(netIdentity))
        {
            ActivateHitbox();
        }
    }

    public void ActivateHitbox()
    {
        hitboxGroup.enabled = true;
    }

    public void DestroyProjectile()
    {
        if (!destroyed)
        {
            destroyed = true;
            if (impactEffect != null)
            {
                EffectManager.CreateSimpleEffect(impactEffect, transform.position);
            }
            GameManager.instance.UnspawnProjectile(gameObject);
        }
    }

    public IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        DestroyProjectile();
    }
}

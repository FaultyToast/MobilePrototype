using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Projectile : MonoBehaviour
{
    Transform target;
    public float moveSpeed;
    public float rotationSpeed;
    public DamageComponent damagecomp;

    bool destroyed = false;

    private void Awake()
    {
        damagecomp = GetComponent<DamageComponent>();
        damagecomp.ownerMaster = NetworkClient.localPlayer.GetComponent<CharacterMaster>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (GameManager.instance.enemies.Count > 0)
        {
            target = GameManager.instance.enemies[Random.Range(0, GameManager.instance.enemies.Count)].bodyCenter;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            // Determine which direction to rotate towards
            Vector3 targetDirection = target.transform.position - transform.position;

            // The step size is equal to speed times frame time.
            float singleStep = rotationSpeed * Time.deltaTime;

            // Rotate the forward vector towards the target direction by one step
            Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, singleStep, 0.0f);

            // Calculate a rotation a step closer to the target and applies rotation to this object
            transform.rotation = Quaternion.LookRotation(newDirection);
        }
        //Move in facing direction
        transform.position = transform.position + transform.forward * moveSpeed * Time.deltaTime;
    }

    public void DestroySelf()
    {
        if(!destroyed)
        {
            StartCoroutine(DestroyTimer());
            destroyed = true;
        }
    }

    IEnumerator DestroyTimer()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}

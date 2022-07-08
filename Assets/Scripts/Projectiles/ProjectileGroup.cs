using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(ProjectileMaster))]
public class ProjectileGroup : NetworkBehaviour
{
    protected List<GameObject> projectiles = new List<GameObject>();
    public List<Transform> projectileLocations;

    public bool followPosition = true;
    public bool followRotation = false;
    public bool useGenericFire = true;

    private bool[] projectilesFired;

    protected bool fired = false;
    public float fireInterval = 0;

    public ProjectileGroupChild projectilePrefab;
    private ProjectileMaster projectileMaster;
    [System.NonSerialized] public CharacterMaster owner;
    [System.NonSerialized] public Transform followTarget;
    [System.NonSerialized] public Transform rotationTarget;

    public enum ParentDestroyedBehaviour
    {
        None,
        DestroySelf,
        Fire
    }

    public ParentDestroyedBehaviour parentDestroyedBehaviour;

    public virtual void Start()
    {
        projectileMaster = GetComponent<ProjectileMaster>();

        if (!FracturedUtility.HasEffectiveAuthority(netIdentity))
        {
            return;
        }

        if (projectileMaster.owner != null)
        {
            owner = GetComponent<ProjectileMaster>().owner.GetComponent<CharacterMaster>();
            owner.onDisable.AddListener(OwnerDisabled);
        }

        if ((followRotation || followPosition) && owner != null)
        {
            CharacterMaster ownerMaster = owner.GetComponent<CharacterMaster>();

            followTarget = ownerMaster.transform;
            transform.position = followTarget.position;

            rotationTarget = ownerMaster.modelPivot;
            transform.rotation = rotationTarget.rotation;
        }

        Vector3[] startingLocations = new Vector3[projectileLocations.Count];
        Quaternion[] startingRotations = new Quaternion[projectileLocations.Count];

        for (int i = 0; i < projectileLocations.Count; i++)
        {
            startingLocations[i] = projectileLocations[i].position;
            startingRotations[i] = projectileLocations[i].rotation;
        }

        SpawnProjectiles(startingLocations, startingRotations);
    }

    [Command(requiresAuthority = false)]
    public void SpawnProjectiles(Vector3[] locations, Quaternion[] rotations)
    {
        List<GameObject> spawnedProjectiles = new List<GameObject>();
        for (int i = 0; i < locations.Length; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab.gameObject, locations[i], rotations[i]);
            NetworkServer.Spawn(projectile, netIdentity.connectionToClient);
            projectile.GetComponent<ProjectileMaster>().owner = projectileMaster.owner;
            spawnedProjectiles.Add(projectile);
        }

        // Send to client vs server
        if (NetworkServer.active)
        {
            ReceiveProjectiles(spawnedProjectiles);
        }
        ClientRpcReceiveProjectiles(spawnedProjectiles);
    }

    [ClientRpc]
    public void ClientRpcReceiveProjectiles(List<GameObject> spawnedProjectiles)
    {
        if (!NetworkServer.active)
        {
            ReceiveProjectiles(spawnedProjectiles);
        }
    }

    public void ReceiveProjectiles(List<GameObject> spawnedProjectiles)
    {
        if (FracturedUtility.HasEffectiveAuthority(netIdentity))
        {
            for (int i = 0; i < spawnedProjectiles.Count; i++)
            {
                projectiles.Add(spawnedProjectiles[i]);
                spawnedProjectiles[i].GetComponent<ProjectileGroupChild>().parentTransform = projectileLocations[i];
            }
            projectilesFired = new bool[spawnedProjectiles.Count];
        }

        OnReceiveProjectiles(spawnedProjectiles);
    }

    public virtual void LateUpdate()
    {
        if (followPosition && followTarget != null)
        {
            transform.position = followTarget.position;
        }
        if (followRotation && followTarget != null)
        {
            transform.rotation = rotationTarget.rotation;
        }
    }

    public virtual void OnReceiveProjectiles(List<GameObject> spawnedProjectiles)
    {

    }

    public virtual void OwnerDisabled(NetworkConnection conn)
    {
        switch (parentDestroyedBehaviour)
        {
            case ParentDestroyedBehaviour.DestroySelf:
                {
                    DestroySelf();
                    break;
                }
            case ParentDestroyedBehaviour.Fire:
                {
                    Fire();
                    break;
                }
        }
    }

    public void OnDestroy()
    {
        if (NetworkClient.active)
        {
            OnFiredOrDestroyed();
        }
    }

    public void Fire(Transform target = null)
    {
        fired = true;
        OnFiredOrDestroyed();
        StartCoroutine(FireSequentially(target));
    }

    public IEnumerator FireSequentially(Transform target = null)
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (fireInterval > 0)
            {
                yield return new WaitForSeconds(fireInterval);
            }
            projectiles[i].GetComponent<ProjectileGroupChild>().connectedToParent = false;

            if (useGenericFire)
            {
                GenericFire(projectiles[i], target);
            }
            else FireBehaviour(projectiles[i]);
            projectilesFired[i] = true;
        }
        DestroySelf();
    }

    // Virtual behaviour for individual projectiles
    public virtual void FireBehaviour(GameObject projectile)
    {

    }

    // Generic fire for ease of creation
    public void GenericFire(GameObject projectile, Transform target)
    {
        projectile.GetComponent<GenericProjectile>().Fire(target);
    }

    public void DestroySelf()
    {
        // Delete all non-fired projectiles
        if (NetworkClient.active)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                if (projectilesFired.Length == 0 || !projectilesFired[i])
                {
                    GameManager.instance.UnspawnProjectile(projectiles[i]);
                }
            }

            GameManager.instance.UnspawnProjectile(gameObject);
        }

    }

    public virtual void OnFiredOrDestroyed()
    {

    }
}

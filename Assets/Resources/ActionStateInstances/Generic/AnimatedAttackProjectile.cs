using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "AnimatedAttackProjectile", menuName = "ActionStates/AnimatedAttackProjectile", order = 1)]
public class AnimatedAttackProjectile : AnimatedAttack
{
    public List<ProjectileSpawnInfo> projectiles;

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (NetworkServer.active)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                ProjectileSpawnInfo spawnInfo = projectiles[i];
                if (GetAnimationFrame() >= spawnInfo.fireFrame)
                {
                    GameObject spawnedPrefab = Instantiate(spawnInfo.prefabToSpawn);
                    Transform spawnTransform = characterMaster.childLocator.GetChild(spawnInfo.spawnPointChildName);
                    spawnedPrefab.transform.position = spawnTransform.position;
                    if (spawnInfo.matchPointRotation)
                    {
                        spawnedPrefab.transform.rotation = spawnTransform.rotation;
                    }
                    if (spawnInfo.assignDamageComponent)
                    {
                        AssignDamageComponent(spawnedPrefab);
                    }
                    OnInstantiate(spawnedPrefab);
                    NetworkServer.Spawn(spawnedPrefab);
                    OnSpawn(spawnedPrefab);
                    projectiles.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public void AssignDamageComponent(GameObject gameObject)
    {
        gameObject.GetComponent<ProjectileMaster>().owner = outer.netIdentity;
    }

    public virtual void OnInstantiate(GameObject spawnedPrefab)
    {

    }

    public virtual void OnSpawn(GameObject spawnedPrefab)
    {

    }

    [System.Serializable]
    public class ProjectileSpawnInfo
    {
        public GameObject prefabToSpawn;
        public string spawnPointChildName;
        public bool assignDamageComponent = true;
        public int fireFrame;
        public bool matchPointRotation = false;
    }
}

using System.Collections;
using UnityEngine;
using Mirror;

public class ImplosionOrb : HomingOrb
{
    public float damage = 5f;
    public float procMultiplier = 1f;
    public bool canCrit = false;
    public DamageType damageType;
    public GameObject effectPrefab;

    public GameObject explosionPrefab;

    public override void Initialize()
    {
        EffectData effectData = new EffectData
        {
            genericCharacterReference = target,
            genericFloat = travelTime,
            origin = startPosition,
        };

        EffectManager.CreateEffect(effectPrefab, effectData, true);

        if(target == null)
        {
            flaggedForRemoval = true;
        }
    }

    public override void OnArrival()
    {
        GameManager.instance.SpawnProjectile(new ProjectileSpawnInfo(explosionPrefab, Vector3.zero, Quaternion.identity, "ROOT", true, owner, target));
    }
    
}
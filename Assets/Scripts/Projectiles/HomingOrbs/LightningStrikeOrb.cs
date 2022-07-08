using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LightningStrikeOrb : HomingOrb
{
    private static GameObject prefab;
    public override void Initialize()
    {
        travelTime = 0.15f;

        EffectData effectData = new EffectData
        {
            genericCharacterReference = target,
            genericFloat = travelTime,
            origin = startPosition + Vector3.up * 10f,
        };

        EffectManager.CreateEffect(Resources.Load<GameObject>("Effects/HomingOrbEffects/LightningStrikeOrbEffect"), effectData, true);

        if (prefab == null) 
        {
            prefab = Resources.Load<GameObject>("Projectiles/Explosions/LightningStrikeExplosion");
        }
    }

    public override void OnArrival()
    {
        GameManager.instance.SpawnProjectile(new ProjectileSpawnInfo(prefab, target.GetComponent<CharacterMaster>().bodyCenter.position, Quaternion.identity, "", false, owner));
    }
}
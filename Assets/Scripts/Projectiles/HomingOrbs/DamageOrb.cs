using System.Collections;
using UnityEngine;
using Mirror;

public class DamageOrb : HomingOrb
{
    public float damage = 5f;
    public float procMultiplier = 1f;
    public bool canCrit = false;
    public DamageType damageType;
    public GameObject effectPrefab;

    public DamageOrb()
    {

    }

    public DamageOrb(Vector3 startPosition, NetworkIdentity target, GameObject effectPrefab, float travelTime, NetworkIdentity owner, float damage, DamageType damageType = DamageType.None, float procMultiplier = 1, bool canCrit = true)
    {
        this.startPosition = startPosition;
        this.target = target;
        this.effectPrefab = effectPrefab;
        this.travelTime = travelTime;
        this.owner = owner;
        this.damage = damage;
        this.damageType = damageType;
        this.procMultiplier = procMultiplier;
        this.canCrit = canCrit;
    }

    public override void Initialize()
    {
        EffectData effectData = new EffectData
        {
            genericCharacterReference = target,
            genericFloat = travelTime,
            origin = startPosition,
        };

        EffectManager.CreateEffect(effectPrefab, effectData, true);
    }

    public override void OnArrival()
    {
        DealDamage();
    }

    public void DealDamage()
    {
        DamageInfo damageInfo = new DamageInfo
        {
            attacker = owner,
            damage = damage,
            hitLocation = target.GetComponent<CharacterMaster>().bodyCenter.position,
            procMultiplier = procMultiplier,
            canCrit = canCrit,
            damageType = damageType,
        };
        DamageComponent.CalculateDamageValues(ref damageInfo, owner.GetComponent<CharacterMaster>());
        target.GetComponent<CharacterHealth>().AttemptDamage(damageInfo.WriteDamageInfo());
    }
    
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageComponentDelegate : MonoBehaviour
{
    public DamageComponent damageComponent;
    private HitboxGroup hitboxGroup;

    public void Awake()
    {
        hitboxGroup = GetComponent<HitboxGroup>();

        if (hitboxGroup != null)
        {
            // Add damage deal callback to hitbox group
            hitboxGroup.AddListener(damageComponent.DealGeneralDamage);
        }
    }
}

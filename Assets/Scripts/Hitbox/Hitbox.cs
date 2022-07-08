using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [System.NonSerialized]
    public List<HitboxGroup> groups = new List<HitboxGroup>();

    [System.NonSerialized]
    new public Collider collider;

    private void Awake()
    {
        collider = GetComponent<Collider>();
    }

    public void OnTriggerEnter(Collider other)
    {
        List<HitboxGroup> validGroups = new List<HitboxGroup>(groups);
        for (int i = 0; i < validGroups.Count; i++)
        {
            if (validGroups[i].listenerCount == 0)
            {
                validGroups.RemoveAt(i);
                i--;
            }
        }

        if (validGroups.Count == 0)
        {
            return;
        }

        bool hitTerrain = FracturedUtility.terrainMask == (FracturedUtility.terrainMask | (1 << other.gameObject.layer)) && !other.isTrigger;

        foreach (HitboxGroup group in groups)
        {
            if (hitTerrain)
            {
                group.TerrainCollisionEnter();
            }
        }

        Hitbox otherHitbox = other.GetComponent<Hitbox>();
        if (otherHitbox == null)
        {
            return;
        }

        foreach (HitboxGroup otherGroup in otherHitbox.groups)
        {
            if (otherGroup.associationOnly)
            {
                continue;
            }

            foreach (HitboxGroup group in groups)
            {
                group.GroupCollisionEnter(otherGroup, otherHitbox, this);
            }


            List<HitboxGroup> associations = otherGroup.GetRelevantAssociations(otherHitbox);
            foreach (HitboxGroup assocation in associations)
            {
                foreach (HitboxGroup group in groups)
                {
                    group.GroupCollisionEnter(assocation, otherHitbox, this);
                }
            }
        }
    }
}

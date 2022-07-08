using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class HitboxGroup : MonoBehaviour
{
    [SerializeField]
    List<Hitbox> hitboxes;

    [System.NonSerialized] private UnityEvent<HitboxGroup, Collider, Hitbox> onCollision = new UnityEvent<HitboxGroup, Collider, Hitbox>();
    [System.NonSerialized] private UnityEvent onTerrainCollision = new UnityEvent();

    public float disableAfterSeconds = 0;
    public float enableAfterSeconds = 0;

    [HideInInspector] public bool active = true;

    List<HitboxGroup> hitThisActivation = new List<HitboxGroup>();

    public bool hitSameTargetMultiplePerActivation = false;
    public bool associationOnly = false;

    [Header("When this hitbox is hit associated hitboxes that share the hit collider will also be hit.")]
    public List<HitboxGroup> associatedGroups = new List<HitboxGroup>();

    [System.NonSerialized] public int listenerCount = 0;

    public bool setEnabledOnChildren = true;

    public int hitboxLayer
    {
        set
        {
            foreach(Hitbox hitbox in hitboxes)
            {
                hitbox.gameObject.layer = value;
            }
        }
    }

    public void OnEnable()
    {
        hitThisActivation.Clear();

        if (setEnabledOnChildren)
        {
            EnableHitboxes();
        }
        else
        {
            FlipChildren();
        }
    }

    public void OnDisable()
    {
        if (setEnabledOnChildren)
        {
            DisableHitboxes();
        }
    }

    public void FlipChildren()
    {
        ToggleHitBoxes();
        ToggleHitBoxes();
    }

    public void AddListener(UnityAction<HitboxGroup, Collider, Hitbox> listener)
    {
        onCollision.AddListener(listener);
        listenerCount++;
    }

    public void AddTerrainListener(UnityAction listener)
    {
        onTerrainCollision.AddListener(listener);
        listenerCount++;
    }

    public void ToggleHitBoxes()
    {
        foreach (Hitbox hitbox in hitboxes)
        {
            Collider collider = hitbox.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = !collider.enabled;
            }
        }
    }

    public void DisableHitboxes()
    {
        foreach (Hitbox hitbox in hitboxes)
        {
            Collider collider = hitbox.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    public void EnableHitboxes()
    {
        foreach (Hitbox hitbox in hitboxes)
        {
            Collider collider = hitbox.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }
    }

    public void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        foreach(Hitbox hitbox in hitboxes)
        {
            hitbox.groups.Add(this);
            if (!enabled && setEnabledOnChildren)
            {
                hitbox.GetComponent<Collider>().enabled = false;
            }
        }

        if (disableAfterSeconds > 0.1f)
        {
            StartCoroutine(DisableAfter());
        }

        if (enableAfterSeconds > 0.1f)
        {
            active = false;
            StartCoroutine(EnableAfter());
        }
    }

    public void AddHitbox(Hitbox hitbox)
    {
        hitboxes.Add(hitbox);
        hitbox.groups.Add(this);
    }

    public void RemoveHitbox(Hitbox hitbox)
    {
        hitboxes.Remove(hitbox);
        hitbox.groups.Remove(this);
    }

    public void RemoveAllHitboxes(bool clearIndividuals = true)
    {
        if (clearIndividuals)
        {
            for (int i = 0; i < hitboxes.Count; i++)
            {
                hitboxes[i].groups.Remove(this);
            }
        }

        hitboxes.Clear();
    }

    public void GroupCollisionEnter(HitboxGroup other, Hitbox otherHitBox, Hitbox thisHitbox)
    {
        if (!enabled)
        {
            return;
        }

        if (!hitSameTargetMultiplePerActivation)
        {
            if (hitThisActivation.Contains(other))
            {
                return;
            }
        }
        if (active)
        {
            if (!hitSameTargetMultiplePerActivation)
            {
                hitThisActivation.Add(other);
            }
            onCollision.Invoke(other, otherHitBox.GetComponent<Collider>(), thisHitbox);
        }
    }

    public void TerrainCollisionEnter()
    {
        if (!enabled)
        {
            return;
        }

        onTerrainCollision.Invoke();
    }

    public IEnumerator DisableAfter()
    {
        yield return new WaitForSeconds(disableAfterSeconds);
        active = false;
    }

    public IEnumerator EnableAfter()
    {
        yield return new WaitForSeconds(enableAfterSeconds);
        active = true;
    }

    public List<HitboxGroup> GetRelevantAssociations(Hitbox hitHitbox)
    {
        List<HitboxGroup> associations = new List<HitboxGroup>();
        foreach(HitboxGroup association in associatedGroups)
        {
            if (association.hitboxes.Contains(hitHitbox))
            {
                associations.Add(association);
            }
        }
        return associations;
    }
}

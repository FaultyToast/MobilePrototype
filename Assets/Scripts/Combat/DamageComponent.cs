using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using UnityEngine.Serialization;

public class DamageComponent : MonoBehaviour
{
    [FormerlySerializedAs("damageInfo")]
    [HideInInspector] public old.DamageInfo oldDamageInfo;
    [SerializeField] public DamageInfo damageInfo;

    [Tooltip("If this is null an owner identity must be assigned at runtime")]

    public CharacterMaster ownerMaster;

    [FormerlySerializedAs("hitPrefabs")]
    public Effect[] oldHitPrefabs;

    public List<HitPrefabInfo> hitPrefabInfo;

    [System.Serializable]
    public class HitPrefabInfo
    {
        public Effect hitPrefab;

        [Tooltip("Referenced used for rotating effect correctly, used for sword swing hits")]
        public Transform referenceTransform;
    }

    public UnityEvent damageAttemptedCallback;

    public bool doFriendlyFire = false;
    private bool damageCalculated = false;

    [System.NonSerialized] public HitboxGroup hitboxGroup;

    // Use RollOnCreation for projectiles, use RollOnHit for character bound damage
    public enum DamageRollType
    {
        RollOnCreation,
        RollOnHit,
        NeverRoll,
    }

    public DamageRollType damageRollType = DamageRollType.RollOnHit;

    [Header("Only use if manually assigning master to child")]
    public ProjectileMaster projectileMaster;

    public void Awake()
    {
        hitboxGroup = GetComponent<HitboxGroup>();

        if (hitboxGroup != null)
        {
            // Add damage deal callback to hitbox group
            hitboxGroup.AddListener(DealGeneralDamage);
        }

    }

    public void Start()
    {
        if (projectileMaster != null)
        {
            SetOwner(projectileMaster.owner.GetComponent<CharacterMaster>());
        }
        if (damageRollType == DamageRollType.RollOnCreation && ownerMaster != null && !damageCalculated)
        {
            damageCalculated = true;
            CalculateDamageValues(ref damageInfo, ownerMaster);
        }
    }

    public void SetOwner(CharacterMaster owner)
    {
        ownerMaster = owner;
        if (damageRollType == DamageRollType.RollOnCreation && ownerMaster != null && !damageCalculated)
        {
            damageCalculated = true;
            CalculateDamageValues(ref damageInfo, ownerMaster);
        }
    }

    public void DealGeneralDamage(HitboxGroup other, Collider hitCollider, Hitbox thisHitbox)
    {
        DealDamage(other, hitCollider, thisHitbox, damageInfo);
    }

    public void DealDamage(HitboxGroup other, Collider hitCollider, Hitbox thisHitbox, DamageInfo damageInfoToDeal)
    {
        // Make sure there is health to damage
        CharacterHealth otherHealth = other.GetComponent<CharacterHealth>();
        if (otherHealth == null)
        {
            return;
        }

        // Get attacker and victim
        NetworkIdentity identity1 = null;
        NetworkIdentity identity2 = otherHealth.GetComponent<NetworkIdentity>();

        if (ownerMaster != null)
        {
            identity1 = ownerMaster.GetComponent<NetworkIdentity>();

            // Stop things from hitting themselves
            if (ReferenceEquals(identity1, identity2))
            {
                return;
            }

            // Make sure the right source is handling the damage
            if (!IsAuthorityValid(identity1, identity2))
            {
                return;
            }
        }
        else
        {
            if (!FracturedUtility.HasEffectiveAuthority(identity2))
            {
                return;
            }
        }



        DamageInfo newDamageInfo = damageInfoToDeal.Clone();

        newDamageInfo.attacker = identity1;
        newDamageInfo.doFriendlyFire = doFriendlyFire;

        // Set team index
        if (ownerMaster != null)
        {
            newDamageInfo.teamIndex = ownerMaster.teamIndex;
        }
        else newDamageInfo.teamIndex = -1;

        if (damageRollType == DamageRollType.RollOnHit)
        {
            CalculateDamageValues(ref newDamageInfo, ownerMaster);
        }

        // Ensure damage is valid between teams
        if (!otherHealth.FilterDamage(newDamageInfo))
        {
            return;
        }

        // Calculate hit location
        Vector3 location = hitCollider.ClosestPoint(thisHitbox.GetComponent<Collider>().bounds.center);
        newDamageInfo.hitLocation = location;

        otherHealth.AttemptDamage(newDamageInfo.WriteDamageInfo());
        damageAttemptedCallback.Invoke();

        if (ownerMaster != null && damageInfo.damage > 0)
        {
            ownerMaster.OnDamageDealtAttempted(newDamageInfo, otherHealth.characterMaster);
        }

        for (int i = 0; i < hitPrefabInfo.Count; i++)
        {
            Transform referenceTransform = hitPrefabInfo[i].referenceTransform;
            Quaternion rotation = Quaternion.identity;
            if (referenceTransform != null)
            {
                rotation = Quaternion.LookRotation((referenceTransform.position - newDamageInfo.hitLocation).normalized);
                rotation *= Quaternion.AngleAxis(90, Vector3.up);
            }
            EffectManager.CreateSimpleEffect(hitPrefabInfo[i].hitPrefab, newDamageInfo.hitLocation, rotation);
        }
    }

    public static void CalculateDamageValues(ref DamageInfo targetDamageInfo, CharacterMaster owner)
    {
        if (owner == null)
        {
            return;
        }

        // Set team index
        targetDamageInfo.teamIndex = owner.teamIndex;

        targetDamageInfo.damage *= targetDamageInfo.weaponModifier;
        targetDamageInfo.force = targetDamageInfo.damage * targetDamageInfo.forceMultiplier;
        targetDamageInfo.damage *= owner.damageModifier;

        switch (targetDamageInfo.damageType)
        {
            case DamageType.Melee:
                {
                    targetDamageInfo.damage *= owner.meleeDamageMultiplier;
                    break;
                }
            case DamageType.Magic:
                {
                    targetDamageInfo.damage *= owner.magicDamageMultiplier;
                    break;
                }
            case DamageType.Generic:
                {
                    targetDamageInfo.damage *= owner.genericDamageMultiplier;
                    break;
                }
        }

        if (!targetDamageInfo.isPassiveDamage && targetDamageInfo.canCrit && owner.RollCrit())
        {
            targetDamageInfo.isCrit = true;
            targetDamageInfo.colorFlags = targetDamageInfo.colorFlags | (DamageNumberColorFlags.IsCrit);
            targetDamageInfo.damage *= owner.criticalDamage;
        }
    }

    public bool IsAuthorityValid(NetworkIdentity identity1, NetworkIdentity identity2)
    {
        // If the sender is not valid authority goes to server
        if (identity1 == null)
        {
            return NetworkServer.active;
        }

        // Determine who handles the hit detection
        if (!DamageInvolvesLocalPlayer(identity1, identity2))
        {
            if (NetworkServer.active)
            {
                if (!ServerHandlesDamage(identity1, identity2))
                {
                    return false;
                }
            }
            else return false;
        }

        return true;
    }

    public bool ServerHandlesDamage(NetworkIdentity identity1, NetworkIdentity identity2)
    {
        return (identity1.connectionToClient == null && identity2.connectionToClient == null);
    }

    public bool DamageInvolvesLocalPlayer(NetworkIdentity identity1, NetworkIdentity identity2)
    {
        return NetworkClient.active && (identity1.hasAuthority || identity2.hasAuthority);
    }

    public void OnValidate()
    {
        if (oldDamageInfo.damage > 0)
        {
            damageInfo.damage = oldDamageInfo.damage;
            damageInfo.damageType = oldDamageInfo.damageType;
            oldDamageInfo.damage = 0;
        }

        if (oldHitPrefabs != null)
        {
            for (int i = 0; i < oldHitPrefabs.Length; i++)
            {
                hitPrefabInfo.Add(new HitPrefabInfo { hitPrefab = oldHitPrefabs[i], referenceTransform = null });
            }
            oldHitPrefabs = null;
        }

    }
}

public enum DamageType
{
    None,
    Generic,
    Melee,
    Magic
}

[System.Serializable]
public class BuffInflictInfo
{
    public BuffDef buff;
    public int stacks;
    public float timeOverride;
}

public static class BuffInflictInfoExtensions
{
    public static void WriteBuffInflictInfo(this NetworkWriter writer, BuffInflictInfo buffInflictInfo)
    {
        writer.WriteInt(buffInflictInfo.buff.assetID);
        writer.WriteInt(buffInflictInfo.stacks);
        writer.WriteFloat(buffInflictInfo.timeOverride);
    }

    public static BuffInflictInfo ReadBuffInflictInfo(this NetworkReader reader)
    {
        BuffInflictInfo buffInflictInfo = new BuffInflictInfo();
        buffInflictInfo.buff = BuffManager.buffs[reader.ReadInt()];
        buffInflictInfo.stacks = reader.ReadInt();
        buffInflictInfo.timeOverride = reader.ReadFloat();
        return buffInflictInfo;
    }
}

[System.Serializable]
public class DamageInfo
{
    public float damage;
    public DamageType damageType;
    [Header("For directional blocking, eg forgemaster should use owner center because he flails a lot")]
    public DamageOriginType damageOriginType;
    public List<BuffInflictInfo> buffsInflicted = new List<BuffInflictInfo>();

    public Vector3 damageOrigin
    {
        get
        {
            if (damageOriginType == DamageOriginType.OwnerCenter)
            {
                if (attacker != null)
                {
                    return attacker.GetComponent<CharacterMaster>().bodyCenter.position;
                }
                else
                {
                    return hitLocation;
                }
            }
            else
            {
                return hitLocation;
            }
        }
    }

    public enum DamageOriginType
    {
        HitLocation,
        OwnerCenter
    }

    public enum LaunchConditions
    {
        None,
        ForceThreshold,
        LaunchAlways
    }

    public enum LaunchType
    {
        Juggle,
        FixedLaunchUp,
        Slam
    }

    [System.NonSerialized] public int teamIndex;
    [System.NonSerialized] public Vector3 hitLocation;
    [System.NonSerialized] public DamageNumberColorFlags colorFlags;
    [System.NonSerialized] public NetworkIdentity attacker;
    [System.NonSerialized] public float force;
    [System.NonSerialized] public bool doFriendlyFire;
    [System.NonSerialized] public bool hurtAttacker;
    [System.NonSerialized] public bool isPassiveDamage;
    [System.NonSerialized] public bool canCrit = true;
    [System.NonSerialized] public bool isCrit;
    [System.NonSerialized] public float procMultiplier = 1f;
    [System.NonSerialized] public int attackReference = -1;
    [System.NonSerialized] public float weaponModifier = 1f;
    [System.NonSerialized] public float forceMultiplier = 1f;
    [System.NonSerialized] public LaunchConditions launchConditions;
    [System.NonSerialized] public float launchForceThreshold;
    [System.NonSerialized] public LaunchType launchType;
    [System.NonSerialized] public float weaponSpeed = 1f;
    [System.NonSerialized] public NetworkIdentity excludeDamagingCharacter;

    // Unsynced
    [System.NonSerialized] public bool processDamage = true;

    // Calculated in function
    [HideInInspector] public int executionerCount;

    public static DamageInfo ReadDamageInfo(byte[] data)
    {
        DamageInfo newDamageInfo = new DamageInfo();
        NetworkReader reader = new NetworkReader(data);
        newDamageInfo.damage = reader.ReadFloat();
        newDamageInfo.procMultiplier = reader.ReadFloat();
        newDamageInfo.weaponModifier = reader.ReadFloat();
        newDamageInfo.damageType = (DamageType)reader.ReadInt();
        newDamageInfo.buffsInflicted = new List<BuffInflictInfo>(reader.ReadArray<BuffInflictInfo>());
        newDamageInfo.teamIndex = reader.ReadInt();
        newDamageInfo.colorFlags = (DamageNumberColorFlags)reader.ReadInt();
        newDamageInfo.attackReference = reader.ReadInt();
        newDamageInfo.hitLocation = reader.ReadVector3();
        newDamageInfo.attacker = reader.ReadNetworkIdentity();
        newDamageInfo.force = reader.ReadFloat();
        newDamageInfo.doFriendlyFire = reader.ReadBool();
        newDamageInfo.hurtAttacker = reader.ReadBool();
        newDamageInfo.isPassiveDamage = reader.ReadBool();
        newDamageInfo.canCrit = reader.ReadBool();
        newDamageInfo.isCrit = reader.ReadBool();
        newDamageInfo.forceMultiplier = reader.ReadFloat();
        newDamageInfo.launchConditions = (LaunchConditions)reader.ReadInt();
        newDamageInfo.launchForceThreshold = reader.ReadFloat();
        newDamageInfo.launchType = (LaunchType)reader.ReadInt();
        newDamageInfo.damageOriginType = (DamageOriginType)reader.ReadInt();
        newDamageInfo.weaponSpeed = reader.ReadFloat();
        newDamageInfo.excludeDamagingCharacter = reader.ReadNetworkIdentity();

        return newDamageInfo;
    }

    public static DamageNumberColorFlags CreateColorFlag(DamageNumberColorFlags flag)
    {
        return CreateColorFlags(new DamageNumberColorFlags[] { flag });
    }

    public static DamageNumberColorFlags CreateColorFlags(DamageNumberColorFlags[] flags)
    {
        DamageNumberColorFlags mask = 0;
        for (int i = 0; i < flags.Length; i++)
        {
            mask |= flags[i];
        }
        return mask;
    }
}

public static class DamageInfoExtensions
{
    public static byte[] WriteDamageInfo(this DamageInfo damageInfo)
    {
        NetworkWriter writer = new NetworkWriter();
        writer.WriteFloat(damageInfo.damage);
        writer.WriteFloat(damageInfo.procMultiplier);
        writer.WriteFloat(damageInfo.weaponModifier);
        writer.WriteInt((int)damageInfo.damageType);
        writer.WriteArray<BuffInflictInfo>(damageInfo.buffsInflicted.ToArray());
        writer.WriteInt(damageInfo.teamIndex);
        writer.WriteInt((int)damageInfo.colorFlags);
        writer.WriteInt(damageInfo.attackReference);
        writer.WriteVector3(damageInfo.hitLocation);
        writer.WriteNetworkIdentity(damageInfo.attacker);
        writer.WriteFloat(damageInfo.force);
        writer.WriteBool(damageInfo.doFriendlyFire);
        writer.WriteBool(damageInfo.hurtAttacker);
        writer.WriteBool(damageInfo.isPassiveDamage);
        writer.WriteBool(damageInfo.canCrit);
        writer.WriteBool(damageInfo.isCrit);
        writer.WriteFloat(damageInfo.forceMultiplier);
        writer.WriteInt((int)damageInfo.launchConditions);
        writer.WriteFloat(damageInfo.launchForceThreshold);
        writer.WriteInt((int)damageInfo.launchType);
        writer.WriteInt((int)(damageInfo.damageOriginType));
        writer.WriteFloat(damageInfo.weaponSpeed);
        writer.WriteNetworkIdentity(damageInfo.excludeDamagingCharacter);

        return writer.ToArray();
    }

    public static DamageInfo Clone(this DamageInfo damageInfo)
    {
        return DamageInfo.ReadDamageInfo(damageInfo.WriteDamageInfo());
    }
}

namespace old
{
    [System.Serializable]
    public struct DamageInfo
    {
        public float damage;
        public DamageType damageType;

        [HideInInspector] public int teamIndex;
        [HideInInspector] public Vector3 hitLocation;
        [HideInInspector] public int executionerCount;
        [HideInInspector] public int colorFlags;
        [HideInInspector] public NetworkIdentity attacker;
        [HideInInspector] public float force;
        [HideInInspector] public bool doFriendlyFire;
        [HideInInspector] public bool hurtAttacker;
        [HideInInspector] public bool isPassiveDamage;
        [HideInInspector] public bool canCrit;
    }
}


public enum DamageNumberColorFlags
{
    None = 0,
    IsCrit = (1 << 0),
    EdenBoundActivated = (1 << 1),
    Burning = (1 << 2),
    Poison = (1 << 3)
}
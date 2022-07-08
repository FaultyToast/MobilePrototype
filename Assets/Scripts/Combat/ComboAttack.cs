using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Mirror;

[CreateAssetMenu(fileName = "New Combo Attack", menuName = "Combat/ComboAttack", order = 1)]
public class ComboAttack : ScriptableObject
{
    public enum MoveTowardsMode
    {
        TargetDirection,
        ModelRotation,
        SoftTargetDirection
    };

    [Header("Animation")]
    public string clipName;

    [Header("Timing")]
    public int minAttackFrames;
    public int maxComboFrames;
    public int exitFrame;
    public int unlockPriorityFrame;

    [Header("Root Motion")]
    public AnimationCurve forwardSpeedAnimationCurve;
    public AnimationCurve upSpeedAnimationCurve;
    public float moveForwardValue = 3f;
    public float moveUpValue = 3f;
    public MoveTowardsMode moveTowards;

    [Header("Tracking")]
    public float rotationSpeedMultiplier = 1f;
    public float maxTrackingAngle = 360f;
    public AnimationCurve trackingCurve;
    public bool trackConstantly = false;

    [Header("Damage")]
    public string damageComponentName;
    public float damage;
    public DamageInfo.LaunchConditions launchConditions = DamageInfo.LaunchConditions.None;
    public float launchForceThreshold = 0f;
    public DamageInfo.LaunchType launchType = DamageInfo.LaunchType.Juggle;

    [Header("Other")]
    public bool enableGravity = true;
    public int enableGravityFrame = -1;
    public bool aerialHoming = false;
    public bool resetCombo = false;
    public bool isFinisher = false;
    public bool haltOnClosedDistance = false;
    public bool scaleWithWeaponSpeed = true;
    public bool hitStopAffectsMovement = true;
    public bool isDodgeAttack = false;
    public bool triggerAerialCooldown = false;

    public List<ComboProjectileSpawnInfo> projectiles;

    [System.Serializable]
    public class ComboProjectileSpawnInfo
    {
        public GameObject prefabToSpawn;
        public string spawnPointChildName;
        public bool assignDamageComponent = true;
        public int fireFrame;
        public bool matchPointRotation = false;
        public bool ChildToParentObject;

    }
}

public static class ComboAttackExtensions
{
    public static void WriteComboAttack(this NetworkWriter writer, ComboAttack comboAttack)
    {
        writer.WriteString(comboAttack.clipName);
        writer.WriteString(comboAttack.damageComponentName);
        writer.WriteBool(comboAttack.resetCombo);
        writer.WriteBool(comboAttack.isFinisher);
        writer.WriteBool(comboAttack.haltOnClosedDistance);
        writer.WriteBool(comboAttack.scaleWithWeaponSpeed);
        writer.WriteBool(comboAttack.hitStopAffectsMovement);
        writer.WriteBool(comboAttack.isDodgeAttack);
        writer.WriteFloat(comboAttack.damage);
        writer.WriteInt((int)comboAttack.launchConditions);
        writer.WriteFloat(comboAttack.launchForceThreshold);
        writer.WriteInt((int)comboAttack.launchType);

    }

    public static ComboAttack ReadComboAttack(this NetworkReader reader)
    {
        ComboAttack comboAttack = ScriptableObject.CreateInstance<ComboAttack>();
        comboAttack.clipName = reader.ReadString();
        comboAttack.damageComponentName = reader.ReadString();
        comboAttack.resetCombo = reader.ReadBool();
        comboAttack.isFinisher = reader.ReadBool();
        comboAttack.haltOnClosedDistance = reader.ReadBool();
        comboAttack.scaleWithWeaponSpeed = reader.ReadBool();
        comboAttack.hitStopAffectsMovement = reader.ReadBool();
        comboAttack.isDodgeAttack = reader.ReadBool();
        comboAttack.damage = reader.ReadFloat();
        comboAttack.launchConditions = (DamageInfo.LaunchConditions)reader.ReadInt();
        comboAttack.launchForceThreshold = reader.ReadFloat();
        comboAttack.launchType = (DamageInfo.LaunchType)reader.ReadInt();
        return comboAttack;
    }
}

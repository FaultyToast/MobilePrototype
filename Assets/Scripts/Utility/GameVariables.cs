using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GameVariables", menuName = "FracturedAssets/GameVariables", order = 1)]
[System.Serializable]
public class GameVariables : ScriptableObject
{
    public static GameVariables instance;

    [Header("Items")]
    public float edenBoundDamageMultiplier = 2f;
    public float edenBoundStackingDamageMultiplier = 0.5f;

    [Header("Hitstop")]
    public float hitStopRecoveryTime;
    public float hitStopTime;
    public float hitStopMinMultiplier;
    public float hitStopBaseForceScaling;
    public float hitStopAdditionalForceMultiplier;

    [Header("Damage")]
    public float damageCap = 999999999f;

    [Header("Gameloop")]
    public int maxFractureLevel = 5;
    public int roomsToBossRoom = 5;
    public float survivalRoomTime = 30f;

    [Header("Weapon Generation")]
    public float damageModifierPerLevel = 0.1f;
    public float criticalModifierPerLevel = 0.1f;
    public float speedModifierPerLevel = 0.1f;
    public float maxWeaponSpeed = 1.2f;
    public float apModifierPerLevel = 0.1f;
    public int globalLevelPerWeaponLevel = 3;
    public GameObject weaponPickUpPrefab;

    [Header("AP")]
    public float baseAPOnHit = 10f;

    [Header("Scaling")]
    [FormerlySerializedAs("baseEnemyScalar")]
    public float baseEnemyDamageScalar = 0.05f;

    [FormerlySerializedAs("enemyScalingExponent")]
    public float enemyDamageScalingExponent = 1.5f;

    public float baseEnemyHealthScalar = 0.2f;
    public float enemyHealthScalingExponent = 1.5f;

    public float baseExpScalar = 0.1f;
    public float expScalingExponent = 1.25f;

    public float baseHealthScalar = 1.5f;

    [Header("Combat")]
    public ActionState aerialAttackCooldownState;
    public ActionState launchState;
    public ActionState slamState;
}

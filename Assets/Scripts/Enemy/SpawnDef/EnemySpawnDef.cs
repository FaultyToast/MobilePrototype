using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnDef : EnemySpawnConditions
{
    public GameObject prefab;
    public float cost;

    public bool spawnInAir = false;

    public int _minGlobalLevel;
    public int minGlobalLevel { get { return _minGlobalLevel; } set { _minGlobalLevel = value; } }

    public float _spawnWeight = 1f;
    public float spawnWeight { get { return _spawnWeight; } set { _spawnWeight = value; } }

    [System.NonSerialized] public float enemyHeight;
    [System.NonSerialized] public float enemyRadius;
}

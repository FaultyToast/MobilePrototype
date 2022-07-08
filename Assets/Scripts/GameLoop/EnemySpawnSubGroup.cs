using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemySpawnSubGroup", menuName = "FracturedAssets/EnemySpawning/EnemySpawnSubGroup", order = 1)]
public class EnemySpawnSubGroup : ScriptableObject, EnemySpawnConditions
{
    public List<EnemySpawnDef> enemySpawnDefs;

    public int _minGlobalLevel;
    public int minGlobalLevel { get { return _minGlobalLevel; } set { _minGlobalLevel = value; } }

    public float _spawnWeight = 1f;
    public float spawnWeight { get { return _spawnWeight; } set { _spawnWeight = value; } }
}

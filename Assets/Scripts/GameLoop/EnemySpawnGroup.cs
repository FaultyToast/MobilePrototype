using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemySpawnGroup", menuName = "FracturedAssets/EnemySpawning/EnemySpawnGroup", order = 1)]
public class EnemySpawnGroup : ScriptableObject
{
    public List<EnemySpawnSubGroup> subGroups;
}

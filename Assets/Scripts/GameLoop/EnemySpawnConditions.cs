using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface EnemySpawnConditions
{
    float spawnWeight { get; set; }
    int minGlobalLevel { get; set; }
}

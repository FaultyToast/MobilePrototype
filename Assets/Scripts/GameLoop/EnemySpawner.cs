using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Mirror;
using UnityEngine.InputSystem;

public class EnemySpawner : MonoBehaviour
{
    public EnemySpawnGroup enemySpawnGroup;

    private List<float> enemyHeights = new List<float>();
    private List<float> enemyRadii = new List<float>();

    private BoxCollider spawnZone;

    [System.NonSerialized] public UnityEvent onWavesCompleted = new UnityEvent();
    [System.NonSerialized] public UnityEvent onBossDefeated = new UnityEvent();

    private Transform noSpawnCenter;
    private float noSpawnDistance = 15;
    private bool enemyCountDirty = false;

    private int failCount = 0;
    private int enemyCount = 0;
    private int waveCount = 1;
    private int currentWave = 0;
    private int maxEnemies = 20;

    private float spawnBudget = 0;
    [System.NonSerialized] public bool wavesOngoing = false;

    public GameObject debugEnemy;

    public class EnemySpawnInfo
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
        public float expOnDeath = 0f;
        public bool childToRoom = false;
        public float delay = 0f;
    }

    [System.NonSerialized] public List<EnemySpawnInfo> enemiesToSpawn = new List<EnemySpawnInfo>();

    private static EnemySpawner _instance;
    public static EnemySpawner instance
    {
        get
        {
            return _instance;
        }
    }

    void Awake()
    {
        _instance = this;
    }

    public void Start()
    {
        for (int i = 0; i < enemySpawnGroup.subGroups.Count; i++)
        {
            for (int j = 0; j < enemySpawnGroup.subGroups[i].enemySpawnDefs.Count; j++)
            {
                float height = 0;
                float radius = 0;
                EnemySpawnDef spawnDef = enemySpawnGroup.subGroups[i].enemySpawnDefs[j];
                CapsuleCollider collider = spawnDef.prefab.GetComponent<CapsuleCollider>();
                if (collider != null)
                {
                    height = collider.height;
                    radius = collider.radius;
                }
                spawnDef.enemyHeight = height;
                spawnDef.enemyRadius = radius;
            }
        }
    }

    public void SpawnBoss(Transform bossSpawnPoint, GameObject bossPrefab)
    {
        GameObject boss = SpawnEnemy(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation, 2500);
        boss.GetComponent<CharacterHealth>().onDeath.AddListener(BossDefeated);
    }

    public void BossDefeated()
    {
        onBossDefeated.Invoke();
    }

    public void WavesCompleted()
    {
        wavesOngoing = false;
        onWavesCompleted.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateQueuedEnemies();

#if UNITY_EDITOR
        // DEBUG
        if (Keyboard.current[Key.Numpad6].wasPressedThisFrame)
        {
            DebugSpawnZones();
        }

        if (Keyboard.current[Key.M].wasPressedThisFrame)
        {
            GameManager.instance.CmdSpawnDebugEnemy();
        }
#endif
    }

    public void DebugSpawnEnemy()
    {
        SpawnEnemy(debugEnemy, FracturedNetworkManager.singleton.GetStartPosition().position, Quaternion.identity, 300f);
    }

    public void QueueEnemyForSpawn(EnemySpawnInfo enemySpawnInfo)
    {
        if (!NetworkServer.active)
        {
            return;
        }
        enemiesToSpawn.Add(enemySpawnInfo);
    }

    public void UpdateQueuedEnemies()
    {
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            EnemySpawnInfo enemy = enemiesToSpawn[i];
            enemy.delay -= Time.deltaTime;
            if (enemy.delay <= 0)
            {
                if (GameManager.instance.isInCombat)
                {
                    SpawnEnemy(enemy.prefab, enemy.position, enemy.rotation, enemy.expOnDeath, enemy.childToRoom);
                }
                enemiesToSpawn.RemoveAt(i);
                i--;
            }
        }
    }

    public void ClearQueuedEnemies()
    {
        enemiesToSpawn.Clear();
    }

    public void DebugSpawnZones()
    {
        int failCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            RandomNavMeshPositionResult result = new RandomNavMeshPositionResult();
            while (!result.validPosition)
            {
                result = GetPositionOnNavMesh(enemySpawnGroup.subGroups[0].enemySpawnDefs[0]);
                failCount++;
                if (failCount > 1000)
                {
                    Debug.Log("Failed too many times");
                    break;
                }
            }

            Vector3 point = GetSpawnPointFromNavmeshPoint(result.point, enemySpawnGroup.subGroups[0].enemySpawnDefs[0]);

            Debug.DrawRay(point, Vector3.up * 2f, Color.green, 99f);
        }

    }

    public void StartWaves(float budget)
    {
        enemyCount = 0;
        currentWave = 0;
        wavesOngoing = true;
        SpawnWave(budget);
    }

    public void SpawnWave(float budget)
    {
        failCount = 0;
        float oldTime = Time.realtimeSinceStartup;

        spawnBudget = budget;
        spawnBudget *= 1 + (GameManager.instance.globalLevel - 1) * 0.15f;

        List<EnemySpawnSubGroup> enemyPool = SelectEnemyPool();

        EnemySpawnDef chosenSpawnDef = null;
        do
        {
            if (enemyCount > maxEnemies)
            {
                break;
            }
            chosenSpawnDef = GetEnemyDef(enemyPool);
            if (chosenSpawnDef != null)
            {
                SpawnEnemyOnNavmesh(chosenSpawnDef);
            }
        }
        while (chosenSpawnDef != null);

        float newTime = Time.realtimeSinceStartup;

        //Debug.Log("Time per spawn: " + ((newTime - oldTime) * 1000f / enemyCount).ToString() + "ms");
        //Debug.Log("Fail count: " + failCount);

        currentWave++;
    }

    public void LateUpdate()
    {
        if (enemyCountDirty)
        {
            enemyCountDirty = false;
            UpdateEnemyCount();
        }
    }

    public void UpdateEnemyCount()
    {
        if (wavesOngoing)
        {
            GameManager.instance.RpcSetSubObjective(enemyCount + " enemies left");
        }
    }

    public Dictionary<string, int> weightingTest = new Dictionary<string, int>();
    public List<EnemySpawnSubGroup> SelectEnemyPool()
    {
        List<EnemySpawnSubGroup> chosenSpawnDefs = new List<EnemySpawnSubGroup>();
        List<EnemySpawnConditions> subGroupsConditions = new List<EnemySpawnConditions>(enemySpawnGroup.subGroups);

        List<EnemySpawnConditions> chosenSubGroupsConditions = SelectEnemySpawnConditions(subGroupsConditions, 7);

        for (int i = 0; i < chosenSubGroupsConditions.Count; i++)
        {
            chosenSpawnDefs.Add(chosenSubGroupsConditions[i] as EnemySpawnSubGroup);
        }

        return chosenSpawnDefs;
    }

    public List<EnemySpawnConditions> SelectEnemySpawnConditions(List<EnemySpawnConditions> potential, int count)
    {
        List<EnemySpawnConditions> chosenConditions = new List<EnemySpawnConditions>();
        List<EnemySpawnConditions> validSubGroups = new List<EnemySpawnConditions>(potential);
        for (int i = 0; i < validSubGroups.Count; i++)
        {
            if (GameManager.instance.globalLevel < validSubGroups[i].minGlobalLevel)
            {
                validSubGroups.RemoveAt(i);
                i--;
            }
        }

        List<float> weightings = new List<float>();
        for (int i = 0; i < validSubGroups.Count; i++)
        {
            weightings.Add(validSubGroups[i].spawnWeight);
        }

        for (int i = 0; i < count; i++)
        {
            int chosen = FracturedUtility.RandomWeighted(weightings);
            chosenConditions.Add(validSubGroups[chosen]);
        }

        return chosenConditions;
    }

    public EnemySpawnDef GetEnemyDef(List<EnemySpawnSubGroup> enemySubGroupPool)
    {
        if (enemySubGroupPool.Count == 0)
        {
            return null;
        }

        while (enemySubGroupPool.Count > 0)
        {
            int randomIndex = Random.Range(0, enemySubGroupPool.Count);
            EnemySpawnDef chosenSpawnDef = ChooseFromSubGroup(enemySubGroupPool[randomIndex]);

            float cost = chosenSpawnDef.cost;
            if (cost <= spawnBudget)
            {
                spawnBudget -= cost;
                return chosenSpawnDef;
            }


            enemySubGroupPool.RemoveAt(randomIndex);
        }

        return null;
    }

    public EnemySpawnDef ChooseFromSubGroup(EnemySpawnSubGroup enemySpawnSubGroup)
    {
        List<EnemySpawnConditions> spawnDefsConditions = new List<EnemySpawnConditions>(enemySpawnSubGroup.enemySpawnDefs);
        List<EnemySpawnConditions> chosenEnemy = SelectEnemySpawnConditions(spawnDefsConditions, 1);
        return (chosenEnemy[0] as EnemySpawnDef);
    }

    private void SpawnEnemyOnNavmesh(EnemySpawnDef spawnDef)
    {
        if (enemyCount >= 25)
        {
            return;
        }

        RandomNavMeshPositionResult result = new RandomNavMeshPositionResult();
        while (!result.validPosition)
        {
            result = GetPositionOnNavMesh(spawnDef);
            failCount++;
            if (failCount > 1000)
            {
                Debug.Log("Failed too many times");
                break;
            }
        }

        Vector3 spawnPoint = GetSpawnPointFromNavmeshPoint(result.point, spawnDef);

        GameObject enemy = SpawnEnemy(spawnDef.prefab, spawnPoint, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)), spawnDef.cost);
    }

    public Vector3 GetSpawnPointFromNavmeshPoint(Vector3 navmeshPoint, EnemySpawnDef spawnDef)
    {
        Vector3 spawnPoint = navmeshPoint + Vector3.up * (spawnDef.enemyHeight * 0.5f - 0.5f);

        if (spawnDef.spawnInAir)
        {
            float upCheck = 5f;
            Vector3 point1 = navmeshPoint + Vector3.up * 0.1f + Vector3.up * spawnDef.enemyRadius;
            Vector3 point2 = navmeshPoint + Vector3.up * 0.1f + Vector3.up * (spawnDef.enemyHeight - spawnDef.enemyRadius);
            RaycastHit hit;
            if (Physics.CapsuleCast(point1, point2, spawnDef.enemyRadius, Vector3.up, out hit, upCheck + spawnDef.enemyHeight, FracturedUtility.terrainMask))
            {
                float yPosition = Mathf.Max(hit.point.y - spawnDef.enemyHeight * 0.5f, spawnPoint.y);
                spawnPoint.y = yPosition;
            }
            else
            {
                spawnPoint.y += upCheck;
            }
        }

        return spawnPoint;
    }

    public GameObject SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation, float expOnDeath = 0f, bool childToRoom = false)
    {
        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
        CharacterMaster enemyMaster = enemy.GetComponent<CharacterMaster>();
        enemyMaster.characterMovement.motor.SetPosition(position);
        enemyMaster.modelPivot.rotation = rotation;
        enemyMaster.expOnDeath = expOnDeath;
        enemyMaster.weaponDropChance = expOnDeath / 500f;
        enemyMaster.killOnRoomExit = childToRoom;

        if (wavesOngoing)
        {
            var enemyHealth = enemyMaster.characterHealth;
            enemyHealth.onDeath.AddListener(EnemyKilled);
        }

        NetworkServer.Spawn(enemy);

        if (wavesOngoing)
        {
            enemyCount++;
            enemyCountDirty = true;
        }
        return enemy;
    }

    public struct RandomNavMeshPositionResult
    {
        public bool validPosition;
        public Vector3 point;
    }

    private RandomNavMeshPositionResult GetPositionOnNavMesh(EnemySpawnDef enemySpawnDef)
    {
        RandomNavMeshPositionResult result = new RandomNavMeshPositionResult();

        Vector3 randomPos = GetPointInSpawnZone();

        NavMeshHit myNavHit;
        if (NavMesh.SamplePosition(randomPos, out myNavHit, 100, -1))
        {
            if (NavMesh.SamplePosition(myNavHit.position, out myNavHit, 100, -1))
            {
                Vector3 point0 = myNavHit.position + new Vector3(0f, enemySpawnDef.enemyRadius, 0f);
                Vector3 point1 = myNavHit.position + new Vector3(0f, enemySpawnDef.enemyHeight - enemySpawnDef.enemyRadius, 0f);

                if (Physics.OverlapCapsule(point0, point1, enemySpawnDef.enemyRadius, ~0, QueryTriggerInteraction.Ignore).Length == 0)
                {
                    result.validPosition = true;
                    result.point = myNavHit.position;
                    //Debug.DrawLine(point1, point0, Color.red, 10f);
                    //Debug.DrawLine(myNavHit.position, point0, Color.green, 10f);
                    //Debug.DrawLine(point1, point1 + new Vector3(0f, capsuleRadius, 0f), Color.green, 10f);
                    return result;
                }
            }
        }
        result.validPosition = false;
        return result;
    }

    public Vector3 GetPointInSpawnZone()
    {
        float x = Random.Range(spawnZone.bounds.max.x, spawnZone.bounds.min.x);
        float y = Random.Range(spawnZone.bounds.max.y, spawnZone.bounds.min.y);
        float z = Random.Range(spawnZone.bounds.max.z, spawnZone.bounds.min.z);
        return new Vector3(x, y, z);
    }

    public void EnemyKilled()
    {
        enemyCount--;

        if (!wavesOngoing || enemiesToSpawn.Count > 0)
        {
            return;
        }
        enemyCountDirty = true;
        if (enemyCount <= 0)
        {
            if (currentWave < waveCount)
            {
                SpawnWave(175f);
            }
            else
            {
                Debug.Log("Waves completed");
                WavesCompleted();
            }
        }
    }
}

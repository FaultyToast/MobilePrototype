using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public static class FracturedAssets
{
    public static Effect[] effects;
    public static ProjectileMaster[] projectiles;
    public static ActionState[] actionStateInstances;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        effects = ContentLoader.LoadContentWithID<Effect>("Effects", typeof(Effects), true);
        projectiles = ContentLoader.LoadContentWithID<ProjectileMaster>("Projectiles", typeof(Projectiles), true);
        actionStateInstances = ContentLoader.LoadContentWithID<ActionState>("ActionStateInstances");
        //BuffManager.Initialize();

        LoadAllPrefabsToRegister();
    }

    public static void LoadAllPrefabsToRegister()
    {
        FracturedNetworkManager.prefabsToRegister = new List<GameObject>();

        GameObject[] enemies = ContentLoader.LoadContent<GameObject>("Enemies");
        GameObject[] rooms = ContentLoader.LoadContent<GameObject>("RoomGen/Rooms");
        GameObject[] networkedPrefabs = ContentLoader.LoadContent<GameObject>("NetworkedPrefabs");

        AddPrefabsToRegisterList(projectiles);
        AddPrefabsToRegisterList(enemies);
        AddPrefabsToRegisterList(rooms);
        AddPrefabsToRegisterList(networkedPrefabs);
    }

    public static void AddPrefabsToRegisterList(MonoBehaviour[] prefabs)
    {
        for (int i = 0; i < prefabs.Length; i++)
        {
            FracturedNetworkManager.prefabsToRegister.Add(prefabs[i].gameObject);
        }
    }

    public static void AddPrefabsToRegisterList(GameObject[] prefabs)
    {
        for (int i = 0; i < prefabs.Length; i++)
        {
            FracturedNetworkManager.prefabsToRegister.Add(prefabs[i]);
        }
    }
}

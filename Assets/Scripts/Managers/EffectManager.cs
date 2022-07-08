using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;

public static class EffectManager
{
    public struct EffectMessage : NetworkMessage
    {
        public int effectID;
        public uint originID;

        public byte[] data;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        FracturedNetworkManager.onStartServer.AddListener(ServerStart);
        FracturedNetworkManager.onStartClient.AddListener(ClientStart);
    }

    public static void ServerStart()
    {
        NetworkServer.RegisterHandler<EffectMessage>(ReceiveEffectServer);
    }

    public static void ClientStart()
    {
        NetworkClient.RegisterHandler<EffectMessage>(ReceiveEffectClient);
    }

    public static GameObject CreateSimpleEffect(GameObject effectPrefab, Vector3 position, NetworkIdentity parent = null, bool sync = true)
    {
        return CreateSimpleEffect(effectPrefab.GetComponent<Effect>(), position, Quaternion.identity, parent, sync);
    }

    public static GameObject CreateSimpleEffect(Effect effectPrefab, Vector3 position, NetworkIdentity parent = null, bool sync = true)
    {
        return CreateSimpleEffect(effectPrefab, position, Quaternion.identity, parent, sync);
    }

    public static GameObject CreateSimpleEffect(Effect effectPrefab, Vector3 position, Quaternion rotation, NetworkIdentity parent = null, bool sync = true)
    {
        EffectData effectData = new EffectData { origin = position, parentReference = parent, rotation = rotation };

        return CreateEffect(GetEffectIDFromPrefab(effectPrefab), effectData, sync);
    }

    [Obsolete("Effects now spawn using the EffectData class to provide extra info. Alternatively use CreateSimpleEffect to avoid making EffectData.")]
    public static GameObject CreateEffect(Effect effectPrefab, Vector3 position, NetworkIdentity parent = null, bool sync = true)
    {
        return CreateSimpleEffect(effectPrefab, position, Quaternion.identity, parent, sync);
    }

    public static GameObject CreateEffect(GameObject effectPrefab, EffectData effectData, bool sync = true)
    {
        return CreateEffect(effectPrefab.GetComponent<Effect>(), effectData, sync);
    }

    public static GameObject CreateEffect(Effect effectPrefab, EffectData effectData, bool sync = true)
    {
        return CreateEffect(effectPrefab.assetID, effectData, sync);
    }

    public static GameObject CreateEffect(int effectID, EffectData effectData, bool sync = true)
    {
        GameObject spawnedEffect = null;
        if (NetworkClient.active)
        {
            spawnedEffect = GameObject.Instantiate(GetEffectPrefabFromID(effectID), effectData.origin, effectData.rotation);
            spawnedEffect.GetComponent<Effect>().effectData = effectData;
            if (effectData.parentReference != null)
            {
                spawnedEffect.transform.SetParent(effectData.parentReference.transform, true);
            }
        }

        if (sync)
        {
            uint connectionID = 0;
            if (NetworkClient.active)
            {
                connectionID = NetworkClient.connection.identity.netId;
            }
            SyncEffect(effectID, effectData, connectionID);
        }

        return spawnedEffect;
    }

    public static void ReceiveEffectClient(EffectMessage message)
    {
        EffectData effectData = EffectData.ReadEffectData(message.data);
        CreateEffect(message.effectID, effectData, false);
    }

    public static void ReceiveEffectServer(NetworkConnection connection, EffectMessage message)
    {
        EffectData effectData = EffectData.ReadEffectData(message.data);
        if (NetworkClient.active)
        {
            CreateEffect(message.effectID, effectData, false);
        }

        SyncEffect(message.effectID, effectData, message.originID);
    }

    public static void SyncEffect(int effectID, EffectData effectData, uint originID = 0)
    {

        EffectMessage message = new EffectMessage()
        {
            effectID = effectID,
            originID = originID,
            data = effectData.WriteEffectData()
        };


        if (!NetworkServer.active)
        {
            NetworkClient.Send(message);
        }
        else
        {
            foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
            {
                if (connection.Value.identity.netId != originID && !(NetworkClient.active && connection.Value.identity.netId == NetworkClient.connection.identity.netId))
                {
                    connection.Value.Send(message);
                }
            }
        }
    }

    public static int GetEffectIDFromPrefab(Effect prefab)
    {
        return prefab.assetID;
    }

    public static GameObject GetEffectPrefabFromID(int ID)
    {
        return FracturedAssets.effects[ID].gameObject;
    }
}

public static class Effects
{
    public static Effect RetaliationOrbEffect;
    public static Effect GenericDamageOrbEffect;
    public static Effect ReflectSparksOrbEffect;
    public static Effect ProjectileOnCastOrbEffect;
    public static Effect ChainLightningOrbEffect;
}

public class EffectData
{
    /// <summary>
    /// Effect will be parented to this NetworkIdentity on spawn
    /// </summary>
    public NetworkIdentity parentReference;

    /// <summary>
    /// Not involved in the effect spawning but can be referenced by scripts on the effect
    /// </summary>
    public NetworkIdentity genericCharacterReference;

    /// <summary>
    /// Effect spawn point
    /// </summary>
    public Vector3 origin;

    /// <summary>
    /// Effect rotation
    /// </summary>
    public Quaternion rotation = Quaternion.identity;

    /// <summary>
    /// Not involved in the effect spawning but can be referenced by scripts on the effect
    /// </summary>
    public float genericFloat;

    /// <summary>
    /// Not involved in the effect spawning but can be referenced by scripts on the effect
    /// </summary>
    public bool genericBool;


    public static EffectData ReadEffectData(byte[] data)
    {
        EffectData effectData = new EffectData();
        NetworkReader reader = new NetworkReader(data);
        effectData.parentReference = reader.ReadNetworkIdentity();
        effectData.genericCharacterReference = reader.ReadNetworkIdentity();
        effectData.origin = reader.ReadVector3();
        effectData.genericFloat = reader.ReadFloat();
        effectData.genericBool = reader.ReadBool();
        effectData.rotation = reader.ReadQuaternion();

        return effectData;
    }

}


public static class EffectDataExtensions
{
    public static byte[] WriteEffectData(this EffectData effectData)
    {
        NetworkWriter writer = new NetworkWriter();
        writer.WriteNetworkIdentity(effectData.parentReference);
        writer.WriteNetworkIdentity(effectData.genericCharacterReference);
        writer.WriteVector3(effectData.origin);
        writer.WriteFloat(effectData.genericFloat);
        writer.WriteBool(effectData.genericBool);
        writer.WriteQuaternion(effectData.rotation);

        return writer.ToArray();
    }
}
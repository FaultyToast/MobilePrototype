using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

public struct ProjectileSpawnInfo
{
    // Owner should only be null when getting spawned through action state machine (will automatically set owner to self)
    public ProjectileSpawnInfo(GameObject projectilePrefab, Vector3 spawnPosition, Quaternion spawnRotation, string parentTransformLocator = "", bool isLocal = false, NetworkIdentity ownerOverride = null, NetworkIdentity parentOverride = null)
    {
        this.projectileID = projectilePrefab.GetComponent<ProjectileMaster>().assetID;
        this.spawnPosition = spawnPosition;
        this.spawnRotation = spawnRotation;
        this.owner = ownerOverride;
        this.parentTransformLocator = parentTransformLocator;
        this.isLocal = isLocal;
        this.parentOverride = parentOverride;
        this.genericFloat = 0f;
    }

    ///<summary> ID of projectile being spawned </summary> 
    public int projectileID;
    ///<summary> Position of projectile </summary> 
    public Vector3 spawnPosition;
    ///<summary> Rotation of projectile </summary> 
    public Quaternion spawnRotation;
    ///<summary> Spawn projectile in local space or world space </summary> 
    public bool isLocal;
    ///<summary> Projectile's owner/creator primarily for damage checks (necessary) </summary> 
    public NetworkIdentity owner;
    /// <summary> 
    /// Sets the transform parent to this object instead of the owner
    /// </summary>
    public NetworkIdentity parentOverride;
    ///<summary> Sets parent of the projectile using child locator string, "" for no parent, "ROOT" for owner root (no ChildLocator needed) </summary> 
    ///
    public string parentTransformLocator;

    /// <summary> 
    /// Can be used to pass values to all kinds of projectiles
    /// </summary>
    public float genericFloat;
}

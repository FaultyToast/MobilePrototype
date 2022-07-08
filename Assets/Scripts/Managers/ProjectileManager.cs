using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProjectileManager
{
    public static ProjectileMaster GetProjectileFromID(int id)
    {
        return FracturedAssets.projectiles[id];
    }

    public static int GetIDFromProjectile(GameObject projectile)
    {
        return projectile.GetComponent<ProjectileMaster>().assetID;
    }
}

public static class Projectiles
{
    public static ProjectileMaster DeathExplosion;
}
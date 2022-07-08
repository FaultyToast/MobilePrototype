using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

public static class FracturedUtility
{
    public static LayerMask terrainMask
    {
        get
        {
            return LayerMask.GetMask("Default", "Stairs", "Pillar", "Terrain", "NonWalkable", "Ground");
        }
    }
    public static bool HasEffectiveAuthority(NetworkIdentity networkIdentity)
    {
        return networkIdentity && (networkIdentity.hasAuthority || (NetworkServer.active && networkIdentity.connectionToClient == null));
    }

    public static bool LineBlockedByTerrain(Vector3 position1, Vector3 position2, bool debug = false)
    {
        bool cast = Physics.Linecast(position1, position2, FracturedUtility.terrainMask, QueryTriggerInteraction.Ignore);
        if (debug)
        {
            Debug.DrawLine(position1, position2, cast ? Color.red : Color.green, 0.1f);
        }
        return cast;
    }

    public static float TerrainDistance(Vector3 origin, Vector3 direction)
    {
        RaycastHit? hit = TerrainCast(origin,direction);
        if (hit != null)
        {
            return hit.Value.distance;
        }
        else return Mathf.Infinity;
    }

    public static Vector3 GetAimPoint(CharacterMaster characterMaster, bool collideWithCharacters = true)
    {
        Vector3 origin = Camera.main.transform.position;
        Vector3 forward = Camera.main.transform.forward;
        RaycastHit? hit;
        if (collideWithCharacters)
        {
            hit = AimCast(origin, forward, characterMaster);
        }
        else
        {
            hit = TerrainCast(origin, forward);
        }

        if (hit != null)
        {
            return hit.Value.point;
        }

        return origin + forward * 10000f;
    }

    public static RaycastHit? AimCast(Vector3 origin, Vector3 direction, CharacterMaster characterMaster)
    {
        List<RaycastHit> hits = new List<RaycastHit>(Physics.RaycastAll(origin, direction, Mathf.Infinity, terrainMask | (1 << LayerMask.NameToLayer("Hurtbox"))));
        hits.Sort((p1, p2) => SortHitsByDistance(origin, p1, p2));

        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i].collider.gameObject.layer == LayerMask.NameToLayer("Hurtbox"))
            {
                Hitbox hitbox = hits[i].collider.gameObject.GetComponent<Hitbox>();
                if (hitbox == null)
                {
                    continue;
                }
                CharacterMaster otherMaster = hitbox.groups[0].GetComponent<CharacterMaster>();
                if (otherMaster == null || otherMaster.teamIndex != characterMaster.teamIndex)
                {
                    return hits[i];
                }
                else continue;
            }

            return hits[i];
        }

        return null;
    }

    private static int SortHitsByDistance(Vector3 origin, RaycastHit h1, RaycastHit h2)
    {
        float d1 = Vector3.Distance(origin, h1.point);
        float d2 = Vector3.Distance(origin, h2.point);
        return d1.CompareTo(d2);
    }

    public static RaycastHit? TerrainCast(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, Mathf.Infinity, FracturedUtility.terrainMask, QueryTriggerInteraction.Ignore))
        {
            return hit;
        }
        else return null;
    }

    public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime, bool x = true, bool y = true, bool z = true)
    {
        Vector3 c = current.eulerAngles;
        Vector3 t = target.eulerAngles;
        return Quaternion.Euler(
          x ? Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime) : c.x,
          y ? Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime) : c.y,
          z ? Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime) : c.z
        );
    }

    public static int RandomWeighted(List<float> weightings)
    {
        List<int> shuffledWeightings = new List<int>();
        float weightTotal = 0f;
        for (int i = 0; i < weightings.Count; i++)
        {
            shuffledWeightings.Add(i);
            weightTotal += weightings[i];
        }

        // Shuffle array
        int tempWeight;
        for (int i = 0; i < shuffledWeightings.Count - 1; i++)
        {
            int rnd = UnityEngine.Random.Range(i, shuffledWeightings.Count);
            tempWeight = shuffledWeightings[rnd];
            shuffledWeightings[rnd] = shuffledWeightings[i];
            shuffledWeightings[i] = tempWeight;
        }

        int result = 0;
        float total = 0;
        float randVal = UnityEngine.Random.Range(0f, weightTotal);
        for (result = 0; result < weightings.Count; result++)
        {
            total += weightings[shuffledWeightings[result]];
            if (total >= randVal) break;
        }
        return shuffledWeightings[result];
    }

    public static Transform ClosestEnemyInDirection(Vector3 direction, Vector3 origin, int team, bool lineOfSightNeeded = false)
    {
        float lowestAngle = Mathf.Infinity;
        Transform lowestTransform = null;
        float minAngle = 20f;
        foreach (CharacterMaster enemy in CharacterSearcher.searchTargets)
        {
            if (enemy.teamIndex == team)
            {
                continue;
            }
            float angle = Vector3.Angle(direction, (enemy.bodyCenter.position - origin).normalized);
            if (angle <= minAngle)
            {
                if (!lineOfSightNeeded || !FracturedUtility.LineBlockedByTerrain(enemy.bodyCenter.position, origin, true))
                {
                    if (angle < lowestAngle)
                    {
                        lowestAngle = angle;
                        lowestTransform = enemy.bodyCenter;
                    }
                }
            }
        }

        return lowestTransform;
    }

    public static bool IsTransformInLoS(Transform transform, Vector3 direction, Vector3 origin, float maxAngle, float maxRange = Mathf.Infinity)
    {
        // Check range
        float range = Vector3.Distance(transform.position, origin);
        if (range > maxRange)
        {
            return false;
        }

        // Check angle
        float angle = Vector3.Angle(direction, (transform.position - origin).normalized);
        if (angle <= maxAngle)
        {
            if (!FracturedUtility.LineBlockedByTerrain(transform.position, origin, true))
            {
                return true;
            }
        }
        return false;
    }

    public static List<CharacterMaster> GetEnemiesInArea(Vector3 origin, float radius, bool sorted = false, CharacterMaster excludes = null)
    {
        return GetCharactersInArea(origin, radius, sorted, ~(1 << 0), excludes);
    }

    public static List<CharacterMaster> GetCharactersInArea(Vector3 origin, float radius, bool sorted = false, int teamMask = ~0, CharacterMaster excludes = null)
    {
        // Get all character colliders in range
        Collider[] characters = Physics.OverlapSphere(origin, radius, LayerMask.GetMask("Player", "Character", "CharacterNoCollideOtherCharacters"));

        List<CharacterMaster> characterList = new List<CharacterMaster>();
        for (int i = 0; i < characters.Length; i++)
        {
            // Check if character can heal itself
            if (excludes == null || !ReferenceEquals(characters[i].gameObject, excludes.gameObject))
            {
                CharacterMaster otherMaster = characters[i].GetComponent<CharacterMaster>();
                if (otherMaster != null && (teamMask & 1 << otherMaster.teamIndex) == (1 << otherMaster.teamIndex))
                {
                    characterList.Add(otherMaster);
                }
            }
        }

        if (sorted)
        {
            characterList.Sort((p1, p2) => Vector3.Distance(p1.bodyCenter.position, origin).CompareTo(Vector3.Distance(p2.bodyCenter.position, origin)));
        }

        return characterList;
    }

    public static float WrapAngle(float angle)
    {
        if (angle < 0)
        {
            angle = 360 + angle;
        }
        if (angle > 360)
        {
            angle = (angle - 360);
        }
        return angle;
    }
}

using UnityEngine;

/// <summary>
/// Shared utility methods used by SafeRandomSpawner and EcsSpawnBridge to find
/// overlap-free placement positions using Physics.CheckSphere against the spawnFootprint layer.
/// </summary>
public static class SpawnPlacementUtils
{
    public struct FootprintSphere
    {
        public Vector3 LocalOffset;
        public float   Radius;
    }

    /// <summary>
    /// Collects all SphereColliders on the spawnFootprint layer from the prefab hierarchy.
    /// Returns an empty array if none are found.
    /// </summary>
    public static FootprintSphere[] CollectFootprints(GameObject prefab)
    {
        var colliders = prefab.GetComponentsInChildren<SphereCollider>(true);
        int spawnFootprintLayer = (int)Layers.spawnFootprint;

        int count = 0;
        foreach (var col in colliders)
        {
            if (col.gameObject.layer == spawnFootprintLayer)
                count++;
        }

        var result = new FootprintSphere[count];
        int index = 0;
        foreach (var col in colliders)
        {
            if (col.gameObject.layer != spawnFootprintLayer)
                continue;

            result[index++] = new FootprintSphere
            {
                LocalOffset = col.center,
                Radius      = col.radius * col.transform.lossyScale.x,
            };
        }

        return result;
    }

    /// <summary>
    /// Tries up to maxAttempts times to find a position within [minX,maxX] x [minZ,maxZ]
    /// on the ground plane where no footprint sphere overlaps anything on the spawnFootprint layer.
    /// Returns true and writes the position to result on success.
    /// </summary>
    public static bool TryFindPosition(
        FootprintSphere[] footprints,
        float minX, float maxX,
        float minZ, float maxZ,
        float spawnY,
        int maxAttempts,
        out Vector3 result)
    {
        int footprintMask = 1 << (int)Layers.spawnFootprint;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = new Vector3(
                Random.Range(minX, maxX),
                spawnY,
                Random.Range(minZ, maxZ));

            if (IsPositionClear(footprints, candidate, footprintMask))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Variant that samples within a circle of searchRadius around origin.
    /// Used by EcsSpawnBridge for enemy-triggered spawns.
    /// Z is kept from origin.
    /// </summary>
    public static bool TryFindPositionAround(
        FootprintSphere[] footprints,
        Vector3 origin,
        float searchRadius,
        int maxAttempts,
        out Vector3 result)
    {
        int footprintMask = 1 << (int)Layers.spawnFootprint;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 offset    = Random.insideUnitCircle * searchRadius;
            var     candidate = new Vector3(origin.x + offset.x, origin.y, origin.z + offset.y);

            if (IsPositionClear(footprints, candidate, footprintMask))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Destroys every child GameObject that lives on the spawnFootprint layer.
    /// Call this immediately after Instantiate to prevent footprint colliders from
    /// being baked into ECS entities.
    /// </summary>
    public static void RemoveSpawnFootprints(GameObject instance)
    {
        int spawnFootprintLayer = (int)Layers.spawnFootprint;
        var allTransforms = instance.GetComponentsInChildren<Transform>(true);

        // Collect first, destroy after — avoids mutating the hierarchy mid-iteration.
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform t in allTransforms)
        {
            if (t.gameObject.layer == spawnFootprintLayer && t.gameObject != instance)
                toDestroy.Add(t.gameObject);
        }

        foreach (GameObject go in toDestroy)
        {
            if (go != null)
                Object.Destroy(go);
        }
    }

    // Returns true when none of the footprint spheres overlap spawnFootprint colliders at candidate.
    private static bool IsPositionClear(FootprintSphere[] footprints, Vector3 candidate, int footprintMask)
    {
        foreach (var sphere in footprints)
        {
            Vector3 worldPos = candidate + sphere.LocalOffset;
            if (Physics.CheckSphere(worldPos, sphere.Radius, footprintMask, QueryTriggerInteraction.Collide))
                return false;
        }
        return true;
    }
}

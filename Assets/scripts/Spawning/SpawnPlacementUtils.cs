using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Managed spawn placement helpers for EcsSpawnBridge using Physics.CheckSphere
/// against the spawnProtection layer.
/// </summary>
public static class SpawnPlacementUtils
{
    public struct ProtectionSphere
    {
        public Vector3 LocalOffset;
        public float   Radius;
    }

    public struct ProtectionCollection
    {
        public ProtectionSphere[] Spheres;
        public uint               LayerMask;
    }

    /// <summary>
    /// Collects spawnProtection colliders from the prefab hierarchy.
    /// </summary>
    public static ProtectionCollection CollectProtection(GameObject prefab)
    {
        var colliders = prefab.GetComponentsInChildren<UnityEngine.Collider>(true);
        int layer = (int)Layers.spawnProtection;

        int count = 0;
        foreach (var col in colliders)
        {
            if (col.gameObject.layer == layer)
                count++;
        }

        var spheres = new ProtectionSphere[count];
        int index = 0;
        foreach (var col in colliders)
        {
            if (col.gameObject.layer != layer)
                continue;

            if (col is UnityEngine.SphereCollider sphere)
            {
                spheres[index++] = new ProtectionSphere
                {
                    LocalOffset = sphere.center,
                    Radius      = sphere.radius * sphere.transform.lossyScale.x,
                };
            }
            else
            {
                Bounds bounds = col.bounds;
                Vector3 localCenter = prefab.transform.InverseTransformPoint(bounds.center);
                spheres[index++] = new ProtectionSphere
                {
                    LocalOffset = localCenter,
                    Radius      = math.max(bounds.extents.x, bounds.extents.z),
                };
            }
        }

        return new ProtectionCollection
        {
            Spheres   = spheres,
            LayerMask = SpawnProtection.LayerMask,
        };
    }

    public static float ComputePushStepDistance(ProtectionSphere[] spheres)
    {
        float maxExtent = 0.01f;
        foreach (var sphere in spheres)
        {
            float extent = sphere.LocalOffset.magnitude + sphere.Radius;
            if (extent > maxExtent)
                maxExtent = extent;
        }

        return maxExtent * 2f;
    }

    public static bool TryFindPosition(
        ProtectionCollection protection,
        float minX, float maxX,
        float minZ, float maxZ,
        float spawnY,
        int maxAttempts,
        out Vector3 result)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = new Vector3(
                UnityEngine.Random.Range(minX, maxX),
                spawnY,
                UnityEngine.Random.Range(minZ, maxZ));

            if (IsPositionClear(protection, candidate))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    public static bool TryFindPositionAround(
        ProtectionCollection protection,
        Vector3 origin,
        float searchRadius,
        int maxAttempts,
        out Vector3 result)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 offset    = UnityEngine.Random.insideUnitCircle * searchRadius;
            var     candidate = new Vector3(origin.x + offset.x, origin.y, origin.z + offset.y);

            if (IsPositionClear(protection, candidate))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Destroys spawnProtection child objects after GameObject.Instantiate so they
    /// are not left in the live scene hierarchy.
    /// </summary>
    public static void RemoveSpawnProtection(GameObject instance)
    {
        int layer = (int)Layers.spawnProtection;
        var allTransforms = instance.GetComponentsInChildren<Transform>(true);

        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform t in allTransforms)
        {
            if (t.gameObject.layer == layer && t.gameObject != instance)
                toDestroy.Add(t.gameObject);
        }

        foreach (GameObject go in toDestroy)
        {
            if (go != null)
                Object.Destroy(go);
        }
    }

    private static bool IsPositionClear(ProtectionCollection protection, Vector3 candidate)
    {
        foreach (var sphere in protection.Spheres)
        {
            Vector3 worldPos = candidate + sphere.LocalOffset;
            if (Physics.CheckSphere(worldPos, sphere.Radius, (int)protection.LayerMask, QueryTriggerInteraction.Collide))
                return false;
        }

        return true;
    }
}

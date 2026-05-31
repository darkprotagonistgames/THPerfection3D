using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using PhysicsCollider = Unity.Physics.Collider;
using PhysicsSphereCollider = Unity.Physics.SphereCollider;

/// <summary>
/// Shared utility methods used by SafeRandomSpawner and EcsSpawnBridge to find
/// overlap-free placement positions using physics collider queries.
/// </summary>
public static class SpawnPlacementUtils
{
    public struct FootprintSphere
    {
        public Vector3 LocalOffset;
        public float   Radius;
    }

    public struct FootprintCollection
    {
        public FootprintSphere[] Spheres;
        public uint              LayerMask;
    }

    /// <summary>
    /// Collects all SphereColliders on the spawnFootprint layer from the prefab hierarchy.
    /// LayerMask is derived from each collider's GameObject layer.
    /// </summary>
    public static FootprintCollection CollectFootprints(GameObject prefab)
    {
        var colliders = prefab.GetComponentsInChildren<UnityEngine.SphereCollider>(true);
        int spawnFootprintLayer = (int)Layers.spawnFootprint;

        int count = 0;
        foreach (var col in colliders)
        {
            if (col.gameObject.layer == spawnFootprintLayer)
                count++;
        }

        var spheres = new FootprintSphere[count];
        uint layerMask = 0;
        int index = 0;
        foreach (var col in colliders)
        {
            if (col.gameObject.layer != spawnFootprintLayer)
                continue;

            layerMask |= 1u << col.gameObject.layer;
            spheres[index++] = new FootprintSphere
            {
                LocalOffset = col.center,
                Radius      = col.radius * col.transform.lossyScale.x,
            };
        }

        return new FootprintCollection
        {
            Spheres   = spheres,
            LayerMask = layerMask,
        };
    }

    /// <summary>
    /// Builds a Unity Physics collider blob from collected footprint spheres.
    /// </summary>
    public static BlobAssetReference<PhysicsCollider> CreateFootprintPhysicsCollider(FootprintCollection footprints)
    {
        if (footprints.Spheres.Length == 0)
            return default;

        var filter = new CollisionFilter
        {
            BelongsTo    = footprints.LayerMask,
            CollidesWith = footprints.LayerMask,
        };

        if (footprints.Spheres.Length == 1)
        {
            FootprintSphere sphere = footprints.Spheres[0];
            return PhysicsSphereCollider.Create(
                new SphereGeometry
                {
                    Center = (float3)sphere.LocalOffset,
                    Radius = sphere.Radius,
                },
                filter);
        }

        var instances = new NativeArray<CompoundCollider.ColliderBlobInstance>(
            footprints.Spheres.Length, Allocator.Temp);
        try
        {
            for (int i = 0; i < footprints.Spheres.Length; i++)
            {
                FootprintSphere sphere = footprints.Spheres[i];
                instances[i] = new CompoundCollider.ColliderBlobInstance
                {
                    Collider = PhysicsSphereCollider.Create(
                        new SphereGeometry { Center = float3.zero, Radius = sphere.Radius },
                        CollisionFilter.Default),
                    CompoundFromChild = new RigidTransform(quaternion.identity, (float3)sphere.LocalOffset),
                };
            }

            return CompoundCollider.Create(instances);
        }
        finally
        {
            instances.Dispose();
        }
    }

    /// <summary>
    /// Push distance used when nudging a rejected candidate — twice the largest footprint reach.
    /// </summary>
    public static float ComputePushStepDistance(FootprintSphere[] footprints)
    {
        float maxExtent = 0.01f;
        foreach (var sphere in footprints)
        {
            float extent = sphere.LocalOffset.magnitude + sphere.Radius;
            if (extent > maxExtent)
                maxExtent = extent;
        }

        return maxExtent * 2f;
    }

    /// <summary>
    /// Push distance from a baked physics collider blob.
    /// </summary>
    public static float ComputePushStepDistance(BlobAssetReference<PhysicsCollider> footprintCollider)
    {
        if (!footprintCollider.IsCreated)
            return 1f;

        Aabb  aabb    = footprintCollider.Value.CalculateAabb(RigidTransform.identity);
        float extentX = aabb.Max.x - aabb.Min.x;
        float extentZ = aabb.Max.z - aabb.Min.z;
        return math.max(extentX, extentZ);
    }

    /// <summary>
    /// Tries up to maxAttempts times to find a position within [minX,maxX] x [minZ,maxZ]
    /// on the ground plane where the footprint collider does not overlap anything.
    /// </summary>
    public static bool TryFindPosition(
        FootprintCollection footprints,
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

            if (IsPositionClear(footprints, candidate))
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
    /// </summary>
    public static bool TryFindPositionAround(
        FootprintCollection footprints,
        Vector3 origin,
        float searchRadius,
        int maxAttempts,
        out Vector3 result)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 offset    = UnityEngine.Random.insideUnitCircle * searchRadius;
            var     candidate = new Vector3(origin.x + offset.x, origin.y, origin.z + offset.y);

            if (IsPositionClear(footprints, candidate))
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

    private static bool IsPositionClear(FootprintCollection footprints, Vector3 candidate)
    {
        foreach (var sphere in footprints.Spheres)
        {
            Vector3 worldPos = candidate + sphere.LocalOffset;
            if (Physics.CheckSphere(worldPos, sphere.Radius, (int)footprints.LayerMask, QueryTriggerInteraction.Collide))
                return false;
        }

        return true;
    }
}

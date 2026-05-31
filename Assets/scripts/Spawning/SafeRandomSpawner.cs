using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Authoring component. Place inside a subscene to scatter entity prefabs across a
/// rectangular area at startup. Bakes all parameters into a blob asset so the spawner
/// system can run fully Burst-compiled with no managed allocations.
/// </summary>
public class SafeRandomSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("The entity prefab to spawn.")]
    public GameObject Prefab;

    [Header("Spawn Area (XZ)")]
    public Vector2 BoundsMin = new Vector2(-10f, -10f);
    public Vector2 BoundsMax = new Vector2(10f,  10f);

    [Header("Spawn Settings")]
    [Min(1)] public int SpawnCount = 10;

    [Tooltip("Minimum separation radius between spawns and static colliders.")]
    [Min(0.01f)] public float FootprintRadius = 0.5f;

    [Tooltip("Max pushes per spawn before it is discarded.")]
    [Min(1)] public int MaxPushAttempts = 20;

    [Tooltip("Deterministic seed. Leave 0 to auto-derive from position.")]
    public uint RandomSeed = 0;

    public class Baker : Baker<SafeRandomSpawner>
    {
        public override void Bake(SafeRandomSpawner authoring)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref SpawnConfigBlob root = ref builder.ConstructRoot<SpawnConfigBlob>();

            root.BoundsMin       = authoring.BoundsMin;
            root.BoundsMax       = authoring.BoundsMax;
            root.SpawnGroundY    = authoring.transform.position.y;
            root.FootprintRadius = authoring.FootprintRadius;
            root.SpawnCount      = authoring.SpawnCount;
            root.MaxPushAttempts = authoring.MaxPushAttempts;

            // Auto-seed: hash bounds + count so two spawners with different configs
            // get different distributions without needing manual seed assignment.
            root.Seed = authoring.RandomSeed != 0
                ? authoring.RandomSeed
                : (uint)math.hash(new float4(
                    authoring.BoundsMin.x, authoring.BoundsMin.y,
                    authoring.BoundsMax.x, authoring.SpawnCount));

            var blobRef = builder.CreateBlobAssetReference<SpawnConfigBlob>(Allocator.Persistent);

            // AddBlobAsset hashes the asset so identical configs share one allocation.
            AddBlobAsset(ref blobRef, out _);

            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SafeRandomSpawnerData
            {
                EntityPrefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                Config       = blobRef,
            });
        }
    }
}

/// <summary>Blob-stored spawn configuration. Shared across identical spawner configs.</summary>
public struct SpawnConfigBlob
{
    public float2 BoundsMin;
    public float2 BoundsMax;
    public float  SpawnGroundY;
    public float  FootprintRadius;
    public int    SpawnCount;
    public int    MaxPushAttempts;
    public uint   Seed;
}

public struct SafeRandomSpawnerData : IComponentData
{
    /// <summary>Entity prefab instantiated for each confirmed spawn position.</summary>
    public Entity EntityPrefab;

    /// <summary>Blob reference to the shared spawn configuration.</summary>
    public BlobAssetReference<SpawnConfigBlob> Config;
}

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Authoring component. Place inside a subscene to scatter entity prefabs across a
/// rectangular area at startup. Prefabs must have a spawnProtection collider child.
/// </summary>
public class SafeRandomSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Entity prefab with a spawnProtection collider on layer spawnProtection.")]
    public GameObject Prefab;

    [Header("Spawn Area (XZ)")]
    public Vector2 BoundsMin = new Vector2(-10f, -10f);
    public Vector2 BoundsMax = new Vector2(10f,  10f);

    [Header("Spawn Settings")]
    [Min(1)] public int SpawnCount = 10;

    [Tooltip("Max pushes per spawn before it is discarded.")]
    [Min(1)] public int MaxPushAttempts = 20;

    [Tooltip("Deterministic seed. Leave 0 to auto-derive from position.")]
    public uint RandomSeed = 0;

    public class Baker : Baker<SafeRandomSpawner>
    {
        public override void Bake(SafeRandomSpawner authoring)
        {
            DependsOn(authoring.Prefab);

            SpawnPlacementUtils.ProtectionCollection protection =
                SpawnPlacementUtils.CollectProtection(authoring.Prefab);

            using var builder = new BlobBuilder(Allocator.Temp);
            ref SpawnConfigBlob root = ref builder.ConstructRoot<SpawnConfigBlob>();

            root.BoundsMin       = authoring.BoundsMin;
            root.BoundsMax       = authoring.BoundsMax;
            root.SpawnGroundY    = authoring.transform.position.y;
            root.SpawnCount      = authoring.SpawnCount;
            root.MaxPushAttempts = authoring.MaxPushAttempts;

            root.Seed = authoring.RandomSeed != 0
                ? authoring.RandomSeed
                : (uint)math.hash(new float4(
                    authoring.BoundsMin.x, authoring.BoundsMin.y,
                    authoring.BoundsMax.x, authoring.SpawnCount));

            BlobBuilderArray<SpawnProtectionSphereBlob> sphereArray =
                builder.Allocate(ref root.ProtectionSpheres, protection.Spheres.Length);

            for (int i = 0; i < protection.Spheres.Length; i++)
            {
                sphereArray[i] = new SpawnProtectionSphereBlob
                {
                    LocalOffset = protection.Spheres[i].LocalOffset,
                    Radius      = protection.Spheres[i].Radius,
                };
            }

            var blobRef = builder.CreateBlobAssetReference<SpawnConfigBlob>(Allocator.Persistent);
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

public struct SpawnProtectionSphereBlob
{
    public float3 LocalOffset;
    public float  Radius;
}

public struct SpawnConfigBlob
{
    public float2 BoundsMin;
    public float2 BoundsMax;
    public float  SpawnGroundY;
    public int    SpawnCount;
    public int    MaxPushAttempts;
    public uint   Seed;
    public BlobArray<SpawnProtectionSphereBlob> ProtectionSpheres;
}

public struct SafeRandomSpawnerData : IComponentData
{
    public Entity EntityPrefab;
    public BlobAssetReference<SpawnConfigBlob> Config;
}

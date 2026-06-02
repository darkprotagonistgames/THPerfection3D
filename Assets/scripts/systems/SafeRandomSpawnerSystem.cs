using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Spawner for the XZ ground plane. Uses spawnProtection sphere geometry and the
/// layer matrix (spawnProtection collides only with spawnProtection).
/// Runs at the start of simulation, before <see cref="TransformSystemGroup"/>, so spawned entities
/// get world transforms before the first physics step (subscene-safe; Init group can run before subscenes load).
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct SafeRandomSpawnerSystem : ISystem
{
    private static readonly CollisionFilter ProtectionFilter = new()
    {
        BelongsTo    = SpawnProtection.LayerMask,
        CollidesWith = SpawnProtection.LayerMask,
    };

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SafeRandomSpawnerData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        bool hasPhysicsWorld = SystemAPI.TryGetSingleton(out PhysicsWorldSingleton physicsWorld);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawner, spawnerEntity) in
                 SystemAPI.Query<RefRO<SafeRandomSpawnerData>>().WithEntityAccess())
        {
            ref SpawnConfigBlob config = ref spawner.ValueRO.Config.Value;

            if (config.ProtectionSpheres.Length == 0)
                continue;

            float pushStep = ComputePushStepDistance(ref config);

            var rng       = new Random(config.Seed);
            var confirmed = new NativeList<float3>(config.SpawnCount, Allocator.Temp);

            for (int i = 0; i < config.SpawnCount; i++)
            {
                float3 candidate = new(
                    rng.NextFloat(config.BoundsMin.x, config.BoundsMax.x),
                    config.SpawnGroundY,
                    rng.NextFloat(config.BoundsMin.y, config.BoundsMax.y));

                bool placed = false;
                for (int push = 0; push < config.MaxPushAttempts; push++)
                {
                    if (push > 0)
                    {
                        float2 dir = rng.NextFloat2Direction();
                        candidate += new float3(dir.x, 0f, dir.y) * pushStep;
                        candidate.x = math.clamp(candidate.x, config.BoundsMin.x, config.BoundsMax.x);
                        candidate.z = math.clamp(candidate.z, config.BoundsMin.y, config.BoundsMax.y);
                    }

                    if (!OverlapsAnything(hasPhysicsWorld, in physicsWorld, in candidate, ref config, in confirmed))
                    {
                        placed = true;
                        break;
                    }
                }

                if (placed)
                    confirmed.Add(candidate);
            }

            for (int i = 0; i < confirmed.Length; i++)
            {
                Entity spawned = ecb.Instantiate(spawner.ValueRO.EntityPrefab);
                ecb.SetComponent(spawned, LocalTransform.FromPosition(confirmed[i]));
                ecb.AddComponent<SpawnInvulnerabilityTag>(spawned);
            }

            confirmed.Dispose();
            ecb.RemoveComponent<SafeRandomSpawnerData>(spawnerEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static bool OverlapsAnything(
        bool hasPhysicsWorld,
        in PhysicsWorldSingleton physicsWorld,
        in float3 position,
        ref SpawnConfigBlob config,
        in NativeList<float3> confirmed)
    {
        if (hasPhysicsWorld && OverlapsPhysicsWorld(in physicsWorld, in position, ref config))
            return true;

        for (int i = 0; i < confirmed.Length; i++)
        {
            if (ProtectionOverlaps(in position, confirmed[i], ref config))
                return true;
        }

        return false;
    }

    private static bool OverlapsPhysicsWorld(
        in PhysicsWorldSingleton physicsWorld,
        in float3 position,
        ref SpawnConfigBlob config)
    {
        var hits = new NativeList<DistanceHit>(Allocator.Temp);

        for (int i = 0; i < config.ProtectionSpheres.Length; i++)
        {
            ref SpawnProtectionSphereBlob sphere = ref config.ProtectionSpheres[i];
            float3 worldPos = position + sphere.LocalOffset;

            hits.Clear();
            if (physicsWorld.OverlapSphere(
                    worldPos,
                    sphere.Radius,
                    ref hits,
                    ProtectionFilter,
                    QueryInteraction.Default))
            {
                hits.Dispose();
                return true;
            }
        }

        hits.Dispose();
        return false;
    }

    private static bool ProtectionOverlaps(
        in float3 positionA,
        in float3 positionB,
        ref SpawnConfigBlob config)
    {
        for (int i = 0; i < config.ProtectionSpheres.Length; i++)
        {
            ref SpawnProtectionSphereBlob sphereA = ref config.ProtectionSpheres[i];
            float3 centerA = positionA + sphereA.LocalOffset;

            for (int j = 0; j < config.ProtectionSpheres.Length; j++)
            {
                ref SpawnProtectionSphereBlob sphereB = ref config.ProtectionSpheres[j];
                float3 centerB = positionB + sphereB.LocalOffset;

                float minDistance = sphereA.Radius + sphereB.Radius;
                if (math.distancesq(centerA, centerB) < minDistance * minDistance)
                    return true;
            }
        }

        return false;
    }

    private static float ComputePushStepDistance(ref SpawnConfigBlob config)
    {
        float maxExtent = 0.01f;

        for (int i = 0; i < config.ProtectionSpheres.Length; i++)
        {
            ref SpawnProtectionSphereBlob sphere = ref config.ProtectionSpheres[i];
            float extent = math.length(sphere.LocalOffset) + sphere.Radius;
            maxExtent = math.max(maxExtent, extent);
        }

        return maxExtent * 2f;
    }
}

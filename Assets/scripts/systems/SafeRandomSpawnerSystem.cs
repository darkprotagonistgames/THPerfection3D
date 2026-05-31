using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Burst-compiled spawner for the XZ ground plane. BoundsMin/BoundsMax are X and Z extents.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SafeRandomSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SafeRandomSpawnerData>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (spawner, spawnerEntity) in
            SystemAPI.Query<RefRO<SafeRandomSpawnerData>>().WithEntityAccess())
        {
            ref readonly SpawnConfigBlob config = ref spawner.ValueRO.Config.Value;
            Entity prefab = spawner.ValueRO.EntityPrefab;

            var rng = new Random(config.Seed);
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
                        candidate += new float3(dir.x, 0f, dir.y) * (config.FootprintRadius * 2f);
                        candidate.x = math.clamp(candidate.x, config.BoundsMin.x, config.BoundsMax.x);
                        candidate.z = math.clamp(candidate.z, config.BoundsMin.y, config.BoundsMax.y);
                    }

                    if (!OverlapsAnything(in physicsWorld, in candidate, config.FootprintRadius, in confirmed))
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
                Entity spawned = ecb.Instantiate(prefab);
                ecb.SetComponent(spawned, LocalTransform.FromPosition(confirmed[i]));
            }

            confirmed.Dispose();
            ecb.RemoveComponent<SafeRandomSpawnerData>(spawnerEntity);
        }
    }

    [BurstCompile]
    private static bool OverlapsAnything(
        in PhysicsWorldSingleton physicsWorld,
        in float3 position,
        float radius,
        in NativeList<float3> confirmed)
    {
        var query = new PointDistanceInput
        {
            Position = position,
            MaxDistance = radius,
            Filter = CollisionFilter.Default,
        };

        if (physicsWorld.CalculateDistance(query))
            return true;

        float minSepSq = (radius * 2f) * (radius * 2f);
        for (int i = 0; i < confirmed.Length; i++)
        {
            if (math.distancesq(position, confirmed[i]) < minSepSq)
                return true;
        }

        return false;
    }
}

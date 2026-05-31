using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using PhysicsCollider = Unity.Physics.Collider;

/// <summary>
/// Spawner for the XZ ground plane. Runs early in simulation so the physics world
/// is ready (Init group may finish before PhysicsWorldSingleton exists).
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial struct SafeRandomSpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SafeRandomSpawnerData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton(out PhysicsWorldSingleton physicsWorld))
            return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawner, spawnerEntity) in
                 SystemAPI.Query<RefRO<SafeRandomSpawnerData>>().WithEntityAccess())
        {
            ref SpawnConfigBlob config = ref spawner.ValueRO.Config.Value;
            BlobAssetReference<PhysicsCollider> footprintCollider = spawner.ValueRO.FootprintCollider;
            Entity prefab = spawner.ValueRO.EntityPrefab;

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
                        candidate += new float3(dir.x, 0f, dir.y) * config.PushStepDistance;
                        candidate.x = math.clamp(candidate.x, config.BoundsMin.x, config.BoundsMax.x);
                        candidate.z = math.clamp(candidate.z, config.BoundsMin.y, config.BoundsMax.y);
                    }

                    if (!OverlapsAnything(in physicsWorld, in candidate, ref config, footprintCollider, in confirmed))
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

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static bool OverlapsAnything(
        in PhysicsWorldSingleton physicsWorld,
        in float3 position,
        ref SpawnConfigBlob config,
        BlobAssetReference<PhysicsCollider> footprintCollider,
        in NativeList<float3> confirmed)
    {
        if (!footprintCollider.IsCreated)
            return false;

        if (OverlapsPhysicsWorld(in physicsWorld, in position, ref config, footprintCollider))
            return true;

        for (int i = 0; i < confirmed.Length; i++)
        {
            if (FootprintsOverlap(in position, confirmed[i], footprintCollider))
                return true;
        }

        return false;
    }

    private static bool OverlapsPhysicsWorld(
        in PhysicsWorldSingleton physicsWorld,
        in float3 position,
        ref SpawnConfigBlob config,
        BlobAssetReference<PhysicsCollider> footprintCollider)
    {
        if (config.FootprintLayerMask == 0)
            return false;

        var input = new ColliderDistanceInput(
            footprintCollider,
            0f,
            new RigidTransform(quaternion.identity, position));

        return physicsWorld.CalculateDistance(input);
    }

    private static bool FootprintsOverlap(
        in float3 positionA,
        in float3 positionB,
        BlobAssetReference<PhysicsCollider> footprintCollider)
    {
        RigidTransform transformA = new RigidTransform(quaternion.identity, positionA);
        RigidTransform transformB = new RigidTransform(quaternion.identity, positionB);
        RigidTransform bInA       = math.mul(math.inverse(transformA), transformB);

        var input = new ColliderDistanceInput(footprintCollider, 0f, bInA);
        return footprintCollider.Value.CalculateDistance(input);
    }
}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Runs once during initialization (before simulation and rendering) to spawn all
/// registered prefabs at their configured positions. Placing this in
/// InitializationSystemGroup guarantees the entity is correctly positioned before
/// physics or rendering ever process it.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PrefabSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PrefabSpawnerData>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawner, spawnerEntity) in
            SystemAPI.Query<RefRO<PrefabSpawnerData>>().WithEntityAccess())
        {
            Entity spawned = ecb.Instantiate(spawner.ValueRO.Prefab);

            ecb.SetComponent(spawned, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition));

            // Remove so this spawner never fires again
            ecb.RemoveComponent<PrefabSpawnerData>(spawnerEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

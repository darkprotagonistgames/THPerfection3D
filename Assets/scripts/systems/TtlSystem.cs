using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Decrements <see cref="TtlData.SecondsRemaining"/> each frame and destroys the host entity when it expires.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct TtlSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TtlData>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (ttl, entity) in SystemAPI.Query<RefRW<TtlData>>().WithEntityAccess())
        {
            ttl.ValueRW.SecondsRemaining -= deltaTime;
            if (ttl.ValueRO.SecondsRemaining <= 0f)
                ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

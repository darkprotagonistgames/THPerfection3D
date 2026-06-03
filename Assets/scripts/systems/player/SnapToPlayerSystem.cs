using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Copies the player entity's <see cref="LocalTransform"/> onto every entity with <see cref="SnapToPlayerTag"/>.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct SnapToPlayerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerMovementData>();
        state.RequireForUpdate<SnapToPlayerTag>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        LocalTransform playerTransform = SystemAPI.GetComponent<LocalTransform>(
            SystemAPI.GetSingletonEntity<PlayerMovementData>());

        new SnapToPlayerJob { PlayerTransform = playerTransform }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct SnapToPlayerJob : IJobEntity
    {
        public LocalTransform PlayerTransform;

        public void Execute(ref LocalTransform localTransform, in SnapToPlayerTag _)
        {
            localTransform = PlayerTransform;
        }
    }
}

using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Writes the player XZ position into <see cref="MoveToTarget"/> for followers.
/// </summary>
[BurstCompile]
[UpdateBefore(typeof(MoveToSystem))]
public partial struct FollowPlayerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerMovementData>();
        state.RequireForUpdate<FollowPlayerTag>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerMovementData>();
        LocalTransform playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
        float2 playerPosition = TopDownPlane.FromPosition(playerTransform.Position);

        new FollowPlayerJob { PlayerPosition = playerPosition }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct FollowPlayerJob : IJobEntity
    {
        public float2 PlayerPosition;

        public void Execute(ref MoveToTarget moveToTarget, in FollowPlayerTag _)
        {
            moveToTarget.TargetPosition = PlayerPosition;
            moveToTarget.IsActive = true;
        }
    }
}

using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Rotates characters with <see cref="FaceVelocityRotation"/> so their forward
/// axis points along their XZ <see cref="PhysicsVelocity"/>.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(PlayerMovementSystem))]
[UpdateAfter(typeof(MoveToSystem))]
public partial struct FaceVelocityRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FaceVelocityRotation>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new FaceVelocityRotationJob().ScheduleParallel();
    }

    [BurstCompile]
    public partial struct FaceVelocityRotationJob : IJobEntity
    {
        public void Execute(
            ref LocalTransform transform,
            in PhysicsVelocity physicsVelocity,
            in FaceVelocityRotation settings)
        {
            float2 velocityXZ = TopDownPlane.FromPosition(physicsVelocity.Linear);
            if (math.lengthsq(velocityXZ) < settings.MinSpeedSq)
                return;

            float3 direction = math.normalize(new float3(velocityXZ.x, 0f, velocityXZ.y));
            transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
        }
    }
}

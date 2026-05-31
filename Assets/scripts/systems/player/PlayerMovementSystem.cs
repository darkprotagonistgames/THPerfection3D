using THPerfection.GeneratedEvents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public struct PlayerMovementData : IComponentData
{
    public float Acceleration;
    public float MaxSpeed;
}

/// <summary>
/// Input data for the player. Move is normalized XZ top-down input
/// (x: strafe, y: forward/back on the ground plane).
/// </summary>
public struct PlayerInputData : IComponentData
{
    public float2 Move;
    public bool Jump;
}

[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new PlayerMovementJob
        {
            DeltaTime = deltaTime,
            ECB = ecb.AsParallelWriter(),
        };

        job.ScheduleParallel();
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct PlayerMovementJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;

        public void Execute([ChunkIndexInQuery] int sortKey, ref PhysicsVelocity physicsVelocity, in PlayerMovementData movement, in PlayerInputData input, Entity entity)
        {
            float2 inputDir = input.Move;
            float inputMagnitude = math.length(inputDir);

            float2 desiredVelocity = float2.zero;
            if (inputMagnitude > 0f)
            {
                float2 dir = inputDir / inputMagnitude;
                desiredVelocity = dir * movement.MaxSpeed;
            }

            float2 currentVelocityXZ = TopDownPlane.FromPosition(physicsVelocity.Linear);
            float2 newVelocityXZ = MoveTowards(
                currentVelocityXZ,
                desiredVelocity,
                movement.Acceleration * DeltaTime
            );

            physicsVelocity.Linear = TopDownPlane.ToLinear(newVelocityXZ, physicsVelocity.Linear.y);

            if (input.Jump)
            {
                entity.CreatejumpEvent(ECB, sortKey, 1);
            }
        }

        private static float2 MoveTowards(float2 current, float2 target, float maxDelta)
        {
            float2 delta = target - current;
            float distance = math.length(delta);

            if (distance <= maxDelta || distance == 0f)
            {
                return target;
            }

            return current + delta * (maxDelta / distance);
        }
    }
}

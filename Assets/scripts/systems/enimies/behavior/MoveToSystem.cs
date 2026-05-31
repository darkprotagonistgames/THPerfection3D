using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Accelerates entities with <see cref="MoveToTarget"/> toward their target on the XZ plane.
/// </summary>
[BurstCompile]
public partial struct MoveToSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MoveToTarget>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new MoveToJob { DeltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct MoveToJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            ref PhysicsVelocity physicsVelocity,
            in LocalTransform transform,
            in MoveToTarget moveToTarget,
            in MoveToStatsReference statsRef)
        {
            if (!moveToTarget.IsActive)
                return;

            ref readonly MoveToStats stats = ref statsRef.Stats.Value;

            float2 worldPosition = TopDownPlane.FromPosition(transform.Position);
            float2 toTarget = moveToTarget.TargetPosition - worldPosition;
            float distanceToTarget = math.length(toTarget);

            float2 currentVelocity = TopDownPlane.FromPosition(physicsVelocity.Linear);

            if (distanceToTarget < 0.01f)
            {
                float2 braked = MoveTowards(currentVelocity, float2.zero, stats.Acceleration * DeltaTime);
                physicsVelocity.Linear = TopDownPlane.ToLinear(braked, physicsVelocity.Linear.y);
                return;
            }

            float2 desiredVelocity = (toTarget / distanceToTarget) * stats.MaxMoveSpeed;
            float2 newVelocity = MoveTowards(currentVelocity, desiredVelocity, stats.Acceleration * DeltaTime);

            physicsVelocity.Linear = TopDownPlane.ToLinear(newVelocity, physicsVelocity.Linear.y);
        }

        private static float2 MoveTowards(float2 current, float2 target, float maxDelta)
        {
            float2 delta = target - current;
            float distance = math.length(delta);

            if (distance <= maxDelta || distance == 0f)
                return target;

            return current + delta * (maxDelta / distance);
        }
    }
}

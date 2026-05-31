using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Drives a companion Animator from ECS physics velocity and rotates the entity on the XZ plane.
/// </summary>
[UpdateAfter(typeof(MoveToSystem))]
[UpdateAfter(typeof(PlayerMovementSystem))]
public partial class MecanimLocomotionSystem : SystemBase
{
    private const float MovingSpeedThresholdSq = 0.01f;

    protected override void OnCreate()
    {
        RequireForUpdate<MecanimLocomotionTag>();
    }

    protected override void OnUpdate()
    {
        foreach (var (transform, velocity, animState, entity) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRO<PhysicsVelocity>, RefRO<MecanimLocomotionState>>()
                     .WithAll<MecanimLocomotionTag>()
                     .WithEntityAccess())
        {
            if (!EntityManager.HasComponent<Animator>(entity))
                continue;

            Animator animator = EntityManager.GetComponentObject<Animator>(entity);
            if (animator == null)
                continue;

            float2 velocityXZ = TopDownPlane.FromPosition(velocity.ValueRO.Linear);
            bool moving = math.lengthsq(velocityXZ) > MovingSpeedThresholdSq;

            if (moving)
            {
                float yaw = math.degrees(math.atan2(velocityXZ.x, velocityXZ.y));
                transform.ValueRW.Rotation = quaternion.RotateY(math.radians(yaw));

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (animState.ValueRO.WalkStateHash != 0 &&
                    stateInfo.shortNameHash != animState.ValueRO.WalkStateHash &&
                    !animator.IsInTransition(0))
                {
                    animator.CrossFade(animState.ValueRO.WalkStateHash, 0.1f, 0);
                }

                animator.speed = 1f;
            }
            else
            {
                animator.speed = 0f;
            }
        }
    }
}

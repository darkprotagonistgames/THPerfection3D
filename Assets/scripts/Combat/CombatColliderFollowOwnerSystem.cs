using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

/// <summary>
/// Kinematic combat colliders (hitbox / hurtbox children) have their own physics bodies. Unity
/// Physics can integrate them independently of the character root, leaving colliders at stale
/// positions. Re-apply the baked local offset and zero velocity before each physics step.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
public partial struct CombatColliderFollowOwnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        var hitboxOwnerLookup = SystemAPI.GetComponentLookup<HitboxOwner>(true);
        var hurtboxOwnerLookup = SystemAPI.GetComponentLookup<HurtboxOwner>(true);

        new FollowOwnerJob
        {
            LtwLookup = ltwLookup,
            HitboxOwnerLookup = hitboxOwnerLookup,
            HurtboxOwnerLookup = hurtboxOwnerLookup
        }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct FollowOwnerJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
        [ReadOnly] public ComponentLookup<HitboxOwner> HitboxOwnerLookup;
        [ReadOnly] public ComponentLookup<HurtboxOwner> HurtboxOwnerLookup;

        public void Execute(
            Entity entity,
            ref LocalTransform localTransform,
            ref PhysicsVelocity physicsVelocity,
            in CombatColliderLocalOffset localOffset)
        {
            Entity owner = Entity.Null;
            if (HitboxOwnerLookup.HasComponent(entity))
                owner = HitboxOwnerLookup[entity].Value;
            else if (HurtboxOwnerLookup.HasComponent(entity))
                owner = HurtboxOwnerLookup[entity].Value;

            if (owner != Entity.Null && LtwLookup.HasComponent(owner))
            {
                LocalToWorld ownerLtw = LtwLookup[owner];
                
                // Calculate world position by transforming local offset by owner's matrix
                float3 worldPos = math.transform(ownerLtw.Value, localOffset.LocalPosition);
                // Calculate world rotation
                quaternion worldRot = math.mul(ownerLtw.Rotation, localOffset.LocalRotation);

                localTransform = LocalTransform.FromPositionRotationScale(
                    worldPos,
                    worldRot,
                    localOffset.LocalScale);
            }
            else
            {
                // Fallback to local offset if owner is missing (should be rare)
                localTransform = LocalTransform.FromPositionRotationScale(
                    localOffset.LocalPosition,
                    localOffset.LocalRotation,
                    localOffset.LocalScale);
            }

            physicsVelocity = new PhysicsVelocity();
        }
    }
}

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>Hurtbox snapshot used by cone and targeted attack systems.</summary>
public struct CombatAttackTargetCandidate
{
    public float2 PositionXZ;
    public Entity Owner;
    public uint LayerMask;
}

/// <summary>Shared hurtbox targeting helpers for auto-attack systems.</summary>
public static class CombatAttackTargeting
{
    public static EntityQuery CreateHurtboxTargetQuery(ref SystemState state)
    {
        return state.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<HurtboxOwner>(),
            ComponentType.ReadOnly<PhysicsCollider>(),
            ComponentType.ReadOnly<HurtboxData>());
    }

    public static void CollectHurtboxTargets(EntityQuery query, NativeList<CombatAttackTargetCandidate> targets)
    {
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var owners = query.ToComponentDataArray<HurtboxOwner>(Allocator.Temp);
        var colliders = query.ToComponentDataArray<PhysicsCollider>(Allocator.Temp);

        try
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                CollisionFilter filter = colliders[i].Value.Value.GetCollisionFilter();
                if (filter.BelongsTo == 0)
                    continue;

                targets.Add(new CombatAttackTargetCandidate
                {
                    PositionXZ = TopDownPlane.FromPosition(transforms[i].Position),
                    Owner = owners[i].Value,
                    LayerMask = filter.BelongsTo,
                });
            }
        }
        finally
        {
            transforms.Dispose();
            owners.Dispose();
            colliders.Dispose();
        }
    }

    public static bool IsValidCandidate(
        in CombatAttackTargetCandidate candidate,
        Entity spawnerEntity,
        uint targetLayerMask)
    {
        if (targetLayerMask == 0)
            return false;

        if (candidate.Owner == spawnerEntity)
            return false;

        return (targetLayerMask & candidate.LayerMask) != 0;
    }

    public static bool TryFindClosestTarget(
        in LocalTransform spawnerTransform,
        Entity spawnerEntity,
        in NativeList<CombatAttackTargetCandidate> targets,
        uint targetLayerMask,
        float range,
        out Entity closestTarget)
    {
        closestTarget = Entity.Null;

        if (targetLayerMask == 0 || range <= 0f)
            return false;

        float2 spawnerPos = TopDownPlane.FromPosition(spawnerTransform.Position);
        float rangeSq = range * range;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < targets.Length; i++)
        {
            CombatAttackTargetCandidate candidate = targets[i];
            if (!IsValidCandidate(in candidate, spawnerEntity, targetLayerMask))
                continue;

            float2 toTarget = candidate.PositionXZ - spawnerPos;
            float distSq = math.lengthsq(toTarget);
            if (distSq > rangeSq || distSq < 1e-8f)
                continue;

            if (distSq >= bestDistSq)
                continue;

            bestDistSq = distSq;
            closestTarget = candidate.Owner;
        }

        return closestTarget != Entity.Null;
    }

    public static bool TryFindConeTarget(
        in LocalTransform spawnerTransform,
        Entity spawnerEntity,
        in NativeList<CombatAttackTargetCandidate> targets,
        uint targetLayerMask,
        float range,
        float halfAngleRadians,
        out Entity _)
    {
        _ = Entity.Null;

        if (targetLayerMask == 0 || range <= 0f)
            return false;

        float2 spawnerPos = TopDownPlane.FromPosition(spawnerTransform.Position);
        float rangeSq = range * range;
        float minDot = math.cos(halfAngleRadians);
        float2 forwardXZ = TopDownPlane.ForwardFromRotation(spawnerTransform.Rotation);

        for (int i = 0; i < targets.Length; i++)
        {
            CombatAttackTargetCandidate candidate = targets[i];
            if (!IsValidCandidate(in candidate, spawnerEntity, targetLayerMask))
                continue;

            float2 toTarget = candidate.PositionXZ - spawnerPos;
            float distSq = math.lengthsq(toTarget);
            if (distSq > rangeSq || distSq < 1e-8f)
                continue;

            float2 dir = math.normalize(toTarget);
            if (math.dot(forwardXZ, dir) < minDot)
                continue;

            return true;
        }

        return false;
    }
}

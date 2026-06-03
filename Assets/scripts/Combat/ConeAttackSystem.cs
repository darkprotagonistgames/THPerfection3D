using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// When an entity's cone-attack cooldown has elapsed, scans hurtboxes on matching physics layers in
/// range and forward cone; on the first match, instantiates <see cref="ConeAttackData.ConeAttackPrefab"/>
/// at the spawner's <see cref="LocalTransform"/> with no ongoing link to the spawner.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct ConeAttackSystem : ISystem
{
    private struct TargetCandidate
    {
        public float2 PositionXZ;
        public Entity Owner;
        public uint LayerMask;
    }

    private NativeList<Entity> _spawnedThisFrame;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConeAttackData>();
        _spawnedThisFrame = new NativeList<Entity>(8, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_spawnedThisFrame.IsCreated)
            _spawnedThisFrame.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var targets = new NativeList<TargetCandidate>(Allocator.Temp);

        foreach (var (transform, owner, physicsCollider) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<HurtboxOwner>, RefRO<PhysicsCollider>>()
                     .WithAll<HurtboxData>())
        {
            CollisionFilter filter = physicsCollider.ValueRO.Value.Value.GetCollisionFilter();
            if (filter.BelongsTo == 0)
                continue;

            targets.Add(new TargetCandidate
            {
                PositionXZ = TopDownPlane.FromPosition(transform.ValueRO.Position),
                Owner = owner.ValueRO.Value,
                LayerMask = filter.BelongsTo,
            });
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        float deltaTime = SystemAPI.Time.DeltaTime;
        _spawnedThisFrame.Clear();

        foreach (var (coneAttack, transform, entity) in SystemAPI
                     .Query<RefRW<ConeAttackData>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            ref ConeAttackData data = ref coneAttack.ValueRW;

            if (data.CooldownRemaining > 0f)
            {
                data.CooldownRemaining = math.max(0f, data.CooldownRemaining - deltaTime);
                continue;
            }

            if (data.ConeAttackPrefab == Entity.Null || data.Range <= 0f)
                continue;

            if (!TryFindTarget(
                    in transform.ValueRO,
                    entity,
                    in data,
                    in targets,
                    out _))
                continue;

            Entity spawned = ecb.Instantiate(data.ConeAttackPrefab);
            ecb.SetComponent(spawned, transform.ValueRO);
            _spawnedThisFrame.Add(spawned);
            data.CooldownRemaining = data.Cooldown;
        }

        ecb.Playback(state.EntityManager);

        if (_spawnedThisFrame.Length > 0
            && SystemAPI.TryGetSingletonEntity<PlayerMovementData>(out Entity playerEntity))
        {
            var em = state.EntityManager;
            for (int i = 0; i < _spawnedThisFrame.Length; i++)
            {
                Entity spawned = _spawnedThisFrame[i];
                AssignHitboxOwners(em, spawned, playerEntity);
                if (!em.HasComponent<SnapToPlayerTag>(spawned))
                    em.AddComponent<SnapToPlayerTag>(spawned);
            }
        }

        ecb.Dispose();
        targets.Dispose();
    }

    static void AssignHitboxOwners(EntityManager entityManager, Entity prefabRoot, Entity owner)
    {
        if (!entityManager.HasBuffer<LinkedEntityGroup>(prefabRoot))
        {
            if (entityManager.HasComponent<HitboxData>(prefabRoot))
                entityManager.SetComponentData(prefabRoot, new HitboxOwner { Value = owner });
            return;
        }

        DynamicBuffer<LinkedEntityGroup> linked = entityManager.GetBuffer<LinkedEntityGroup>(prefabRoot);
        for (int i = 0; i < linked.Length; i++)
        {
            Entity member = linked[i].Value;
            if (entityManager.HasComponent<HitboxData>(member))
                entityManager.SetComponentData(member, new HitboxOwner { Value = owner });
        }
    }

    static bool TryFindTarget(
        in LocalTransform spawnerTransform,
        Entity spawnerEntity,
        in ConeAttackData data,
        in NativeList<TargetCandidate> targets,
        out Entity _)
    {
        _ = Entity.Null;

        if (data.TargetLayerMask == 0)
            return false;

        float2 spawnerPos = TopDownPlane.FromPosition(spawnerTransform.Position);
        float rangeSq = data.Range * data.Range;
        float minDot = math.cos(data.HalfAngleRadians);

        float2 forwardXZ = TopDownPlane.ForwardFromRotation(spawnerTransform.Rotation);

        for (int i = 0; i < targets.Length; i++)
        {
            TargetCandidate target = targets[i];

            if (target.Owner == spawnerEntity)
                continue;

            if ((data.TargetLayerMask & target.LayerMask) == 0)
                continue;

            float2 toTarget = target.PositionXZ - spawnerPos;
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

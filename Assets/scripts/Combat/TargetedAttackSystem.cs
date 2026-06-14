using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// When an entity's targeted-attack cooldown has elapsed, finds the closest hurtbox on matching physics
/// layers within range and instantiates <see cref="TargetedAttackData.AttackPrefab"/> at the spawner's
/// <see cref="LocalTransform"/> with <see cref="AttackSpawnContext"/> origin and target set.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct TargetedAttackSystem : ISystem
{
    private EntityQuery _hurtboxTargetQuery;

    private struct PendingSpawn
    {
        public Entity Prefab;
        public LocalTransform Transform;
        public Entity GroupOwner;
        public Entity OriginEntity;
        public Entity TargetEntity;
    }

    public void OnCreate(ref SystemState state)
    {
        _hurtboxTargetQuery = CombatAttackTargeting.CreateHurtboxTargetQuery(ref state);
        state.RequireForUpdate<TargetedAttackData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var targets = new NativeList<CombatAttackTargetCandidate>(Allocator.Temp);
        CombatAttackTargeting.CollectHurtboxTargets(_hurtboxTargetQuery, targets);

        var pending = new NativeList<PendingSpawn>(4, Allocator.Temp);
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (targetedAttack, transform, entity) in SystemAPI
                     .Query<RefRW<TargetedAttackData>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            ref TargetedAttackData data = ref targetedAttack.ValueRW;

            if (data.CooldownRemaining > 0f)
            {
                data.CooldownRemaining = math.max(0f, data.CooldownRemaining - deltaTime);
                continue;
            }

            if (data.AttackPrefab == Entity.Null || data.Range <= 0f)
                continue;

            if (!CombatAttackTargeting.TryFindClosestTarget(
                    in transform.ValueRO,
                    entity,
                    in targets,
                    data.TargetLayerMask,
                    data.Range,
                    out Entity targetEntity))
                continue;

            pending.Add(new PendingSpawn
            {
                Prefab = data.AttackPrefab,
                Transform = transform.ValueRO,
                GroupOwner = entity,
                OriginEntity = entity,
                TargetEntity = targetEntity,
            });
            data.CooldownRemaining = data.Cooldown;
        }

        if (pending.Length > 0)
        {
            var em = state.EntityManager;
            for (int i = 0; i < pending.Length; i++)
            {
                PendingSpawn spawn = pending[i];
                LinkedEntityGroupUtility.InstantiateAsSpawnChildWithContext(
                    em,
                    spawn.GroupOwner,
                    spawn.Prefab,
                    spawn.Transform,
                    spawn.OriginEntity,
                    spawn.TargetEntity);
            }
        }

        pending.Dispose();
        targets.Dispose();
    }
}

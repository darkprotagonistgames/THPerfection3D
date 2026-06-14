using THPerfection.GeneratedEvents;
using Unity.Entities;

/// <summary>
/// Reads <see cref="AttackSpawnContext"/> on entities with <see cref="DirectDamageData"/>,
/// emits a <see cref="damageEvent"/> to the target, then destroys the direct-damage entity.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TargetedAttackSystem))]
[UpdateBefore(typeof(EnableAllEcsEventsSystem))]
public partial class DirectDamageSystem : SystemBase
{
    EntityQuery _hurtboxQuery;

    protected override void OnCreate()
    {
        _hurtboxQuery = GetEntityQuery(typeof(HurtboxData), typeof(HurtboxOwner));
        RequireForUpdate<DirectDamageData>();
    }

    protected override void OnUpdate()
    {
        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(World.Unmanaged);

        var invulnerableLookup = SystemAPI.GetComponentLookup<SpawnInvulnerabilityTag>(true);
        invulnerableLookup.Update(this);

        foreach (var (directDamage, context, entity) in SystemAPI
                     .Query<RefRO<DirectDamageData>, RefRO<AttackSpawnContext>>()
                     .WithEntityAccess())
        {
            Entity victim = context.ValueRO.TargetEntity;
            Entity sender = context.ValueRO.OriginEntity;

            if (victim == Entity.Null
                || (sender != Entity.Null && victim == sender)
                || invulnerableLookup.HasComponent(victim))
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            targetable category = CombatTargetableUtility.ResolveCategory(EntityManager, _hurtboxQuery, victim);
            victim.CreatedamageEvent(
                sender,
                ecb,
                directDamage.ValueRO.Damage,
                directDamage.ValueRO.WeaponType,
                category);

            ecb.DestroyEntity(entity);
        }
    }
}

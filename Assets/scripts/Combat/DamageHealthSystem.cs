using THPerfection.GeneratedEvents;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Applies enabled <see cref="damageEvent"/> amounts to the victim's <see cref="Health"/>.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnableAllEcsEventsSystem))]
[UpdateBefore(typeof(DeathFromHealthSystem))]
[UpdateBefore(typeof(CleanupAllEcsEventsSystem))]
public partial class DamageHealthSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var healthLookup = SystemAPI.GetComponentLookup<Health>();
        healthLookup.Update(this);

        foreach (var ev in SystemAPI.Query<RefRO<damageEvent>>())
        {
            if (!ev.ValueRO.Enabled)
                continue;

            Entity victim = ev.ValueRO.Sender;
            if (!healthLookup.HasComponent(victim))
                continue;

            float damage = ev.ValueRO.amount;
            RefRW<Health> health = healthLookup.GetRefRW(victim);
            health.ValueRW.Value -= damage;

            Debug.Log(
                $"Damage victim={victim.Index} amount={damage} health={health.ValueRO.Value} weapon={ev.ValueRO.sourceType} target={ev.ValueRO.targetType}");
        }
    }
}

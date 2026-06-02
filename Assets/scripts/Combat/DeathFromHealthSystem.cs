using THPerfection.GeneratedEvents;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Emits <see cref="deathEvent"/> when a damage victim's <see cref="Health"/> reaches zero or below.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(DamageHealthSystem))]
[UpdateBefore(typeof(CleanupAllEcsEventsSystem))]
public partial class DeathFromHealthSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var healthLookup = SystemAPI.GetComponentLookup<Health>(true);
        healthLookup.Update(this);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var ev in SystemAPI.Query<RefRO<damageEvent>>())
        {
            if (!ev.ValueRO.Enabled)
                continue;

            Entity victim = ev.ValueRO.Victim;
            if (!healthLookup.HasComponent(victim))
                continue;

            if (healthLookup[victim].Value > 0f)
                continue;

            victim.CreatedeathEvent(ecb);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

using THPerfection.GeneratedEvents;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Handles enabled <see cref="deathEvent"/> instances: logs the victim and destroys its entity.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnableAllEcsEventsSystem))]
[UpdateBefore(typeof(CleanupAllEcsEventsSystem))]
public partial class DeathSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var ev in SystemAPI.Query<RefRO<deathEvent>>())
        {
            if (!ev.ValueRO.Enabled)
                continue;

            Entity dead = ev.ValueRO.Sender;
            Debug.Log($"Death entity={dead.Index}");

            if (EntityManager.Exists(dead))
                ecb.DestroyEntity(dead);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

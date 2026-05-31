using THPerfection.GeneratedEvents;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Logs enabled damageEvent instances for Play Mode validation (v1 — no health).
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnableAllEcsEventsSystem))]
[UpdateBefore(typeof(CleanupAllEcsEventsSystem))]
public partial class DamageEventLoggerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var ev in SystemAPI.Query<RefRO<damageEvent>>())
        {
            if (!ev.ValueRO.Enabled)
                continue;

            Debug.Log(
                $"Damage victim={ev.ValueRO.Sender.Index} amount={ev.ValueRO.amount} weapon={ev.ValueRO.sourceType} target={ev.ValueRO.targetType}");
        }
    }
}

using THPerfection.GeneratedEvents;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Listens for jumpEvent entities that have been enabled this frame and logs "Jump" to the console.
/// Runs after EnableAllEcsEventsSystem so events are guaranteed to be active before being read.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnableAllEcsEventsSystem))]
[UpdateBefore(typeof(CleanupAllEcsEventsSystem))]
public partial class JumpEventLoggerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithAll<jumpEvent>()
            .ForEach((in jumpEvent ev) =>
            {
                if (ev.Enabled)
                {
                    Debug.Log("Jump");
                }
            })
            .WithoutBurst()
            .Run();
    }
}

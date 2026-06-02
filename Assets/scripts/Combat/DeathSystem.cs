using THPerfection.GeneratedEvents;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Handles enabled <see cref="deathEvent"/> instances: detaches any descendant tagged with
/// <see cref="KeepAfterDeathTag"/> in world space, then destroys the character root hierarchy.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnableAllEcsEventsSystem))]
[UpdateBefore(typeof(CleanupAllEcsEventsSystem))]
public partial class DeathSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var keepTagLookup = SystemAPI.GetComponentLookup<KeepAfterDeathTag>(true);
        var childLookup = SystemAPI.GetBufferLookup<Child>(true);
        keepTagLookup.Update(this);
        childLookup.Update(this);

        foreach (var ev in SystemAPI.Query<RefRO<deathEvent>>())
        {
            if (!ev.ValueRO.Enabled)
                continue;

            Entity dead = ev.ValueRO.Sender;
            if (!EntityManager.Exists(dead))
                continue;

            Entity keepEntity = DeathHierarchyUtility.FindKeepAfterDeathDescendant(
                dead, EntityManager, keepTagLookup, childLookup);

            if (keepEntity != Entity.Null && EntityManager.Exists(keepEntity))
                Debug.Log($"Death entity={dead.Index} kept visual entity={keepEntity.Index}");
            else
                Debug.Log($"Death entity={dead.Index} (no KeepAfterDeath descendant)");

            DeathHierarchyUtility.PreserveVisualAndDestroyCharacter(dead, keepEntity, EntityManager, ecb);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

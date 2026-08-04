using Unity.Entities;

/// <summary>
/// Disables <see cref="BuildingLayoutChanged"/> after consumers have read it this frame.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
public partial struct BuildingLayoutChangedCleanupSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BuildingLayoutChanged>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<BuildingLayoutChanged>>()
                     .WithAll<BuildingLayoutSingletonTag>()
                     .WithEntityAccess())
        {
            state.EntityManager.SetComponentEnabled<BuildingLayoutChanged>(entity, false);
        }
    }
}

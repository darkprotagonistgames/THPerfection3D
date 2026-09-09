using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Disables <see cref="GridCellChanged"/> / <see cref="RoomChanged"/> after consumers have read them this frame.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
public partial struct SpatialOccupancyCleanupSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TracksSpatialOccupancy>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<GridCellChanged>>()
                     .WithAll<TracksSpatialOccupancy>()
                     .WithEntityAccess())
        {
            ecb.SetComponentEnabled<GridCellChanged>(entity, false);
        }

        foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<RoomChanged>>()
                     .WithAll<TracksSpatialOccupancy>()
                     .WithEntityAccess())
        {
            ecb.SetComponentEnabled<RoomChanged>(entity, false);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

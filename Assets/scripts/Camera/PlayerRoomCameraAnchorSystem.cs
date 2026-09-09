using THPerfection.GeneratedEvents;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Enables room camera anchors whose <see cref="RoomCameraAnchor.RoomInstanceId"/> matches
/// the player's current room; disables all others.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnableAllEcsEventsSystem))]
[UpdateBefore(typeof(CleanupAllEcsEventsSystem))]
public partial struct PlayerRoomCameraAnchorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RoomCameraAnchor>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var hasRoomChange = false;
        var roomId = 0;

        foreach (var ev in SystemAPI.Query<RefRO<playerRoomChangedEvent>>())
        {
            if (!ev.ValueRO.Enabled)
                continue;

            hasRoomChange = true;
            roomId = ev.ValueRO.currentRoomId;
        }

        if (!hasRoomChange)
            return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (anchor, entity) in SystemAPI
                     .Query<RefRO<RoomCameraAnchor>>()
                     .WithAll<CameraAnchor>()
                     .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                     .WithEntityAccess())
        {
            bool enable = roomId != 0 && anchor.ValueRO.RoomInstanceId == roomId;
            ecb.SetComponentEnabled<CameraAnchor>(entity, enable);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

using Unity.Entities;

/// <summary>
/// Cached room instance for an entity with <see cref="TracksSpatialOccupancy"/>.
/// <see cref="RoomInstanceId"/> is 0 when outside any stamped room cell.
/// </summary>
public struct RoomLocation : IComponentData
{
    public int RoomInstanceId;
}

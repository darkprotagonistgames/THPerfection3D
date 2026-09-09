using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Room-prefab camera anchor. <see cref="RoomInstanceId"/> is stamped at spawn.
/// <see cref="PreferredWorldRotation"/> is the authoring pose at room R0; runtime local
/// rotation is countered against the room yaw so world "north" stays fixed.
/// </summary>
public struct RoomCameraAnchor : IComponentData
{
    public int RoomInstanceId;
    public quaternion PreferredWorldRotation;
}

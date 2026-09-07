using Unity.Entities;

/// <summary>
/// Enabled for one frame when an entity's room instance changes.
/// </summary>
public struct RoomChanged : IComponentData, IEnableableComponent
{
    public int PreviousRoomInstanceId;
    public int CurrentRoomInstanceId;
}

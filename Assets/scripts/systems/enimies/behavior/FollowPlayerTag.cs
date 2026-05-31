using Unity.Entities;

/// <summary>
/// Zero-size tag component. Add this to any entity that should continuously
/// track and move toward the player. The <see cref="FollowPlayerSystem"/>
/// will keep <see cref="MoveToTarget"/> up to date each frame.
/// </summary>
public struct FollowPlayerTag : IComponentData { }

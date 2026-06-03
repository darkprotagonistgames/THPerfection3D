using Unity.Entities;

/// <summary>
/// Zero-size tag. Entities with this component have their <see cref="Unity.Transforms.LocalTransform"/>
/// overwritten each frame by <see cref="SnapToPlayerSystem"/> to match the player entity.
/// </summary>
public struct SnapToPlayerTag : IComponentData { }

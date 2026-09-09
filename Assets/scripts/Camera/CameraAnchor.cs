using Unity.Entities;

/// <summary>
/// Marks a camera anchor entity whose <see cref="Unity.Transforms.LocalTransform"/>
/// defines a fixed camera pose. Enableable: room anchors start disabled and are
/// enabled only while the player is in that room instance.
/// </summary>
public struct CameraAnchor : IComponentData, IEnableableComponent
{
}

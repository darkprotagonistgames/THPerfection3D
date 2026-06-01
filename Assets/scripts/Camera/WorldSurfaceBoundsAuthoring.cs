using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Bakes a singleton <see cref="WorldSurfaceBounds"/> entity for camera anchors and other world-scale systems.
/// </summary>
public class WorldSurfaceBoundsAuthoring : MonoBehaviour
{
    [Tooltip("World extent along +X from the origin.")]
    public float Width = 20000f;

    [Tooltip("World extent along +Z from the origin.")]
    public float Depth = 20000f;

    public class Baker : Baker<WorldSurfaceBoundsAuthoring>
    {
        public override void Bake(WorldSurfaceBoundsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new WorldSurfaceBounds
            {
                Width = math.max(1f, authoring.Width),
                Depth = math.max(1f, authoring.Depth),
            });
        }
    }
}

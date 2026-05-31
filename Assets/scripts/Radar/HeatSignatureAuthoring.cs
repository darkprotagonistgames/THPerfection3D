using Unity.Entities;
using UnityEngine;

/// <summary>
/// Baked component that marks an entity as a heat source on the radar.
/// </summary>
public struct HeatSignatureData : IComponentData
{
    /// <summary>Which radar category (or categories) this entity belongs to.</summary>
    public HeatSignatureType SignatureType;

    /// <summary>Total heat units to scatter across the grid each frame.</summary>
    public int Heat;

    /// <summary>Half-size of the random scatter square in pixels.</summary>
    public int Spread;
}

/// <summary>
/// Authoring component that bakes <see cref="HeatSignatureData"/> onto any ECS entity.
/// Place on GameObjects inside a sub-scene that should appear on the radar heatmap.
/// </summary>
public class HeatSignatureAuthoring : MonoBehaviour
{
    [Tooltip("Which radar category (or categories) this entity emits heat for.")]
    public HeatSignatureType SignatureType = HeatSignatureType.Monster;

    [Tooltip("Total heat budget distributed across the scatter square each frame.")]
    public int Heat = 100;

    [Tooltip("Half-side of the random scatter square in pixels.")]
    public int Spread = 5;

    public class Baker : Baker<HeatSignatureAuthoring>
    {
        public override void Bake(HeatSignatureAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new HeatSignatureData
            {
                SignatureType = authoring.SignatureType,
                Heat          = authoring.Heat,
                Spread        = authoring.Spread,
            });
        }
    }
}

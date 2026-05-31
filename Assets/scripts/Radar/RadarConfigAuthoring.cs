using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Singleton ECS component that configures the radar renderer at runtime.
/// Baked by <see cref="RadarConfigAuthoring"/>.
/// </summary>
public struct RadarRendererConfig : IComponentData
{
    /// <summary>Bitmask of <see cref="HeatSignatureType"/> values to include in the heatmap.</summary>
    public HeatSignatureType FilterFlags;

    /// <summary>Width and height of the radar texture in pixels.</summary>
    public int MapSizePixels;

    /// <summary>R channel of the hottest-pixel color.</summary>
    public float TargetColorR;

    /// <summary>G channel of the hottest-pixel color.</summary>
    public float TargetColorG;

    /// <summary>B channel of the hottest-pixel color.</summary>
    public float TargetColorB;

    /// <summary>World XZ center mapped to the middle of the radar texture.</summary>
    public float2 WorldCenterXZ;

    /// <summary>
    /// Half-size of the square world region shown on radar.
    /// Positions in [center - extent, center + extent] on X and Z fill the texture.
    /// </summary>
    public float WorldHalfExtent;
}

/// <summary>
/// Static bridge that lets the managed <see cref="RadarRendererSystem"/> hand off its
/// <see cref="Texture2D"/> to MonoBehaviour-land without coupling the two directly.
/// </summary>
public static class RadarTextureHolder
{
    /// <summary>
    /// Written once by <see cref="RadarRendererSystem.OnCreate"/>;
    /// read by <see cref="RadarUIBridge"/> in its <c>Update</c>.
    /// </summary>
    public static Texture2D Texture;
}

/// <summary>
/// Authoring component that bakes a singleton <see cref="RadarRendererConfig"/> entity.
/// Add this to one GameObject inside a sub-scene (e.g. the config entity in sampleSub).
/// </summary>
public class RadarConfigAuthoring : MonoBehaviour
{
    [Tooltip("Width and height of the radar texture in pixels.")]
    public int MapSizePixels = 512;

    [Tooltip("Color used for the hottest pixels on the radar. Default: green.")]
    public Color TargetColor = Color.green;

    [Tooltip("Which heat-signature types are rendered. Default: everything.")]
    public HeatSignatureType FilterFlags = HeatSignatureType.Monster | HeatSignatureType.Treasure;

    [Tooltip("World XZ center shown at the middle of the radar.")]
    public Vector2 WorldCenterXZ = Vector2.zero;

    [Tooltip("Half-size of the square region mapped to the radar (world units on X and Z).")]
    public float WorldHalfExtent = 60f;

    public class Baker : Baker<RadarConfigAuthoring>
    {
        public override void Bake(RadarConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new RadarRendererConfig
            {
                FilterFlags     = authoring.FilterFlags,
                MapSizePixels   = math.max(1, authoring.MapSizePixels),
                TargetColorR    = authoring.TargetColor.r,
                TargetColorG    = authoring.TargetColor.g,
                TargetColorB    = authoring.TargetColor.b,
                WorldCenterXZ   = authoring.WorldCenterXZ,
                WorldHalfExtent = math.max(1f, authoring.WorldHalfExtent),
            });
        }
    }
}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Bakes <see cref="CameraAnchorGridConfig"/>; anchors are spawned at runtime by
/// <see cref="CameraAnchorSpawnSystem"/> (same pattern as zombie spawners).
/// </summary>
[DisallowMultipleComponent]
public class CameraAnchorGridAuthoring : MonoBehaviour
{
    [Header("Anchor Pose")]
    [Tooltip("World Y height for every camera anchor.")]
    public float AnchorY = 250f;

    [Tooltip("World rotation applied to every camera anchor.")]
    public Vector3 AnchorEulerAngles = new(55f, 0f, 0f);

    [Header("Coverage")]
    [Tooltip("Vertical field of view used to estimate ground coverage per anchor.")]
    [Range(1f, 179f)]
    public float VerticalFovDegrees = 60f;

    [Tooltip("Aspect ratio (width / height) used for coverage estimation.")]
    public float Aspect = 16f / 9f;

    [Tooltip("Overlap between adjacent anchor footprints (1 = touching, lower = more overlap).")]
    [Range(0.5f, 1f)]
    public float OverlapFactor = 0.85f;

    [Header("World Size")]
    [Tooltip("Optional override. When unset, reads WorldSurfaceBoundsAuthoring on this GameObject.")]
    public bool OverrideWorldSize;

    public float WorldWidth = 20000f;
    public float WorldDepth = 20000f;

    public class Baker : Baker<CameraAnchorGridAuthoring>
    {
        public override void Bake(CameraAnchorGridAuthoring authoring)
        {
            DependsOn(authoring);

            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new CameraAnchorGridConfig
            {
                AnchorY = authoring.AnchorY,
                AnchorEulerDegrees = (float3)authoring.AnchorEulerAngles,
                VerticalFovDegrees = authoring.VerticalFovDegrees,
                Aspect = math.max(0.01f, authoring.Aspect),
                OverlapFactor = authoring.OverlapFactor,
                WorldWidth = ResolveWorldWidth(authoring),
                WorldDepth = ResolveWorldDepth(authoring),
            });
        }

        static float ResolveWorldWidth(CameraAnchorGridAuthoring authoring)
        {
            if (authoring.OverrideWorldSize)
                return math.max(1f, authoring.WorldWidth);

            var boundsAuthoring = authoring.GetComponent<WorldSurfaceBoundsAuthoring>();
            return boundsAuthoring != null
                ? math.max(1f, boundsAuthoring.Width)
                : math.max(1f, authoring.WorldWidth);
        }

        static float ResolveWorldDepth(CameraAnchorGridAuthoring authoring)
        {
            if (authoring.OverrideWorldSize)
                return math.max(1f, authoring.WorldDepth);

            var boundsAuthoring = authoring.GetComponent<WorldSurfaceBoundsAuthoring>();
            return boundsAuthoring != null
                ? math.max(1f, boundsAuthoring.Depth)
                : math.max(1f, authoring.WorldDepth);
        }
    }
}

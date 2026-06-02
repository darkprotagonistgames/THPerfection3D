using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Runtime config for spawning the camera anchor grid (baked from <see cref="CameraAnchorGridAuthoring"/>).
/// </summary>
public struct CameraAnchorGridConfig : IComponentData
{
    public float AnchorY;
    public float3 AnchorEulerDegrees;
    public float VerticalFovDegrees;
    public float Aspect;
    public float OverlapFactor;
    public float WorldWidth;
    public float WorldDepth;
}

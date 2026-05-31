using Unity.Mathematics;

/// <summary>
/// Helpers for top-down 3D gameplay on the XZ plane (Y is up).
/// </summary>
public static class TopDownPlane
{
    public static float2 FromPosition(float3 position) => new(position.x, position.z);

    public static float3 ToLinear(float2 velocityXZ, float currentY) =>
        new(velocityXZ.x, currentY, velocityXZ.y);

    public static float3 ToPosition(float2 positionXZ, float groundY) =>
        new(positionXZ.x, groundY, positionXZ.y);
}

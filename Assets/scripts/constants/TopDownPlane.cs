using Unity.Mathematics;

/// <summary>
/// Helpers for top-down 3D gameplay on the XZ plane (Y is up).
/// </summary>
public static class TopDownPlane
{
    /// <summary>
    /// World-space forward for rigged models that face the camera (-Z), not Unity's default +Z.
    /// </summary>
    public static readonly float3 ModelForward = new(0f, 0f, -1f);

    /// <summary><see cref="ModelForward"/> projected onto the XZ plane (unnormalized).</summary>
    public static readonly float2 ModelForwardXZ = new(0f, -1f);

    public static float2 FromPosition(float3 position) => new(position.x, position.z);

    public static float2 ForwardFromRotation(quaternion rotation)
    {
        float3 forward = math.mul(rotation, ModelForward);
        float2 forwardXZ = new(forward.x, forward.z);
        if (math.lengthsq(forwardXZ) < 1e-8f)
            return ModelForwardXZ;

        return math.normalize(forwardXZ);
    }

    public static float3 ToLinear(float2 velocityXZ, float currentY) =>
        new(velocityXZ.x, currentY, velocityXZ.y);

    public static float3 ToPosition(float2 positionXZ, float groundY) =>
        new(positionXZ.x, groundY, positionXZ.y);
}

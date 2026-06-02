using Unity.Mathematics;

/// <summary>
/// Computes how much ground (y = 0) a perspective camera can see from a given anchor pose.
/// Used at bake time to choose grid density so the whole world stays in view.
/// </summary>
public static class CameraAnchorCoverage
{
    /// <summary>
    /// Half-extents of the axis-aligned ground footprint visible from the anchor pose.
    /// Returns fallback half-size when the frustum does not intersect y = 0.
    /// </summary>
    public static float2 ComputeGroundHalfExtents(
        float3 anchorPosition,
        quaternion anchorRotation,
        float verticalFovDegrees,
        float aspect,
        float fallbackHalfSize = 100f)
    {
        float fovRad = math.radians(verticalFovDegrees);
        float tanHalfV = math.tan(fovRad * 0.5f);
        float tanHalfH = tanHalfV * aspect;

        float3 forward = math.forward(anchorRotation);
        float3 right = math.mul(anchorRotation, math.right());
        float3 up = math.mul(anchorRotation, math.up());

        float2 minXZ = new float2(float.MaxValue);
        float2 maxXZ = new float2(float.MinValue);
        var hitCount = 0;

        for (var sy = -1; sy <= 1; sy += 2)
        {
            for (var sx = -1; sx <= 1; sx += 2)
            {
                float3 direction = forward + right * (sx * tanHalfH) + up * (sy * tanHalfV);
                direction = math.normalize(direction);

                if (!TryIntersectGroundPlane(anchorPosition, direction, out float3 hit))
                    continue;

                float2 xz = hit.xz;
                minXZ = math.min(minXZ, xz);
                maxXZ = math.max(maxXZ, xz);
                hitCount++;
            }
        }

        if (hitCount == 0)
            return new float2(fallbackHalfSize);

        return (maxXZ - minXZ) * 0.5f;
    }

    /// <summary>
    /// Grid counts that cover <paramref name="worldWidth"/> x <paramref name="worldDepth"/>
    /// with overlapping anchor footprints.
    /// </summary>
    public static int2 ComputeGridCounts(
        float worldWidth,
        float worldDepth,
        float3 sampleAnchorPosition,
        quaternion anchorRotation,
        float verticalFovDegrees,
        float aspect,
        float overlapFactor = 0.85f)
    {
        float2 halfExtents = ComputeGroundHalfExtents(
            sampleAnchorPosition,
            anchorRotation,
            verticalFovDegrees,
            aspect);

        float spacingX = math.max(1f, halfExtents.x * 2f * overlapFactor);
        float spacingZ = math.max(1f, halfExtents.y * 2f * overlapFactor);

        return new int2(
            math.max(1, (int)math.ceil(worldWidth / spacingX)),
            math.max(1, (int)math.ceil(worldDepth / spacingZ)));
    }

    static bool TryIntersectGroundPlane(float3 origin, float3 direction, out float3 hit)
    {
        if (math.abs(direction.y) < 1e-5f)
        {
            hit = default;
            return false;
        }

        float t = -origin.y / direction.y;
        if (t <= 0f)
        {
            hit = default;
            return false;
        }

        hit = origin + direction * t;
        return true;
    }
}

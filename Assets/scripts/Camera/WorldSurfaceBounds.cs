using Unity.Entities;

/// <summary>
/// Singleton describing the playable flat world on the XZ plane.
/// Origin is (0, 0) on XZ; <see cref="Width"/> and <see cref="Depth"/> extend along +X and +Z.
/// </summary>
public struct WorldSurfaceBounds : IComponentData
{
    public float Width;
    public float Depth;
}

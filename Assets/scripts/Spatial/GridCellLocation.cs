using THPerfection.LevelGen;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Cached grid cell for an entity with <see cref="TracksSpatialOccupancy"/>.
/// </summary>
public struct GridCellLocation : IComponentData
{
    public FloorId Floor;
    public int2 Cell;
}

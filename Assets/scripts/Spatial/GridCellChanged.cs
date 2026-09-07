using THPerfection.LevelGen;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Enabled for one frame when an entity's grid cell changes.
/// </summary>
public struct GridCellChanged : IComponentData, IEnableableComponent
{
    public FloorId PreviousFloor;
    public int2 PreviousCell;
    public FloorId CurrentFloor;
    public int2 CurrentCell;
}

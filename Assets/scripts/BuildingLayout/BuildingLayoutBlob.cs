using THPerfection.LevelGen;
using Unity.Entities;
using Unity.Mathematics;

public struct BuildingLayoutCellBlob
{
    public FloorId Floor;
    public int CellX;
    public int CellY;
    public int RoomInstanceId;
    public DoorMask Doors;
}

public struct BuildingLayoutRoomBlob
{
    public int RoomInstanceId;
    public FloorId Floor;
    public int2 Origin;
    public Rotation90 Rotation;
}

public struct BuildingLayoutBlob
{
    public BlobArray<BuildingLayoutCellBlob> Cells;
    public BlobArray<BuildingLayoutRoomBlob> Rooms;
}

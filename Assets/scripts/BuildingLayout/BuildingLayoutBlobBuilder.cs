using System.Collections.Generic;
using THPerfection.LevelGen;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public static class BuildingLayoutBlobBuilder
{
    public static BlobAssetReference<BuildingLayoutBlob> Build(BuildingRunState state)
    {
        var cellEntries = new List<BuildingLayoutCellBlob>();
        var roomEntries = new List<BuildingLayoutRoomBlob>();

        if (state.Occupancy.TryGetFloor(state.Floor, out FloorGrid grid))
        {
            foreach (KeyValuePair<int2, OccupiedCell> entry in grid.Cells)
            {
                cellEntries.Add(new BuildingLayoutCellBlob
                {
                    Floor            = state.Floor,
                    CellX            = entry.Key.x,
                    CellY            = entry.Key.y,
                    RoomInstanceId   = entry.Value.RoomInstanceId,
                    Doors            = entry.Value.Doors,
                });
            }
        }

        foreach (RoomInstance instance in state.Instances)
        {
            roomEntries.Add(new BuildingLayoutRoomBlob
            {
                RoomInstanceId = instance.Id,
                Floor          = instance.Floor,
                Origin         = instance.Origin,
                Rotation       = instance.Rotation,
            });
        }

        cellEntries.Sort(CompareCells);
        roomEntries.Sort((a, b) => a.RoomInstanceId.CompareTo(b.RoomInstanceId));

        using var builder = new BlobBuilder(Allocator.Temp);
        ref BuildingLayoutBlob root = ref builder.ConstructRoot<BuildingLayoutBlob>();

        BlobBuilderArray<BuildingLayoutCellBlob> cellArray =
            builder.Allocate(ref root.Cells, cellEntries.Count);
        for (int i = 0; i < cellEntries.Count; i++)
            cellArray[i] = cellEntries[i];

        BlobBuilderArray<BuildingLayoutRoomBlob> roomArray =
            builder.Allocate(ref root.Rooms, roomEntries.Count);
        for (int i = 0; i < roomEntries.Count; i++)
            roomArray[i] = roomEntries[i];

        return builder.CreateBlobAssetReference<BuildingLayoutBlob>(Allocator.Persistent);
    }

    static int CompareCells(BuildingLayoutCellBlob a, BuildingLayoutCellBlob b)
    {
        int cmp = ((int)a.Floor).CompareTo((int)b.Floor);
        if (cmp != 0)
            return cmp;

        cmp = a.CellX.CompareTo(b.CellX);
        if (cmp != 0)
            return cmp;

        return a.CellY.CompareTo(b.CellY);
    }
}

using THPerfection.LevelGen;
using Unity.Mathematics;

public static class BuildingLayoutLookup
{
    public static int2 WorldToCell(float2 worldXZ, float cellSize)
    {
        float inv = 1f / math.max(0.01f, cellSize);
        return new int2(
            (int)math.floor(worldXZ.x * inv),
            (int)math.floor(worldXZ.y * inv));
    }

    public static bool TryGetCell(
        ref BuildingLayoutBlob layout,
        FloorId floor,
        int2 cell,
        out BuildingLayoutCellBlob result)
    {
        result = default;
        int length = layout.Cells.Length;
        if (length == 0)
            return false;

        int lo = 0;
        int hi = length - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            ref BuildingLayoutCellBlob entry = ref layout.Cells[mid];
            int cmp = CompareCell(in entry, floor, cell);

            if (cmp == 0)
            {
                result = entry;
                return true;
            }

            if (cmp < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return false;
    }

    public static bool TryGetRoom(
        ref BuildingLayoutBlob layout,
        int roomInstanceId,
        out BuildingLayoutRoomBlob result)
    {
        result = default;
        int length = layout.Rooms.Length;
        if (length == 0)
            return false;

        int lo = 0;
        int hi = length - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            ref BuildingLayoutRoomBlob entry = ref layout.Rooms[mid];
            int cmp = entry.RoomInstanceId.CompareTo(roomInstanceId);

            if (cmp == 0)
            {
                result = entry;
                return true;
            }

            if (cmp < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return false;
    }

    static int CompareCell(in BuildingLayoutCellBlob entry, FloorId floor, int2 cell)
    {
        int cmp = ((int)entry.Floor).CompareTo((int)floor);
        if (cmp != 0)
            return cmp;

        cmp = entry.CellX.CompareTo(cell.x);
        if (cmp != 0)
            return cmp;

        return entry.CellY.CompareTo(cell.y);
    }
}

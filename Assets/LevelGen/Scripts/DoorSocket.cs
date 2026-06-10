using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public readonly struct DoorSocket
    {
        public readonly int2 Cell;
        public readonly DoorSide Side;
        public readonly bool IsMainDoor;

        public DoorSocket(int2 cell, DoorSide side, bool isMainDoor = false)
        {
            Cell       = cell;
            Side       = side;
            IsMainDoor = isMainDoor;
        }
    }

    public readonly struct DoorwaySlot
    {
        public readonly FloorId Floor;
        public readonly int2 Cell;
        public readonly DoorSide Side;
        public readonly int FromRoomInstanceId;

        public DoorwaySlot(FloorId floor, int2 cell, DoorSide side, int fromRoomInstanceId = 0)
        {
            Floor                = floor;
            Cell                 = cell;
            Side                 = side;
            FromRoomInstanceId   = fromRoomInstanceId;
        }

        public int2 ExpansionCell => Cell + GridTransforms.Direction(Side);
    }
}

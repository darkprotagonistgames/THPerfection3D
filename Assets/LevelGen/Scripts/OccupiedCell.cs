namespace THPerfection.LevelGen
{
    public struct OccupiedCell
    {
        public int RoomInstanceId;
        public DoorMask Doors;

        public OccupiedCell(int roomInstanceId, DoorMask doors = DoorMask.None)
        {
            RoomInstanceId = roomInstanceId;
            Doors          = doors;
        }
    }
}

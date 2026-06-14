using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    [System.Serializable]
    public struct BuildingGenConfig
    {
        public uint Seed;
        public int TargetRoomCount;
        public int MaxAttemptsPerDoorway;
        public int MinOpenDoorwaysBeforeDeadEnd;
        public int MinPlacedRoomsBeforeDeadEnd;
        public int2 SeedOrigin;
        public float CellSize;
        public float FloorY;

        public static BuildingGenConfig Default => new()
        {
            Seed                   = 1,
            TargetRoomCount        = 12,
            MaxAttemptsPerDoorway       = 8,
            MinOpenDoorwaysBeforeDeadEnd  = 2,
            MinPlacedRoomsBeforeDeadEnd   = 3,
            SeedOrigin             = int2.zero,
            CellSize               = 4f,
            FloorY                 = 0f,
        };
    }
}

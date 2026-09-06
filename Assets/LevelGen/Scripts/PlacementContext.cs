namespace THPerfection.LevelGen
{
    public readonly struct PlacementContext
    {
        public readonly FloorId Floor;
        public readonly FloorGrid Grid;
        public readonly DoorwaySlot? TargetDoorway;
        public readonly int? OpenDoorwayCount;
        public readonly int? PlacedRoomCount;
        public readonly int? TargetRoomCount;
        public readonly int MinOpenDoorwaysBeforeDeadEnd;
        public readonly int MinPlacedRoomsBeforeDeadEnd;

        public PlacementContext(
            FloorId floor,
            FloorGrid grid,
            DoorwaySlot? targetDoorway = null,
            int? openDoorwayCount = null,
            int? placedRoomCount = null,
            int? targetRoomCount = null,
            int minOpenDoorwaysBeforeDeadEnd = 2,
            int minPlacedRoomsBeforeDeadEnd = 3)
        {
            Floor                          = floor;
            Grid                           = grid;
            TargetDoorway                  = targetDoorway;
            OpenDoorwayCount               = openDoorwayCount;
            PlacedRoomCount                = placedRoomCount;
            TargetRoomCount                = targetRoomCount;
            MinOpenDoorwaysBeforeDeadEnd   = minOpenDoorwaysBeforeDeadEnd;
            MinPlacedRoomsBeforeDeadEnd      = minPlacedRoomsBeforeDeadEnd;
        }

        public bool HasTargetDoorway => TargetDoorway.HasValue;

        public bool RequiresExpansionDoor
        {
            get
            {
                if (!HasTargetDoorway)
                    return false;

                if (PlacedRoomCount.HasValue && TargetRoomCount.HasValue
                    && PlacedRoomCount.Value + 1 >= TargetRoomCount.Value)
                    return false;

                if (PlacedRoomCount.HasValue
                    && PlacedRoomCount.Value < MinPlacedRoomsBeforeDeadEnd)
                    return true;

                return OpenDoorwayCount.HasValue
                    && OpenDoorwayCount.Value <= MinOpenDoorwaysBeforeDeadEnd;
            }
        }
    }
}

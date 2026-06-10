namespace THPerfection.LevelGen
{
    public readonly struct PlacementContext
    {
        public readonly FloorId Floor;
        public readonly FloorGrid Grid;
        public readonly DoorwaySlot? TargetDoorway;

        public PlacementContext(FloorId floor, FloorGrid grid, DoorwaySlot? targetDoorway = null)
        {
            Floor          = floor;
            Grid           = grid;
            TargetDoorway  = targetDoorway;
        }

        public bool HasTargetDoorway => TargetDoorway.HasValue;
    }
}

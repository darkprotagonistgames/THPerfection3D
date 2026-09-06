using System.Collections.Generic;

namespace THPerfection.LevelGen
{
    public sealed class BuildingGenerationResult
    {
        public BuildingOccupancy Occupancy { get; }
        public FloorId Floor { get; }
        public IReadOnlyList<RoomInstance> Instances { get; }
        public IReadOnlyList<DoorwaySlot> OpenFrontier { get; }

        public BuildingGenerationResult(
            BuildingOccupancy occupancy,
            FloorId floor,
            IReadOnlyList<RoomInstance> instances,
            IReadOnlyList<DoorwaySlot> openFrontier)
        {
            Occupancy     = occupancy;
            Floor         = floor;
            Instances     = instances;
            OpenFrontier  = openFrontier;
        }
    }
}

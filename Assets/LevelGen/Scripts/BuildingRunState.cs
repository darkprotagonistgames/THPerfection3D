using System.Collections.Generic;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// Mutable authoritative layout for a run: occupancy, room instances, and doorway history.
    /// Generator passes read and update this instead of rebuilding from scratch.
    /// </summary>
    public sealed class BuildingRunState
    {
        readonly HashSet<DoorEdgeKey> _deadDoorways = new();

        public BuildingGenConfig Config { get; private set; }
        public FloorId Floor { get; }
        public BuildingOccupancy Occupancy { get; }
        public List<RoomInstance> Instances { get; }
        public int PassCount { get; private set; }
        public uint LastPassSeed { get; private set; }

        public IReadOnlyCollection<DoorEdgeKey> DeadDoorways => _deadDoorways;

        public BuildingRunState(in BuildingGenConfig config, FloorId floor = FloorId.Main)
        {
            Config     = config;
            Floor      = floor;
            Occupancy  = new BuildingOccupancy();
            Instances  = new List<RoomInstance>();
        }

        public void SetConfig(in BuildingGenConfig config) => Config = config;

        public void Clear()
        {
            Occupancy.Clear();
            Instances.Clear();
            _deadDoorways.Clear();
            PassCount     = 0;
            LastPassSeed  = 0;
        }

        public void NotePassComplete(uint seedUsed, DoorwayFrontier frontier)
        {
            PassCount++;
            LastPassSeed = seedUsed;
            SyncDeadDoorways(frontier);
        }

        public void SyncDeadDoorways(DoorwayFrontier frontier)
        {
            _deadDoorways.Clear();
            frontier.CopyDeadTo(_deadDoorways);
        }

        public void RestoreFrontier(
            RoomCatalog catalog,
            out DoorwayFrontier frontier,
            out HashSet<DoorEdgeKey> connected)
        {
            frontier  = new DoorwayFrontier();
            connected = new HashSet<DoorEdgeKey>();
            frontier.RestoreDead(_deadDoorways);

            if (!Occupancy.TryGetFloor(Floor, out FloorGrid grid))
                return;

            foreach (RoomInstance instance in Instances)
            {
                foreach (KeyValuePair<DoorEdgeKey, CellDoorState> entry in instance.DoorStates)
                {
                    DoorEdgeKey key = entry.Key;
                    switch (entry.Value)
                    {
                        case CellDoorState.Connected:
                            connected.Add(key);
                            break;

                        case CellDoorState.Open:
                            if (IsValidOpenDoorway(grid, key))
                            {
                                frontier.Enqueue(new DoorwaySlot(
                                    key.Floor, key.Cell, key.Side, instance.Id));
                            }
                            break;
                    }
                }
            }
        }

        public bool RebuildOccupancyFromInstances(RoomCatalog catalog)
        {
            if (!Occupancy.TryGetFloor(Floor, out FloorGrid grid))
                grid = Occupancy.GetOrCreateFloor(Floor);

            grid.Clear();

            foreach (RoomInstance instance in Instances)
            {
                if (!catalog.TryGetTemplate(instance.TemplateId, out RoomTemplateDefinition template))
                    return false;

                grid.StampRoom(template, instance.Origin, instance.Rotation, instance.Id);
            }

            return true;
        }

        public BuildingGenerationResult ToResult(IReadOnlyList<DoorwaySlot> openFrontier) =>
            new(Occupancy, Floor, new List<RoomInstance>(Instances), openFrontier);

        static bool IsValidOpenDoorway(FloorGrid grid, in DoorEdgeKey key)
        {
            int2 neighbor = key.Cell + GridTransforms.Direction(key.Side);
            return !grid.IsOccupied(neighbor);
        }
    }
}

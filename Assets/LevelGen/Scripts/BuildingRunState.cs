using System.Collections.Generic;
using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// Mutable authoritative layout for a run: occupancy, room instances, and doorway history.
    /// Generator passes read and update this instead of rebuilding from scratch.
    /// Only wall-contact doors stay permanently closed; other closed edges can re-enter
    /// the frontier on later passes when the room pool or world state changes.
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

        public void ClearDeadDoorwaysForExpansion() => _deadDoorways.Clear();

        /// <summary>
        /// Snapshot frontier from classified Open edges (e.g. after a pass completes).
        /// </summary>
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
                            if (IsExpandableDoorway(grid, key))
                            {
                                frontier.Enqueue(new DoorwaySlot(
                                    key.Floor, key.Cell, key.Side, instance.Id));
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Rebuilds the expansion frontier from layout geometry. Clears pass-local dead
        /// doorway history so a changed room pool (weights, events) can retry edges that
        /// were previously closed for placement failure or budget — only wall contacts
        /// stay off the frontier.
        /// </summary>
        public void RestoreExpansionFrontier(
            RoomCatalog catalog,
            out DoorwayFrontier frontier,
            out HashSet<DoorEdgeKey> connected)
        {
            frontier  = new DoorwayFrontier();
            connected = new HashSet<DoorEdgeKey>();

            if (!Occupancy.TryGetFloor(Floor, out FloorGrid grid))
                return;

            foreach (RoomInstance instance in Instances)
            {
                if (!catalog.TryGetTemplate(instance.TemplateId, out RoomTemplateDefinition template))
                    continue;

                foreach (DoorSocket socket in RoomPlacementMath.GetWorldDoorSockets(
                             template, instance.Origin, instance.Rotation))
                {
                    var key = new DoorEdgeKey(socket, Floor);

                    if (IsMatedDoor(grid, socket))
                    {
                        connected.Add(key);
                        continue;
                    }

                    if (RoomPlacementRules.FacesAdjacentWall(grid, socket.Cell, socket.Side))
                        continue;

                    if (IsExpandableDoorway(grid, key))
                    {
                        frontier.Enqueue(new DoorwaySlot(
                            Floor, socket.Cell, socket.Side, instance.Id));
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

        static bool IsExpandableDoorway(FloorGrid grid, in DoorEdgeKey key)
        {
            int2 neighbor = key.Cell + GridTransforms.Direction(key.Side);
            return !grid.IsOccupied(neighbor);
        }

        static bool IsMatedDoor(FloorGrid grid, in DoorSocket socket)
        {
            int2 neighbor = socket.Cell + GridTransforms.Direction(socket.Side);
            if (!grid.IsOccupied(neighbor))
                return false;

            if (!grid.TryGet(neighbor, out OccupiedCell occupied))
                return false;

            return GridTransforms.HasDoor(occupied.Doors, GridTransforms.Opposite(socket.Side));
        }
    }
}

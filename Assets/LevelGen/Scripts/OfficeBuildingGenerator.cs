using System.Collections.Generic;
using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public static class OfficeBuildingGenerator
    {
        public static BuildingGenerationResult GenerateMainFloor(
            in BuildingGenConfig config,
            RoomCatalog catalog)
        {
            var occupancy = new BuildingOccupancy();
            FloorGrid grid = occupancy.GetOrCreateFloor(FloorId.Main);
            var frontier = new DoorwayFrontier();
            var connected = new HashSet<DoorEdgeKey>();
            var instances = new List<RoomInstance>();
            var attemptCounts = new Dictionary<DoorEdgeKey, int>();
            var rng = new Random(config.Seed == 0 ? 1u : config.Seed);

            if (!TryPlaceSeed(in config, catalog, grid, frontier, connected, instances, ref rng))
                return BuildResult(occupancy, frontier);

            while (instances.Count < config.TargetRoomCount && frontier.OpenCount > 0)
            {
                int clearOpenDoorways = CountClearOpenDoorways(grid, frontier);

                if (!TryPickDoorway(
                        in config, frontier, grid, catalog, FloorId.Main, instances.Count, clearOpenDoorways, ref rng, out DoorwaySlot doorway))
                    break;

                var candidates = CollectCandidates(
                    FloorId.Main, grid, catalog, in doorway, in config, instances.Count, clearOpenDoorways);

                if (candidates.Count == 0)
                {
                    var key = new DoorEdgeKey(doorway);
                    attemptCounts.TryGetValue(key, out int attempts);
                    attempts++;
                    attemptCounts[key] = attempts;

                    if (attempts >= config.MaxAttemptsPerDoorway)
                        frontier.MarkDead(doorway);

                    continue;
                }

                if (!WeightedSelection.TryPick(ref rng, candidates, out PlacementCandidate picked))
                    continue;

                CommitRoom(
                    grid,
                    frontier,
                    connected,
                    instances,
                    FloorId.Main,
                    in picked,
                    doorway);
            }

            ClassifyDoorStates(grid, frontier, connected, catalog, instances, FloorId.Main);
            return BuildResult(occupancy, frontier, instances);
        }

        static bool TryPlaceSeed(
            in BuildingGenConfig config,
            RoomCatalog catalog,
            FloorGrid grid,
            DoorwayFrontier frontier,
            HashSet<DoorEdgeKey> connected,
            List<RoomInstance> instances,
            ref Random rng)
        {
            var seedCandidates = new List<PlacementCandidate>();
            int minOutwardDoors = config.TargetRoomCount > 2 ? 2 : 1;

            foreach (RoomCatalogEntry entry in catalog.ForFloor(FloorId.Main))
            {
                for (int r = 0; r < 4; r++)
                {
                    var rotation = (Rotation90)r;
                    var ctx = new PlacementContext(FloorId.Main, grid);
                    float weight = entry.Evaluator.EvaluatePlacement(
                        ctx, entry.Template, config.SeedOrigin, rotation);

                    if (weight <= 0f)
                        continue;

                    if (RoomPlacementMath.CountOutwardDoorSockets(
                            entry.Template, config.SeedOrigin, rotation, grid) < minOutwardDoors)
                        continue;

                    seedCandidates.Add(new PlacementCandidate(
                        entry, config.SeedOrigin, rotation, weight));
                }
            }

            if (!WeightedSelection.TryPick(ref rng, seedCandidates, out PlacementCandidate picked))
                return false;

            CommitRoom(grid, frontier, connected, instances, FloorId.Main, in picked, targetDoorway: null);
            return true;
        }

        static bool TryPickDoorway(
            in BuildingGenConfig config,
            DoorwayFrontier frontier,
            FloorGrid grid,
            RoomCatalog catalog,
            FloorId floor,
            int placedRoomCount,
            int clearOpenDoorways,
            ref Random rng,
            out DoorwaySlot doorway)
        {
            bool preferExpansion = placedRoomCount < config.MinPlacedRoomsBeforeDeadEnd
                || clearOpenDoorways <= config.MinOpenDoorwaysBeforeDeadEnd;

            if (preferExpansion)
            {
                var viable = new List<DoorwaySlot>();
                foreach (DoorwaySlot slot in frontier.OpenSlots)
                {
                    if (HasExpansionCandidate(
                            floor, grid, catalog, in config, placedRoomCount, clearOpenDoorways, in slot))
                        viable.Add(slot);
                }

                if (viable.Count > 0)
                {
                    doorway = viable[rng.NextInt(0, viable.Count)];
                    return true;
                }

                doorway = default;
                return false;
            }

            return frontier.TryPick(ref rng, out doorway);
        }

        static bool HasExpansionCandidate(
            FloorId floor,
            FloorGrid grid,
            RoomCatalog catalog,
            in BuildingGenConfig config,
            int placedRoomCount,
            int openDoorwayCount,
            in DoorwaySlot doorway)
        {
            foreach (PlacementCandidate candidate in CollectCandidates(
                         floor, grid, catalog, in doorway, in config, placedRoomCount, openDoorwayCount))
            {
                if (candidate.Weight <= 0f)
                    continue;

                var ctx = BuildContext(
                    floor, grid, in doorway, in config, placedRoomCount, openDoorwayCount);
                if (!ctx.RequiresExpansionDoor)
                    return true;

                if (RoomPlacementRules.CountNewOpenDoorways(
                        ctx, candidate.Template, candidate.Origin, candidate.Rotation) > 0)
                    return true;
            }

            return false;
        }

        static List<PlacementCandidate> CollectCandidates(
            FloorId floor,
            FloorGrid grid,
            RoomCatalog catalog,
            in DoorwaySlot doorway,
            in BuildingGenConfig config,
            int placedRoomCount,
            int openDoorwayCount)
        {
            var candidates = new List<PlacementCandidate>();
            var ctx = BuildContext(
                floor, grid, in doorway, in config, placedRoomCount, openDoorwayCount);

            foreach (RoomCatalogEntry entry in catalog.ForFloor(floor))
            {
                foreach (AlignedPlacement placement in RoomPlacementMath.FindAlignedPlacements(
                             entry.Template, doorway))
                {
                    float weight = entry.Evaluator.EvaluatePlacement(
                        ctx,
                        entry.Template,
                        placement.Origin,
                        placement.Rotation);

                    if (weight > 0f)
                    {
                        candidates.Add(new PlacementCandidate(
                            entry,
                            placement.Origin,
                            placement.Rotation,
                            weight));
                    }
                }
            }

            return candidates;
        }

        static PlacementContext BuildContext(
            FloorId floor,
            FloorGrid grid,
            in DoorwaySlot doorway,
            in BuildingGenConfig config,
            int placedRoomCount,
            int openDoorwayCount) =>
            new(
                floor,
                grid,
                doorway,
                openDoorwayCount: openDoorwayCount,
                placedRoomCount: placedRoomCount,
                targetRoomCount: config.TargetRoomCount,
                minOpenDoorwaysBeforeDeadEnd: config.MinOpenDoorwaysBeforeDeadEnd,
                minPlacedRoomsBeforeDeadEnd: config.MinPlacedRoomsBeforeDeadEnd);

        static void CommitRoom(
            FloorGrid grid,
            DoorwayFrontier frontier,
            HashSet<DoorEdgeKey> connected,
            List<RoomInstance> instances,
            FloorId floor,
            in PlacementCandidate candidate,
            DoorwaySlot? targetDoorway)
        {
            int id = instances.Count + 1;
            var instance = new RoomInstance(
                id, candidate.Template, floor, candidate.Origin, candidate.Rotation);

            grid.StampRoom(candidate.Template, candidate.Origin, candidate.Rotation, id);
            instances.Add(instance);

            if (targetDoorway.HasValue)
            {
                DoorwaySlot target = targetDoorway.Value;
                frontier.Remove(target);
                connected.Add(new DoorEdgeKey(target));

                DoorSocket mainDoor = RoomPlacementMath.GetWorldMainDoor(
                    candidate.Template, candidate.Origin, candidate.Rotation);
                connected.Add(new DoorEdgeKey(mainDoor, floor));
            }

            EnqueueOpenDoorways(grid, frontier, floor, id, candidate, targetDoorway);
        }

        static int CountClearOpenDoorways(FloorGrid grid, DoorwayFrontier frontier)
        {
            // Only these count toward MinOpenDoorwaysBeforeDeadEnd; blocked-ray doors stay on the frontier.
            int count = 0;

            foreach (DoorwaySlot slot in frontier.OpenSlots)
            {
                if (RoomPlacementRules.HasClearExpansionRay(grid, slot.Cell, slot.Side))
                    count++;
            }

            return count;
        }

        static void EnqueueOpenDoorways(
            FloorGrid grid,
            DoorwayFrontier frontier,
            FloorId floor,
            int fromRoomId,
            in PlacementCandidate candidate,
            DoorwaySlot? connectedDoorway)
        {
            foreach (DoorSocket socket in RoomPlacementMath.GetWorldDoorSockets(
                         candidate.Template, candidate.Origin, candidate.Rotation))
            {
                if (connectedDoorway.HasValue && IsSameEdge(socket, connectedDoorway.Value, floor))
                    continue;

                if (connectedDoorway.HasValue)
                {
                    DoorSocket mainDoor = RoomPlacementMath.GetWorldMainDoor(
                        candidate.Template, candidate.Origin, candidate.Rotation);
                    if (socket.Cell.Equals(mainDoor.Cell) && socket.Side == mainDoor.Side)
                        continue;
                }

                int2 neighbor = socket.Cell + GridTransforms.Direction(socket.Side);
                if (!grid.IsOccupied(neighbor))
                    frontier.Enqueue(new DoorwaySlot(floor, socket.Cell, socket.Side, fromRoomId));
            }
        }

        static bool IsSameEdge(in DoorSocket socket, in DoorwaySlot doorway, FloorId floor) =>
            doorway.Floor == floor
            && socket.Cell.Equals(doorway.Cell)
            && socket.Side == doorway.Side;

        static void ClassifyDoorStates(
            FloorGrid grid,
            DoorwayFrontier frontier,
            HashSet<DoorEdgeKey> connected,
            RoomCatalog catalog,
            List<RoomInstance> instances,
            FloorId floor)
        {
            var openKeys = new HashSet<DoorEdgeKey>();
            foreach (DoorwaySlot slot in frontier.OpenSlots)
                openKeys.Add(new DoorEdgeKey(slot));

            foreach (RoomInstance instance in instances)
            {
                RoomTemplateDefinition template = FindTemplate(catalog, instance.TemplateId);
                if (template.TemplateId == null)
                    continue;

                foreach (DoorSocket socket in RoomPlacementMath.GetWorldDoorSockets(
                             template, instance.Origin, instance.Rotation))
                {
                    var key = new DoorEdgeKey(socket, floor);
                    CellDoorState state;

                    if (connected.Contains(key))
                        state = CellDoorState.Connected;
                    else if (RoomPlacementRules.FacesAdjacentWall(grid, socket.Cell, socket.Side))
                        state = CellDoorState.Closed;
                    else if (frontier.IsDead(key))
                        state = CellDoorState.Closed;
                    else if (openKeys.Contains(key))
                        state = CellDoorState.Open;
                    else
                        state = CellDoorState.Open;

                    instance.SetDoorState(key, state);
                }
            }
        }

        static RoomTemplateDefinition FindTemplate(RoomCatalog catalog, string templateId)
        {
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                if (catalog.Entries[i].Template.TemplateId == templateId)
                    return catalog.Entries[i].Template;
            }

            return default;
        }

        static BuildingGenerationResult BuildResult(
            BuildingOccupancy occupancy,
            DoorwayFrontier frontier,
            List<RoomInstance> instances = null)
        {
            return new BuildingGenerationResult(
                occupancy,
                FloorId.Main,
                instances ?? new List<RoomInstance>(),
                new List<DoorwaySlot>(frontier.OpenSlots));
        }
    }
}
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
            var state = new BuildingRunState(config);
            GenerateMainFloorInto(state, catalog);
            return state.ToResult(CollectOpenFrontier(state, catalog));
        }

        public static void GenerateMainFloorInto(BuildingRunState state, RoomCatalog catalog)
        {
            state.Clear();
            RunInitialPass(state, catalog);
        }

        public static ExpansionResult ExpandMainFloor(
            BuildingRunState state,
            RoomCatalog catalog,
            int additionalRoomCount,
            uint expansionSeed = 0)
        {
            if (state == null)
                throw new System.ArgumentNullException(nameof(state));

            if (additionalRoomCount <= 0 || state.Instances.Count == 0)
            {
                state.RestoreFrontier(catalog, out DoorwayFrontier idleFrontier, out _);
                return ExpansionResult.NoChange(state, idleFrontier.OpenSlots);
            }

            int roomsBefore = state.Instances.Count;
            uint seed = ResolvePassSeed(state, expansionSeed);
            var rng = new Random(seed == 0 ? 1u : seed);

            state.RestoreFrontier(catalog, out DoorwayFrontier frontier, out HashSet<DoorEdgeKey> connected);
            FloorGrid grid = state.Occupancy.GetOrCreateFloor(state.Floor);
            var attemptCounts = new Dictionary<DoorEdgeKey, int>();
            int targetTotal = roomsBefore + additionalRoomCount;
            var addedInstances = new List<RoomInstance>();

            while (state.Instances.Count < targetTotal && frontier.OpenCount > 0)
            {
                int clearOpenDoorways = CountClearOpenDoorways(grid, frontier);

                if (!TryPickDoorway(
                        state.Config,
                        frontier,
                        grid,
                        catalog,
                        state.Floor,
                        state.Instances.Count,
                        clearOpenDoorways,
                        targetTotal,
                        ref rng,
                        out DoorwaySlot doorway))
                    break;

                var candidates = CollectCandidates(
                    state.Floor,
                    grid,
                    catalog,
                    in doorway,
                    state.Config,
                    state.Instances.Count,
                    clearOpenDoorways,
                    targetTotal);

                if (candidates.Count == 0)
                {
                    var key = new DoorEdgeKey(doorway);
                    attemptCounts.TryGetValue(key, out int attempts);
                    attempts++;
                    attemptCounts[key] = attempts;

                    if (attempts >= state.Config.MaxAttemptsPerDoorway)
                        frontier.MarkDead(doorway);

                    continue;
                }

                if (!WeightedSelection.TryPick(ref rng, candidates, out PlacementCandidate picked))
                    continue;

                int beforeCount = state.Instances.Count;
                CommitRoom(
                    grid,
                    frontier,
                    connected,
                    state.Instances,
                    state.Floor,
                    in picked,
                    doorway);

                if (state.Instances.Count > beforeCount)
                    addedInstances.Add(state.Instances[state.Instances.Count - 1]);
            }

            ClassifyDoorStates(grid, frontier, connected, catalog, state.Instances, state.Floor);
            state.NotePassComplete(seed, frontier);

            return new ExpansionResult(
                addedInstances,
                state.ToResult(new List<DoorwaySlot>(frontier.OpenSlots)),
                roomsBefore);
        }

        static void RunInitialPass(BuildingRunState state, RoomCatalog catalog)
        {
            uint seed = ResolvePassSeed(state, expansionSeed: 0);
            var rng = new Random(seed == 0 ? 1u : seed);
            FloorGrid grid = state.Occupancy.GetOrCreateFloor(state.Floor);
            var frontier = new DoorwayFrontier();
            var connected = new HashSet<DoorEdgeKey>();
            var attemptCounts = new Dictionary<DoorEdgeKey, int>();

            if (!TryPlaceSeed(state.Config, catalog, grid, frontier, connected, state.Instances, ref rng))
            {
                state.NotePassComplete(seed, frontier);
                return;
            }

            while (state.Instances.Count < state.Config.TargetRoomCount && frontier.OpenCount > 0)
            {
                int clearOpenDoorways = CountClearOpenDoorways(grid, frontier);

                if (!TryPickDoorway(
                        state.Config,
                        frontier,
                        grid,
                        catalog,
                        state.Floor,
                        state.Instances.Count,
                        clearOpenDoorways,
                        state.Config.TargetRoomCount,
                        ref rng,
                        out DoorwaySlot doorway))
                    break;

                var candidates = CollectCandidates(
                    state.Floor,
                    grid,
                    catalog,
                    in doorway,
                    state.Config,
                    state.Instances.Count,
                    clearOpenDoorways,
                    state.Config.TargetRoomCount);

                if (candidates.Count == 0)
                {
                    var key = new DoorEdgeKey(doorway);
                    attemptCounts.TryGetValue(key, out int attempts);
                    attempts++;
                    attemptCounts[key] = attempts;

                    if (attempts >= state.Config.MaxAttemptsPerDoorway)
                        frontier.MarkDead(doorway);

                    continue;
                }

                if (!WeightedSelection.TryPick(ref rng, candidates, out PlacementCandidate picked))
                    continue;

                CommitRoom(
                    grid,
                    frontier,
                    connected,
                    state.Instances,
                    state.Floor,
                    in picked,
                    doorway);
            }

            ClassifyDoorStates(grid, frontier, connected, catalog, state.Instances, state.Floor);
            state.NotePassComplete(seed, frontier);
        }

        static IReadOnlyList<DoorwaySlot> CollectOpenFrontier(BuildingRunState state, RoomCatalog catalog)
        {
            state.RestoreFrontier(catalog, out DoorwayFrontier frontier, out _);
            return new List<DoorwaySlot>(frontier.OpenSlots);
        }

        static uint ResolvePassSeed(BuildingRunState state, uint expansionSeed)
        {
            if (expansionSeed != 0)
                return expansionSeed;

            if (state.Config.Seed != 0)
                return state.Config.Seed + (uint)state.PassCount;

            return (uint)(state.PassCount + 1);
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
            int targetRoomCount,
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
                            floor, grid, catalog, in config, placedRoomCount, clearOpenDoorways, targetRoomCount, in slot))
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
            int targetRoomCount,
            in DoorwaySlot doorway)
        {
            foreach (PlacementCandidate candidate in CollectCandidates(
                         floor, grid, catalog, in doorway, in config, placedRoomCount, openDoorwayCount, targetRoomCount))
            {
                if (candidate.Weight <= 0f)
                    continue;

                var ctx = BuildContext(
                    floor, grid, in doorway, in config, placedRoomCount, openDoorwayCount, targetRoomCount);
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
            int openDoorwayCount,
            int targetRoomCount)
        {
            var candidates = new List<PlacementCandidate>();
            var ctx = BuildContext(
                floor, grid, in doorway, in config, placedRoomCount, openDoorwayCount, targetRoomCount);

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
            int openDoorwayCount,
            int targetRoomCount) =>
            new(
                floor,
                grid,
                doorway,
                openDoorwayCount: openDoorwayCount,
                placedRoomCount: placedRoomCount,
                targetRoomCount: targetRoomCount,
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
            int id = NextInstanceId(instances);
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

        static int NextInstanceId(List<RoomInstance> instances)
        {
            int maxId = 0;
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i].Id > maxId)
                    maxId = instances[i].Id;
            }

            return maxId + 1;
        }

        static int CountClearOpenDoorways(FloorGrid grid, DoorwayFrontier frontier)
        {
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
                    instance.TryGetDoorState(key, out CellDoorState previous);
                    CellDoorState state;

                    if (connected.Contains(key))
                        state = CellDoorState.Connected;
                    else if (previous == CellDoorState.Closed)
                        state = CellDoorState.Closed;
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
    }
}

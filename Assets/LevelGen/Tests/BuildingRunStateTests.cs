using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;

namespace THPerfection.LevelGen.Tests
{
    public sealed class BuildingRunStateTests
    {
        static BuildingGenConfig Config(int roomCount, uint seed = 42) => new()
        {
            Seed                         = seed,
            TargetRoomCount              = roomCount,
            MaxAttemptsPerDoorway        = 8,
            MinOpenDoorwaysBeforeDeadEnd = 2,
            MinPlacedRoomsBeforeDeadEnd  = 3,
            SeedOrigin                   = int2.zero,
            CellSize                     = 4f,
            FloorY                       = 0f,
        };

        static RoomCatalog TestCatalog() => RoomCatalog.CreateDefaultMainFloor();

        [Test]
        public void GenerateMainFloorInto_populatesRunState()
        {
            var state = new BuildingRunState(Config(roomCount: 8));
            OfficeBuildingGenerator.GenerateMainFloorInto(state, TestCatalog());

            Assert.GreaterOrEqual(state.Instances.Count, 1);
            Assert.AreEqual(1, state.PassCount);
            Assert.IsTrue(state.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid grid));
            Assert.Greater(grid.Cells.Count, 0);
        }

        [Test]
        public void ExpandMainFloor_addsRoomsWithoutOverlap()
        {
            var catalog = TestCatalog();
            var state = new BuildingRunState(Config(roomCount: 6, seed: 42));
            OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);

            int beforeCells = CountCells(state);
            int beforeRooms = state.Instances.Count;

            ExpansionResult expansion = OfficeBuildingGenerator.ExpandMainFloor(
                state, catalog, additionalRoomCount: 4, expansionSeed: 100);

            Assert.Greater(expansion.AddedInstances.Count, 0);
            Assert.AreEqual(beforeRooms + expansion.AddedInstances.Count, state.Instances.Count);
            Assert.Greater(CountCells(state), beforeCells);
            AssertNoOverlappingCells(state);
        }

        [Test]
        public void ExpandMainFloor_respectsAdditionalRoomBudget()
        {
            var catalog = TestCatalog();
            var state = new BuildingRunState(Config(roomCount: 6, seed: 7));
            OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);

            ExpansionResult expansion = OfficeBuildingGenerator.ExpandMainFloor(
                state, catalog, additionalRoomCount: 3, expansionSeed: 55);

            Assert.LessOrEqual(expansion.AddedInstances.Count, 3);
            Assert.LessOrEqual(state.Instances.Count, expansion.RoomsBefore + 3);
        }

        [Test]
        public void ExpandMainFloor_isDeterministicForSameStateAndSeed()
        {
            var catalog = TestCatalog();

            ExpansionResult RunOnce()
            {
                var state = new BuildingRunState(Config(roomCount: 6, seed: 11));
                OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);
                return OfficeBuildingGenerator.ExpandMainFloor(
                    state, catalog, additionalRoomCount: 4, expansionSeed: 200);
            }

            ExpansionResult a = RunOnce();
            ExpansionResult b = RunOnce();

            Assert.AreEqual(a.RoomsAfter, b.RoomsAfter);
            Assert.AreEqual(a.AddedInstances.Count, b.AddedInstances.Count);
            Assert.AreEqual(CellCount(a.Snapshot), CellCount(b.Snapshot));
        }

        [Test]
        public void ExpandMainFloor_preservesExistingInstanceIds()
        {
            var catalog = TestCatalog();
            var state = new BuildingRunState(Config(roomCount: 5, seed: 3));
            OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);

            var originalIds = new List<int>();
            foreach (RoomInstance instance in state.Instances)
                originalIds.Add(instance.Id);

            OfficeBuildingGenerator.ExpandMainFloor(
                state, catalog, additionalRoomCount: 3, expansionSeed: 88);

            for (int i = 0; i < originalIds.Count; i++)
                Assert.AreEqual(originalIds[i], state.Instances[i].Id);
        }

        [Test]
        public void ExpandMainFloor_keepsWallAdjacentDoorsClosed()
        {
            var catalog = TestCatalog();
            var state = BuildWallContactLayout();
            var wallDoor = new DoorEdgeKey(FloorId.Main, new int2(1, 0), DoorSide.East);

            Assert.IsTrue(state.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid grid));
            Assert.IsTrue(
                RoomPlacementRules.FacesAdjacentWall(grid, wallDoor.Cell, wallDoor.Side),
                "Synthetic layout should include a wall-contact door.");
            Assert.AreEqual(
                CellDoorState.Closed,
                FindDoorState(state, wallDoor),
                "Wall-contact door should start closed.");

            OfficeBuildingGenerator.ExpandMainFloor(
                state, catalog, additionalRoomCount: 2, expansionSeed: 44);

            Assert.AreEqual(
                CellDoorState.Closed,
                FindDoorState(state, wallDoor),
                "Wall-adjacent door should stay closed after expansion.");
        }

        [Test]
        public void ReclassifyDoorStates_marksSurroundedMatedDoorsConnected()
        {
            var catalog = TestCatalog();
            var state = BuildSurroundedCrossLayout();
            RoomInstance inner = state.Instances[0];

            foreach (KeyValuePair<DoorEdgeKey, CellDoorState> entry in inner.DoorStates)
                inner.SetDoorState(entry.Key, CellDoorState.Open);

            OfficeBuildingGenerator.ReclassifyDoorStates(state, catalog);

            foreach (KeyValuePair<DoorEdgeKey, CellDoorState> entry in inner.DoorStates)
            {
                Assert.AreEqual(
                    CellDoorState.Connected,
                    entry.Value,
                    $"Surrounded mated door {entry.Key} should be Connected.");
            }
        }

        [Test]
        public void RestoreExpansionFrontier_retriesNonWallExteriorDoors()
        {
            var catalog = TestCatalog();
            var state = BuildExteriorDoorLayout();
            var exteriorDoor = new DoorEdgeKey(FloorId.Main, int2.zero, DoorSide.North);

            Assert.IsTrue(state.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid grid));
            Assert.IsFalse(
                RoomPlacementRules.FacesAdjacentWall(grid, exteriorDoor.Cell, exteriorDoor.Side),
                "Synthetic layout door should face empty space, not a wall.");
            Assert.IsTrue(IsExpandableExteriorDoor(grid, exteriorDoor));

            // Simulate a prior pass that closed this edge (dead frontier / budget) without wall contact.
            SetDoorState(state, exteriorDoor, CellDoorState.Closed);

            state.ClearDeadDoorwaysForExpansion();
            state.RestoreExpansionFrontier(catalog, out DoorwayFrontier frontier, out _);

            Assert.IsTrue(
                FrontierContains(frontier, exteriorDoor),
                "Expansion frontier should retry non-wall exterior doors even when previously closed.");
        }

        static BuildingRunState BuildSurroundedCrossLayout()
        {
            var state = new BuildingRunState(Config(roomCount: 1, seed: 1));
            FloorGrid grid = state.Occupancy.GetOrCreateFloor(FloorId.Main);

            RoomTemplateDefinition cross = RoomBuiltinTemplates.FourWayCross;
            int2 crossOrigin = new int2(5, 5);
            var inner = new RoomInstance(191, cross, FloorId.Main, crossOrigin, Rotation90.R0);
            grid.StampRoom(cross, crossOrigin, Rotation90.R0, inner.Id);
            state.Instances.Add(inner);

            StampNeighbor(state, grid, RoomBuiltinTemplates.OneByOneNorth, new int2(5, 4), 192);
            StampNeighbor(state, grid, RoomBuiltinTemplates.OneByOneSouth, new int2(5, 7), 193);
            StampNeighbor(state, grid, RoomBuiltinTemplates.TwoByOneHall, new int2(7, 5), 194);
            StampNeighbor(state, grid, RoomBuiltinTemplates.OneByOneEast, new int2(3, 5), 195);

            OfficeBuildingGenerator.ReclassifyDoorStates(state, TestCatalog());
            return state;
        }

        static void StampNeighbor(
            BuildingRunState state,
            FloorGrid grid,
            RoomTemplateDefinition template,
            int2 origin,
            int id)
        {
            var instance = new RoomInstance(id, template, FloorId.Main, origin, Rotation90.R0);
            grid.StampRoom(template, origin, Rotation90.R0, id);
            state.Instances.Add(instance);
        }

        static BuildingRunState BuildWallContactLayout()
        {
            var state = new BuildingRunState(Config(roomCount: 2, seed: 1));
            FloorGrid grid = state.Occupancy.GetOrCreateFloor(FloorId.Main);

            RoomTemplateDefinition office = RoomBuiltinTemplates.TwoByTwoOffice;
            RoomTemplateDefinition cap = RoomBuiltinTemplates.OneByOneNorth;

            var officeInstance = new RoomInstance(1, office, FloorId.Main, int2.zero, Rotation90.R0);
            grid.StampRoom(office, int2.zero, Rotation90.R0, officeInstance.Id);
            state.Instances.Add(officeInstance);

            int2 capOrigin = new int2(2, 0);
            var capInstance = new RoomInstance(2, cap, FloorId.Main, capOrigin, Rotation90.R0);
            grid.StampRoom(cap, capOrigin, Rotation90.R0, capInstance.Id);
            state.Instances.Add(capInstance);

            var wallDoor = new DoorEdgeKey(FloorId.Main, new int2(1, 0), DoorSide.East);
            SetDoorState(state, wallDoor, CellDoorState.Closed);

            return state;
        }

        static BuildingRunState BuildExteriorDoorLayout()
        {
            var state = new BuildingRunState(Config(roomCount: 1, seed: 1));
            FloorGrid grid = state.Occupancy.GetOrCreateFloor(FloorId.Main);

            RoomTemplateDefinition room = RoomBuiltinTemplates.OneByOneNorth;
            var instance = new RoomInstance(1, room, FloorId.Main, int2.zero, Rotation90.R0);
            grid.StampRoom(room, int2.zero, Rotation90.R0, instance.Id);
            state.Instances.Add(instance);

            var exteriorDoor = new DoorEdgeKey(FloorId.Main, int2.zero, DoorSide.North);
            SetDoorState(state, exteriorDoor, CellDoorState.Open);

            return state;
        }

        static void SetDoorState(BuildingRunState state, in DoorEdgeKey key, CellDoorState doorState)
        {
            foreach (RoomInstance instance in state.Instances)
            {
                if (!catalogHasSocket(instance, key))
                    continue;

                instance.SetDoorState(key, doorState);
                return;
            }

            static bool catalogHasSocket(RoomInstance instance, in DoorEdgeKey key)
            {
                RoomTemplateDefinition template = FindTemplateForInstance(instance);
                if (template.TemplateId == null)
                    return false;

                foreach (DoorSocket socket in RoomPlacementMath.GetWorldDoorSockets(
                             template, instance.Origin, instance.Rotation))
                {
                    var socketKey = new DoorEdgeKey(socket, instance.Floor);
                    if (socketKey.Equals(key))
                        return true;
                }

                return false;
            }
        }

        static RoomTemplateDefinition FindTemplateForInstance(RoomInstance instance)
        {
            if (instance.TemplateId == RoomBuiltinTemplates.TwoByTwoOffice.TemplateId)
                return RoomBuiltinTemplates.TwoByTwoOffice;
            if (instance.TemplateId == RoomBuiltinTemplates.OneByOneNorth.TemplateId)
                return RoomBuiltinTemplates.OneByOneNorth;

            return default;
        }

        static CellDoorState FindDoorState(BuildingRunState state, in DoorEdgeKey key)
        {
            foreach (RoomInstance instance in state.Instances)
            {
                if (instance.TryGetDoorState(key, out CellDoorState doorState))
                    return doorState;
            }

            return CellDoorState.Open;
        }

        static bool FrontierContains(DoorwayFrontier frontier, in DoorEdgeKey key)
        {
            foreach (DoorwaySlot slot in frontier.OpenSlots)
            {
                if (slot.Floor == key.Floor
                    && slot.Cell.Equals(key.Cell)
                    && slot.Side == key.Side)
                    return true;
            }

            return false;
        }

        static bool IsExpandableExteriorDoor(FloorGrid grid, in DoorEdgeKey key)
        {
            int2 neighbor = key.Cell + GridTransforms.Direction(key.Side);
            return !grid.IsOccupied(neighbor);
        }

        [Test]
        public void ExpandMainFloor_withZeroAdditionalRooms_isNoOp()
        {
            var catalog = TestCatalog();
            var state = new BuildingRunState(Config(roomCount: 6, seed: 5));
            OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);

            int roomsBefore = state.Instances.Count;
            ExpansionResult expansion = OfficeBuildingGenerator.ExpandMainFloor(
                state, catalog, additionalRoomCount: 0);

            Assert.AreEqual(0, expansion.AddedInstances.Count);
            Assert.AreEqual(roomsBefore, state.Instances.Count);
        }

        [Test]
        public void RebuildOccupancyFromInstances_matchesStampedLayout()
        {
            var catalog = TestCatalog();
            var state = new BuildingRunState(Config(roomCount: 8, seed: 21));
            OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);

            Assert.IsTrue(state.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid original));
            int originalCellCount = original.Cells.Count;

            Assert.IsTrue(state.RebuildOccupancyFromInstances(catalog));
            Assert.IsTrue(state.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid rebuilt));
            Assert.AreEqual(originalCellCount, rebuilt.Cells.Count);
        }

        [Test]
        public void GenerateMainFloor_matchesGenerateMainFloorIntoSnapshot()
        {
            var catalog = TestCatalog();
            var config = Config(roomCount: 8, seed: 99);

            BuildingGenerationResult direct = OfficeBuildingGenerator.GenerateMainFloor(config, catalog);

            var state = new BuildingRunState(config);
            OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);
            state.RestoreFrontier(catalog, out DoorwayFrontier frontier, out _);
            BuildingGenerationResult fromState = state.ToResult(frontier.OpenSlots);

            Assert.AreEqual(direct.Instances.Count, fromState.Instances.Count);
            Assert.AreEqual(CellCount(direct), CellCount(fromState));
        }

        static int CountCells(BuildingRunState state) =>
            state.Occupancy.TryGetFloor(state.Floor, out FloorGrid grid) ? grid.Cells.Count : 0;

        static int CellCount(BuildingGenerationResult result) =>
            result.Occupancy.TryGetFloor(result.Floor, out FloorGrid grid) ? grid.Cells.Count : 0;

        static void AssertNoOverlappingCells(BuildingRunState state)
        {
            Assert.IsTrue(state.Occupancy.TryGetFloor(state.Floor, out FloorGrid grid));
            var seen = new HashSet<int2>();

            foreach (int2 cell in grid.Cells.Keys)
                Assert.IsTrue(seen.Add(cell), $"Duplicate cell occupied: {cell}");
        }
    }
}

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
            Assert.AreEqual(CountCells(a.Snapshot), CountCells(b.Snapshot));
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
            var state = new BuildingRunState(Config(roomCount: 4, seed: 19));
            OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);
            Assert.IsTrue(state.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid grid));

            var wallClosedBefore = new HashSet<DoorEdgeKey>();
            foreach (RoomInstance instance in state.Instances)
            {
                foreach (KeyValuePair<DoorEdgeKey, CellDoorState> entry in instance.DoorStates)
                {
                    if (entry.Value != CellDoorState.Closed)
                        continue;

                    if (RoomPlacementRules.FacesAdjacentWall(grid, entry.Key.Cell, entry.Key.Side))
                        wallClosedBefore.Add(entry.Key);
                }
            }

            Assume.That(
                wallClosedBefore.Count,
                Is.GreaterThan(0),
                "Need wall-contact closed doors in the seed layout to validate sticky wall closure.");

            OfficeBuildingGenerator.ExpandMainFloor(
                state, catalog, additionalRoomCount: 2, expansionSeed: 44);

            foreach (DoorEdgeKey key in wallClosedBefore)
            {
                foreach (RoomInstance instance in state.Instances)
                {
                    if (!instance.TryGetDoorState(key, out CellDoorState stateAfter))
                        continue;

                    Assert.AreEqual(
                        CellDoorState.Closed,
                        stateAfter,
                        $"Wall-adjacent door {key.Floor} {key.Cell} {key.Side} reopened.");
                }
            }
        }

        [Test]
        public void RestoreExpansionFrontier_retriesNonWallExteriorDoors()
        {
            var catalog = TestCatalog();
            var state = new BuildingRunState(Config(roomCount: 4, seed: 19));
            OfficeBuildingGenerator.GenerateMainFloorInto(state, catalog);
            Assert.IsTrue(state.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid grid));

            bool foundRetryableDoor = false;
            foreach (RoomInstance instance in state.Instances)
            {
                foreach (KeyValuePair<DoorEdgeKey, CellDoorState> entry in instance.DoorStates)
                {
                    if (entry.Value != CellDoorState.Closed)
                        continue;

                    if (RoomPlacementRules.FacesAdjacentWall(grid, entry.Key.Cell, entry.Key.Side))
                        continue;

                    if (!IsExpandableExteriorDoor(grid, entry.Key))
                        continue;

                    foundRetryableDoor = true;
                    break;
                }

                if (foundRetryableDoor)
                    break;
            }

            Assume.That(
                foundRetryableDoor,
                Is.True,
                "Need a non-wall exterior closed door to verify expansion frontier retry.");

            state.ClearDeadDoorwaysForExpansion();
            state.RestoreExpansionFrontier(catalog, out DoorwayFrontier frontier, out _);

            Assert.Greater(frontier.OpenCount, 0, "Expansion frontier should include retryable exterior doors.");
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

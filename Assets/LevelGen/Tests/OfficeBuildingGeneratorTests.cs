using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;

namespace THPerfection.LevelGen.Tests
{
    public sealed class OfficeBuildingGeneratorTests
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

        static RoomCatalog TestCatalog()
        {
            return RoomCatalog.CreateDefaultMainFloor();
        }

        [Test]
        public void GenerateMainFloor_neverStopsAtTwoRoomsWhenTargetingMany()
        {
            var catalog = TestCatalog();
            for (uint seed = 1; seed <= 30; seed++)
            {
                var result = OfficeBuildingGenerator.GenerateMainFloor(
                    Config(roomCount: 12, seed: seed), catalog);

                Assert.GreaterOrEqual(
                    result.Instances.Count,
                    3,
                    $"Seed {seed} produced only {result.Instances.Count} rooms.");
            }
        }

        [Test]
        public void GenerateMainFloor_expandsBeyondTwoRooms()
        {
            var result = OfficeBuildingGenerator.GenerateMainFloor(
                Config(roomCount: 12, seed: 42), TestCatalog());

            Assert.GreaterOrEqual(
                result.Instances.Count,
                5,
                "Catalog should include multi-door templates so expansion continues past the seed pair.");
        }

        [Test]
        public void GenerateMainFloor_placesAtLeastSeedRoom()
        {
            var result = OfficeBuildingGenerator.GenerateMainFloor(
                Config(roomCount: 1), TestCatalog());

            Assert.GreaterOrEqual(result.Instances.Count, 1);
            Assert.IsTrue(result.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid grid));
            Assert.Greater(grid.Cells.Count, 0);
        }

        [Test]
        public void GenerateMainFloor_respectsTargetRoomCount()
        {
            var result = OfficeBuildingGenerator.GenerateMainFloor(
                Config(roomCount: 10), TestCatalog());

            Assert.LessOrEqual(result.Instances.Count, 10);
            Assert.GreaterOrEqual(result.Instances.Count, 1);
        }

        [Test]
        public void GenerateMainFloor_isDeterministicForSameSeed()
        {
            var catalog = TestCatalog();
            var a = OfficeBuildingGenerator.GenerateMainFloor(Config(8, seed: 99), catalog);
            var b = OfficeBuildingGenerator.GenerateMainFloor(Config(8, seed: 99), catalog);

            Assert.AreEqual(a.Instances.Count, b.Instances.Count);
            Assert.AreEqual(CellCount(a), CellCount(b));
        }

        [Test]
        public void GenerateMainFloor_hasNoOverlappingCells()
        {
            var result = OfficeBuildingGenerator.GenerateMainFloor(
                Config(roomCount: 15), TestCatalog());

            Assert.IsTrue(result.Occupancy.TryGetFloor(FloorId.Main, out FloorGrid grid));
            var seen = new HashSet<int2>();

            foreach (int2 cell in grid.Cells.Keys)
            {
                Assert.IsTrue(seen.Add(cell), $"Duplicate cell occupied: {cell}");
            }
        }

        [Test]
        public void GenerateMainFloor_classifiesDoorStates()
        {
            var result = OfficeBuildingGenerator.GenerateMainFloor(
                Config(roomCount: 6), TestCatalog());

            foreach (RoomInstance instance in result.Instances)
                Assert.Greater(instance.DoorStates.Count, 0);
        }

        static int CellCount(BuildingGenerationResult result)
        {
            return result.Occupancy.TryGetFloor(result.Floor, out FloorGrid grid)
                ? grid.Cells.Count
                : 0;
        }
    }
}

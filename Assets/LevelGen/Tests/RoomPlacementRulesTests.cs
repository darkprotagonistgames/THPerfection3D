using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;

namespace THPerfection.LevelGen.Tests
{
    public sealed class RoomPlacementRulesTests
    {
        [Test]
        public void TryValidate_rejectsOverlap()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 5), Rotation90.R0, 1);

            var ctx = new PlacementContext(FloorId.Main, grid);
            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.OneByOneSouth,
                new int2(5, 5),
                Rotation90.R0,
                out PlacementFailure failure);

            Assert.IsFalse(valid);
            Assert.AreEqual(PlacementFailure.Overlap, failure);
        }

        [Test]
        public void TryValidate_acceptsAlignedSnapPlacement()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 5), Rotation90.R0, 1);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(5, 5), DoorSide.North);
            var ctx = new PlacementContext(FloorId.Main, grid, doorway);

            AlignedPlacement placement = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.OneByOneSouth, doorway)
                .First();

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.OneByOneSouth,
                placement.Origin,
                placement.Rotation,
                out PlacementFailure failure);

            Assert.IsTrue(valid);
            Assert.AreEqual(PlacementFailure.None, failure);
        }

        [Test]
        public void TryValidate_rejectsMisalignedMainDoor()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 5), Rotation90.R0, 1);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(5, 5), DoorSide.North);
            var ctx = new PlacementContext(FloorId.Main, grid, doorway);

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.OneByOneNorth,
                new int2(5, 6),
                Rotation90.R0,
                out PlacementFailure failure);

            Assert.IsFalse(valid);
            Assert.AreEqual(PlacementFailure.MainDoorMisaligned, failure);
        }

        [Test]
        public void TryValidate_rejectsDoorIntoWall()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 5), Rotation90.R0, 1);
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(6, 5), Rotation90.R0, 2);

            var template = new RoomTemplateDefinition(
                "east_door_only",
                FloorMask.Main,
                1f,
                new[] { int2.zero },
                new[] { new DoorSocket(int2.zero, DoorSide.East, isMainDoor: true) },
                int2.zero,
                DoorSide.East);

            var ctx = new PlacementContext(FloorId.Main, grid);

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                template,
                new int2(4, 5),
                Rotation90.R0,
                out PlacementFailure failure);

            Assert.IsFalse(valid);
            Assert.AreEqual(PlacementFailure.DoorIntoWall, failure);
        }

        [Test]
        public void TryValidate_rejectsFloorMismatch()
        {
            var grid = new FloorGrid();
            var ctx = new PlacementContext(FloorId.Main, grid);

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.BasementOnly,
                int2.zero,
                Rotation90.R0,
                out PlacementFailure failure);

            Assert.IsFalse(valid);
            Assert.AreEqual(PlacementFailure.FloorNotAllowed, failure);
        }

        [Test]
        public void TryValidate_rejectsDeadEndWhenPlacedRoomCountLow()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 5), Rotation90.R0, 1);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(5, 5), DoorSide.North);
            var ctx = new PlacementContext(
                FloorId.Main, grid, doorway,
                openDoorwayCount: 5,
                placedRoomCount: 1,
                targetRoomCount: 12);

            AlignedPlacement placement = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.OneByOneSouth, doorway)
                .First();

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.OneByOneSouth,
                placement.Origin,
                placement.Rotation,
                out PlacementFailure failure);

            Assert.IsFalse(valid);
            Assert.AreEqual(PlacementFailure.DeadEndWhenFrontierLow, failure);
        }

        [Test]
        public void TryValidate_rejectsDeadEndWhenFrontierLow()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 5), Rotation90.R0, 1);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(5, 5), DoorSide.North);
            var ctx = new PlacementContext(
                FloorId.Main, grid, doorway,
                openDoorwayCount: 2,
                placedRoomCount: 5,
                targetRoomCount: 12);

            AlignedPlacement placement = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.OneByOneSouth, doorway)
                .First();

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.OneByOneSouth,
                placement.Origin,
                placement.Rotation,
                out PlacementFailure failure);

            Assert.IsFalse(valid);
            Assert.AreEqual(PlacementFailure.DeadEndWhenFrontierLow, failure);
        }

        [Test]
        public void TryValidate_allowsBranchingRoomWhenFrontierLow()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.TwoByOneEast, int2.zero, Rotation90.R0, 1);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(1, 0), DoorSide.East);
            var ctx = new PlacementContext(
                FloorId.Main, grid, doorway,
                openDoorwayCount: 1,
                placedRoomCount: 5,
                targetRoomCount: 12);

            AlignedPlacement placement = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.TwoByOneEast, doorway)
                .First();

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.TwoByOneEast,
                placement.Origin,
                placement.Rotation,
                out PlacementFailure failure);

            Assert.IsTrue(valid);
            Assert.AreEqual(PlacementFailure.None, failure);
        }

        [Test]
        public void TryValidate_allowsDeadEndWhenFrontierHigh()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 5), Rotation90.R0, 1);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(5, 5), DoorSide.North);
            var ctx = new PlacementContext(
                FloorId.Main, grid, doorway,
                openDoorwayCount: 3,
                placedRoomCount: 5,
                targetRoomCount: 12);

            AlignedPlacement placement = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.OneByOneSouth, doorway)
                .First();

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.OneByOneSouth,
                placement.Origin,
                placement.Rotation,
                out PlacementFailure failure);

            Assert.IsTrue(valid);
            Assert.AreEqual(PlacementFailure.None, failure);
        }

        [Test]
        public void FacesAdjacentWall_trueWhenNeighborHasNoMatchingDoor()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(0, 0), Rotation90.R0, 1);
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(1, 0), Rotation90.R0, 2);

            Assert.IsTrue(RoomPlacementRules.FacesAdjacentWall(
                grid, new int2(0, 0), DoorSide.East));
        }

        [Test]
        public void FacesAdjacentWall_falseWhenNeighborEmpty()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(0, 0), Rotation90.R0, 1);

            Assert.IsFalse(RoomPlacementRules.FacesAdjacentWall(
                grid, new int2(0, 0), DoorSide.East));
        }

        [Test]
        public void HasClearExpansionRay_falseWhenRoomBlocksRayAtAnyDistance()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 10), Rotation90.R0, 1);

            Assert.IsFalse(RoomPlacementRules.HasClearExpansionRay(
                grid, new int2(5, 5), DoorSide.North));
        }

        [Test]
        public void HasClearExpansionRay_trueWhenNoRoomOnRay()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 10), Rotation90.R0, 1);

            Assert.IsTrue(RoomPlacementRules.HasClearExpansionRay(
                grid, new int2(5, 5), DoorSide.East));
        }

        [Test]
        public void CountNewOpenDoorways_ignoresDoorsWhoseRayHitsRoom()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.TwoByOneEast, new int2(0, 0), Rotation90.R0, 1);
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 0), Rotation90.R0, 2);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(1, 0), DoorSide.East);
            var ctx = new PlacementContext(
                FloorId.Main, grid, doorway,
                openDoorwayCount: 1,
                placedRoomCount: 5,
                targetRoomCount: 12);

            AlignedPlacement placement = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.TwoByOneEast, doorway)
                .First();

            int openDoors = RoomPlacementRules.CountNewOpenDoorways(
                ctx,
                RoomTemplateTestDefinitions.TwoByOneEast,
                placement.Origin,
                placement.Rotation);

            Assert.AreEqual(0, openDoors);
        }

        [Test]
        public void TryValidate_allowsPlacementWhenClearRayBudgetHighEvenIfCandidateAddsNoClearRays()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.TwoByOneEast, new int2(0, 0), Rotation90.R0, 1);
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 0), Rotation90.R0, 2);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(1, 0), DoorSide.East);
            var ctx = new PlacementContext(
                FloorId.Main, grid, doorway,
                openDoorwayCount: 3,
                placedRoomCount: 5,
                targetRoomCount: 12);

            AlignedPlacement placement = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.TwoByOneEast, doorway)
                .First();

            bool valid = RoomPlacementRules.TryValidate(
                ctx,
                RoomTemplateTestDefinitions.TwoByOneEast,
                placement.Origin,
                placement.Rotation,
                out PlacementFailure failure);

            Assert.IsTrue(valid);
            Assert.AreEqual(PlacementFailure.None, failure);
        }

        [Test]
        public void BuildingOccupancy_tracksFloorsIndependently()
        {
            var building = new BuildingOccupancy();
            FloorGrid main = building.GetOrCreateFloor(FloorId.Main);
            FloorGrid basement = building.GetOrCreateFloor(FloorId.Basement);

            main.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(1, 1), Rotation90.R0, 1);
            basement.StampRoom(RoomTemplateTestDefinitions.BasementOnly, new int2(1, 1), Rotation90.R0, 2);

            Assert.IsTrue(main.IsOccupied(new int2(1, 1)));
            Assert.IsTrue(basement.IsOccupied(new int2(1, 1)));
        }
    }
}

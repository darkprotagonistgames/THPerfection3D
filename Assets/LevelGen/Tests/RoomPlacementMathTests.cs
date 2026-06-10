using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;

namespace THPerfection.LevelGen.Tests
{
    public sealed class RoomPlacementMathTests
    {
        [Test]
        public void FindAlignedPlacements_oneByOneSouth_matesNorthDoorway()
        {
            var doorway = new DoorwaySlot(FloorId.Main, new int2(5, 5), DoorSide.North);

            var placements = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.OneByOneSouth, doorway)
                .ToArray();

            Assert.AreEqual(1, placements.Length);
            Assert.AreEqual(new int2(5, 6), placements[0].Origin);
            Assert.AreEqual(Rotation90.R0, placements[0].Rotation);
        }

        [Test]
        public void FindAlignedPlacements_twoByOneEast_matesWestDoorwayWithRotation()
        {
            var doorway = new DoorwaySlot(FloorId.Main, new int2(3, 3), DoorSide.West);
            var template = RoomTemplateTestDefinitions.TwoByOneEast;

            var placements = RoomPlacementMath.FindAlignedPlacements(template, doorway).ToArray();

            Assert.IsTrue(placements.Any(p =>
                p.Origin.Equals(new int2(4, 3)) && p.Rotation == Rotation90.R0));
        }

        [Test]
        public void GetWorldCells_includesAllRotatedOffsets()
        {
            int2[] local = { int2.zero, new int2(1, 0) };
            var world = RoomPlacementMath
                .GetWorldCells(local, new int2(10, 10), Rotation90.R90)
                .ToArray();

            CollectionAssert.Contains(world, new int2(10, 10));
            CollectionAssert.Contains(world, new int2(10, 11));
        }
    }
}

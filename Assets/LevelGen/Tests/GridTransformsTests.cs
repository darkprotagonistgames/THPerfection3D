using NUnit.Framework;
using Unity.Mathematics;

namespace THPerfection.LevelGen.Tests
{
    public sealed class GridTransformsTests
    {
        [Test]
        public void RotateCell_R90_mapsPositiveXToPositiveZ()
        {
            int2 rotated = GridTransforms.RotateCell(new int2(1, 0), Rotation90.R90);
            Assert.AreEqual(new int2(0, 1), rotated);
        }

        [Test]
        public void RotateCell_fourQuarterTurns_returnsOriginal()
        {
            int2 cell = new int2(2, -1);
            int2 result = GridTransforms.RotateCell(
                GridTransforms.RotateCell(
                    GridTransforms.RotateCell(
                        GridTransforms.RotateCell(cell, Rotation90.R90),
                        Rotation90.R90),
                    Rotation90.R90),
                Rotation90.R90);

            Assert.AreEqual(cell, result);
        }

        [Test]
        public void Opposite_isInvolution()
        {
            Assert.AreEqual(DoorSide.South, GridTransforms.Opposite(DoorSide.North));
            Assert.AreEqual(DoorSide.North, GridTransforms.Opposite(DoorSide.South));
            Assert.AreEqual(DoorSide.West, GridTransforms.Opposite(DoorSide.East));
        }

        [Test]
        public void RotateSide_advancesWithRotation()
        {
            Assert.AreEqual(DoorSide.West, GridTransforms.RotateSide(DoorSide.North, Rotation90.R90));
            Assert.AreEqual(DoorSide.North, GridTransforms.RotateSide(DoorSide.West, Rotation90.R90));
        }
    }
}

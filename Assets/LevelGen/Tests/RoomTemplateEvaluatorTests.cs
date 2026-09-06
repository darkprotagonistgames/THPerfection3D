using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen.Tests
{
    public sealed class RoomTemplateEvaluatorTests
    {
        DefaultRoomEvaluator _evaluator;

        [SetUp]
        public void SetUp() => _evaluator = ScriptableObject.CreateInstance<DefaultRoomEvaluator>();

        [TearDown]
        public void TearDown()
        {
            if (_evaluator != null)
                Object.DestroyImmediate(_evaluator);
        }

        [Test]
        public void EvaluatePlacement_returnsZeroOnOverlap()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(2, 2), Rotation90.R0, 1);

            var ctx = new PlacementContext(FloorId.Main, grid);
            float weight = _evaluator.EvaluatePlacement(
                ctx,
                RoomTemplateTestDefinitions.OneByOneSouth,
                new int2(2, 2),
                Rotation90.R0);

            Assert.AreEqual(0f, weight);
        }

        [Test]
        public void EvaluatePlacement_returnsBaseWeightForValidSnap()
        {
            var grid = new FloorGrid();
            grid.StampRoom(RoomTemplateTestDefinitions.OneByOneNorth, new int2(5, 5), Rotation90.R0, 1);

            var doorway = new DoorwaySlot(FloorId.Main, new int2(5, 5), DoorSide.North);
            var ctx = new PlacementContext(FloorId.Main, grid, doorway);

            AlignedPlacement placement = RoomPlacementMath
                .FindAlignedPlacements(RoomTemplateTestDefinitions.OneByOneSouth, doorway)
                .First();

            float weight = _evaluator.EvaluatePlacement(
                ctx,
                RoomTemplateTestDefinitions.OneByOneSouth,
                placement.Origin,
                placement.Rotation);

            Assert.AreEqual(RoomTemplateTestDefinitions.OneByOneSouth.BaseWeight, weight);
        }
    }
}

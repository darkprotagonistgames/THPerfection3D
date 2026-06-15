using NUnit.Framework;
using Unity.Mathematics;

namespace THPerfection.LevelGen.Tests
{
    public sealed class RoomTemplateBakerTests
    {
        [Test]
        public void TryValidate_acceptsConnectedOneByOneWithNorthDoor()
        {
            var cells = new[] { int2.zero };
            var doors = new[] { new DoorSocket(int2.zero, DoorSide.North, isMainDoor: true) };

            bool ok = RoomTemplateBaker.TryValidate(
                "test_room",
                FloorMask.Main,
                1f,
                cells,
                doors,
                out string error,
                out int2 mainCell,
                out DoorSide mainSide);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(int2.zero, mainCell);
            Assert.AreEqual(DoorSide.North, mainSide);
        }

        [Test]
        public void TryValidate_rejectsDisconnectedPolyomino()
        {
            var cells = new[] { int2.zero, new int2(2, 0) };
            var doors = new[]
            {
                new DoorSocket(int2.zero, DoorSide.West, isMainDoor: true),
                new DoorSocket(new int2(2, 0), DoorSide.East),
            };

            bool ok = RoomTemplateBaker.TryValidate(
                "broken",
                FloorMask.Main,
                1f,
                cells,
                doors,
                out string error,
                out _,
                out _);

            Assert.IsFalse(ok);
            Assert.That(error, Does.Contain("connected"));
        }

        [Test]
        public void TryValidate_rejectsInwardFacingDoor()
        {
            var cells = new[] { int2.zero, new int2(1, 0) };
            var doors = new[]
            {
                new DoorSocket(int2.zero, DoorSide.East, isMainDoor: true),
                new DoorSocket(new int2(1, 0), DoorSide.West),
            };

            bool ok = RoomTemplateBaker.TryValidate(
                "hall",
                FloorMask.Main,
                1f,
                cells,
                doors,
                out string error,
                out _,
                out _);

            Assert.IsFalse(ok);
            Assert.That(error, Does.Contain("outward"));
        }

        [Test]
        public void TryValidate_requiresExactlyOneMainDoor()
        {
            var cells = new[] { int2.zero };
            var doors = new[]
            {
                new DoorSocket(int2.zero, DoorSide.North, isMainDoor: true),
                new DoorSocket(int2.zero, DoorSide.South, isMainDoor: true),
            };

            bool ok = RoomTemplateBaker.TryValidate(
                "two_mains",
                FloorMask.Main,
                1f,
                cells,
                doors,
                out string error,
                out _,
                out _);

            Assert.IsFalse(ok);
            Assert.That(error, Does.Contain("main door"));
        }
    }
}

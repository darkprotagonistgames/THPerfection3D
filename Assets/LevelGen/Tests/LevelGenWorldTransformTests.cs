using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen.Tests
{
    public sealed class LevelGenWorldTransformTests
    {
        [Test]
        public void CellCenter_mapsGridToWorldXZ()
        {
            Vector3 center = LevelGenWorldTransform.CellCenter(new int2(3, 5), cellSize: 4f, floorY: 1f);
            Assert.AreEqual(new Vector3(12f, 1f, 20f), center);
        }

        [Test]
        public void RoomRootRotation_steps90Degrees()
        {
            Assert.AreEqual(0f, LevelGenWorldTransform.RoomRootRotation(Rotation90.R0).eulerAngles.y, 0.01f);
            Assert.AreEqual(90f, LevelGenWorldTransform.RoomRootRotation(Rotation90.R90).eulerAngles.y, 0.01f);
        }

        [Test]
        public void DoorEdgeCenter_northIsOnPositiveZSide()
        {
            Vector3 north = LevelGenWorldTransform.DoorEdgeCenter(
                new int2(0, 0), DoorSide.North, cellSize: 4f, y: 0.5f);
            Assert.AreEqual(2f, north.z, 0.01f);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace THPerfection.LevelGen.Tests
{
    public sealed class RoomCameraAnchorPoseTests
    {
        const float CellSize = 150f;

        [Test]
        public void TryCompute_rejectsEmptyOrInvalid()
        {
            Assert.IsFalse(RoomCameraAnchorPose.TryCompute(null, CellSize, out _, out _));
            Assert.IsFalse(RoomCameraAnchorPose.TryCompute(System.Array.Empty<Vector2Int>(), CellSize, out _, out _));
            Assert.IsFalse(RoomCameraAnchorPose.TryCompute(
                new[] { Vector2Int.zero }, 0f, out _, out _));
        }

        [Test]
        public void TryCompute_oneByOne_centersLookOnOccupiedCell()
        {
            bool ok = RoomCameraAnchorPose.TryCompute(
                new[] { Vector2Int.zero },
                CellSize,
                out Vector3 position,
                out Quaternion rotation);

            Assert.IsTrue(ok);
            Assert.AreEqual(RoomCameraAnchorPose.PitchXDegrees, rotation.eulerAngles.x, 0.05f);
            Assert.AreEqual(0f, position.x, 0.01f);
            Assert.Greater(position.y, 0f);
            Assert.Less(position.z, 0f);

            Vector3 lookHit = GroundHit(position, rotation);
            Assert.AreEqual(0f, lookHit.x, 0.5f);
            Assert.AreEqual(0f, lookHit.z, 0.5f);
        }

        [Test]
        public void TryCompute_twoByOneHall_centersOnOccupiedAabb()
        {
            bool ok = RoomCameraAnchorPose.TryCompute(
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
                CellSize,
                out Vector3 position,
                out Quaternion rotation);

            Assert.IsTrue(ok);
            Vector3 lookHit = GroundHit(position, rotation);
            Assert.AreEqual(CellSize * 0.5f, lookHit.x, 0.5f);
            Assert.AreEqual(0f, lookHit.z, 0.5f);
        }

        [Test]
        public void TryCompute_wideHall_isHigherThanCompactRoomOfSameDepth()
        {
            RoomCameraAnchorPose.TryCompute(
                new[] { Vector2Int.zero },
                CellSize,
                out Vector3 compact,
                out _);
            RoomCameraAnchorPose.TryCompute(
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0),
                },
                CellSize,
                out Vector3 wide,
                out _);

            Assert.Greater(wide.y, compact.y + 50f);
        }

        [Test]
        public void TryCompute_offsetCells_centersLookOnOccupiedAabb()
        {
            bool ok = RoomCameraAnchorPose.TryCompute(
                new[]
                {
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 1),
                },
                CellSize,
                out Vector3 position,
                out Quaternion rotation);

            Assert.IsTrue(ok);
            Assert.AreEqual(0f, rotation.eulerAngles.y, 0.05f);
            Assert.AreEqual(0f, rotation.eulerAngles.z, 0.05f);

            Vector3 lookHit = GroundHit(position, rotation);
            Assert.AreEqual(CellSize * 1.5f, lookHit.x, 0.5f);
            Assert.AreEqual(CellSize * 0.5f, lookHit.z, 0.5f);
        }

        [Test]
        public void HeightToFrameCenterSquare_isAtLeastTopDownEstimate()
        {
            const float half = 225f;
            float height = RoomCameraAnchorPose.HeightToFrameCenterSquare(half, half);
            float topDown = half / Mathf.Tan(0.5f * RoomCameraAnchorPose.VerticalFieldOfViewDegrees * Mathf.Deg2Rad);
            Assert.GreaterOrEqual(height, topDown * RoomCameraAnchorPose.ViewportMargin - 1f);
        }

        static Vector3 GroundHit(Vector3 position, Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            float t = -position.y / forward.y;
            return position + forward * t;
        }
    }
}

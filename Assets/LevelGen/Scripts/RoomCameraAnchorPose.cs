using System.Collections.Generic;
using UnityEngine;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// Default local pose for a room <c>CameraAnchorSlot</c>: look-centered on the occupied-cell
    /// AABB, pitched 85° down, high enough that the room plus a strip of neighbors fit inside
    /// the center-square gameplay viewport (side UI crops the left/right of the screen).
    /// Authors can move or duplicate the baked slot; this is only the generator default.
    /// </summary>
    public static class RoomCameraAnchorPose
    {
        public const float PitchXDegrees = 85f;
        public const float VerticalFieldOfViewDegrees = 60f;
        /// <summary>Extra neighbor-room strip on each side of the occupied AABB, in cells.</summary>
        public const float SurroundingCellPadding = 0.5f;
        /// <summary>Extra height so the framed AABB is not flush against the UI square edge.</summary>
        public const float ViewportMargin = 1.15f;

        public static bool TryCompute(
            IReadOnlyList<Vector2Int> cells,
            float cellSize,
            out Vector3 localPosition,
            out Quaternion localRotation)
        {
            localRotation = Quaternion.Euler(PitchXDegrees, 0f, 0f);
            localPosition = Vector3.zero;

            if (cells == null || cells.Count == 0 || cellSize <= 0f)
                return false;

            int minX = cells[0].x;
            int maxX = cells[0].x;
            int minY = cells[0].y;
            int maxY = cells[0].y;
            for (int i = 1; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.y > maxY) maxY = cell.y;
            }

            // Cell markers sit at cell centers; occupied floor is ±half a cell around those origins.
            float centerX = (minX + maxX) * 0.5f * cellSize;
            float centerZ = (minY + maxY) * 0.5f * cellSize;
            float pad = SurroundingCellPadding * cellSize;
            float halfWidth = (maxX - minX + 1) * cellSize * 0.5f + pad;
            float halfDepth = (maxY - minY + 1) * cellSize * 0.5f + pad;
            float height = HeightToFrameCenterSquare(halfWidth, halfDepth);

            // Offset toward local -Z so the 85° look ray hits the occupied center.
            float pitchRad = PitchXDegrees * Mathf.Deg2Rad;
            float lookZ = centerZ - height * Mathf.Cos(pitchRad) / Mathf.Sin(pitchRad);
            localPosition = new Vector3(centerX, height, lookZ);
            return true;
        }

        /// <summary>
        /// Height that fits a ground rectangle of size (2*halfWidth, 2*halfDepth) into a
        /// square frustum matching vertical FOV. Side UI uses the leftover horizontal
        /// strips, so usable width equals usable height (not the full 16:9 FOV).
        /// </summary>
        public static float HeightToFrameCenterSquare(float halfWidth, float halfDepth)
        {
            float pitch = PitchXDegrees * Mathf.Deg2Rad;
            float sinPitch = Mathf.Sin(pitch);
            float cosPitch = Mathf.Cos(pitch);
            float tanHalfFov = Mathf.Tan(0.5f * VerticalFieldOfViewDegrees * Mathf.Deg2Rad);

            // Near-edge (south) is the tight constraint: pitch puts the camera closer to that side.
            float fromDepth = sinPitch * halfDepth * (sinPitch / tanHalfFov + cosPitch);
            float fromWidth = sinPitch * (halfWidth / tanHalfFov + halfDepth * cosPitch);
            return Mathf.Max(fromDepth, fromWidth) * ViewportMargin;
        }

        public static float LookDistanceToGround(float height)
        {
            float sinPitch = Mathf.Sin(PitchXDegrees * Mathf.Deg2Rad);
            return height / Mathf.Max(sinPitch, 0.01f);
        }
    }
}

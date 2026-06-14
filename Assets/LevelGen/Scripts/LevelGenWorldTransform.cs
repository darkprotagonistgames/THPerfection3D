using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    public static class LevelGenWorldTransform
    {
        public static Vector3 RoomRootPosition(int2 origin, float cellSize, float floorY) =>
            CellCenter(origin, cellSize, floorY);

        public static Quaternion RoomRootRotation(Rotation90 rotation) =>
            Quaternion.Euler(0f, (int)rotation * 90f, 0f);

        public static Vector3 CellCenter(int2 cell, float cellSize, float floorY) =>
            new(cell.x * cellSize, floorY, cell.y * cellSize);

        public static Vector3 DoorEdgeCenter(int2 cell, DoorSide side, float cellSize, float y)
        {
            float cx = cell.x * cellSize;
            float cz = cell.y * cellSize;
            const float inset = 0.5f;

            return side switch
            {
                DoorSide.North => new Vector3(cx, y, cz + cellSize * inset),
                DoorSide.South => new Vector3(cx, y, cz - cellSize * inset),
                DoorSide.East  => new Vector3(cx + cellSize * inset, y, cz),
                DoorSide.West  => new Vector3(cx - cellSize * inset, y, cz),
                _              => new Vector3(cx, y, cz),
            };
        }

        public static Vector3 DoorEdgeSize(DoorSide side, float cellSize)
        {
            const float thickness = 0.15f;
            const float span = 0.7f;

            return side is DoorSide.North or DoorSide.South
                ? new Vector3(cellSize * span, 0.2f, cellSize * thickness)
                : new Vector3(cellSize * thickness, 0.2f, cellSize * span);
        }
    }
}

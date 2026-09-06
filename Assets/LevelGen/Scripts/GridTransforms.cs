using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public static class GridTransforms
    {
        public static int2 Direction(DoorSide side) => side switch
        {
            DoorSide.North => new int2(0, 1),
            DoorSide.East  => new int2(1, 0),
            DoorSide.South => new int2(0, -1),
            DoorSide.West  => new int2(-1, 0),
            _              => int2.zero,
        };

        public static DoorSide Opposite(DoorSide side) => side switch
        {
            DoorSide.North => DoorSide.South,
            DoorSide.East  => DoorSide.West,
            DoorSide.South => DoorSide.North,
            DoorSide.West  => DoorSide.East,
            _              => side,
        };

        public static DoorMask ToMask(DoorSide side) => side switch
        {
            DoorSide.North => DoorMask.North,
            DoorSide.East  => DoorMask.East,
            DoorSide.South => DoorMask.South,
            DoorSide.West  => DoorMask.West,
            _              => DoorMask.None,
        };

        public static bool HasDoor(DoorMask mask, DoorSide side) =>
            (mask & ToMask(side)) != 0;

        public static DoorMask AddDoor(DoorMask mask, DoorSide side) =>
            mask | ToMask(side);

        public static int2 RotateCell(int2 cell, Rotation90 rotation)
        {
            return rotation switch
            {
                Rotation90.R0   => cell,
                Rotation90.R90  => new int2(-cell.y, cell.x),
                Rotation90.R180 => new int2(-cell.x, -cell.y),
                Rotation90.R270 => new int2(cell.y, -cell.x),
                _               => cell,
            };
        }

        public static DoorSide RotateSide(DoorSide side, Rotation90 rotation) =>
            (DoorSide)(((int)side + (int)rotation) % 4);

        public static bool ContainsCell(int2[] cells, int2 cell)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Equals(cell))
                    return true;
            }

            return false;
        }

        public static FloorMask ToMask(FloorId floor) => floor switch
        {
            FloorId.Basement => FloorMask.Basement,
            FloorId.Main     => FloorMask.Main,
            FloorId.Attic    => FloorMask.Attic,
            _                => FloorMask.None,
        };
    }
}

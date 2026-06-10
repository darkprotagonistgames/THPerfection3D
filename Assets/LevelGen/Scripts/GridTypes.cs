using System;

namespace THPerfection.LevelGen
{
    [Flags]
    public enum FloorMask
    {
        None     = 0,
        Basement = 1 << 0,
        Main     = 1 << 1,
        Attic    = 1 << 2,
        All      = Basement | Main | Attic,
    }

    public enum FloorId : byte
    {
        Basement = 0,
        Main     = 1,
        Attic    = 2,
    }

    public enum DoorSide : byte
    {
        North = 0,
        East  = 1,
        South = 2,
        West  = 3,
    }

    [Flags]
    public enum DoorMask : byte
    {
        None  = 0,
        North = 1 << 0,
        East  = 1 << 1,
        South = 1 << 2,
        West  = 1 << 3,
    }

    public enum Rotation90 : byte
    {
        R0   = 0,
        R90  = 1,
        R180 = 2,
        R270 = 3,
    }

    public enum PlacementFailure : byte
    {
        None,
        FloorNotAllowed,
        Overlap,
        MainDoorMisaligned,
        DoorIntoWall,
    }
}

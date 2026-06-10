using Unity.Mathematics;

namespace THPerfection.LevelGen.Tests
{
    static class RoomTemplateTestDefinitions
    {
        public static readonly RoomTemplateDefinition OneByOneNorth =
            new(
                "one_by_one_north",
                FloorMask.Main,
                1f,
                new[] { int2.zero },
                new[] { new DoorSocket(int2.zero, DoorSide.North, isMainDoor: true) },
                int2.zero,
                DoorSide.North);

        public static readonly RoomTemplateDefinition OneByOneSouth =
            new(
                "one_by_one_south",
                FloorMask.Main,
                1f,
                new[] { int2.zero },
                new[] { new DoorSocket(int2.zero, DoorSide.South, isMainDoor: true) },
                int2.zero,
                DoorSide.South);

        public static readonly RoomTemplateDefinition TwoByOneEast =
            new(
                "two_by_one_east",
                FloorMask.Main,
                1f,
                new[] { int2.zero, new int2(1, 0) },
                new[]
                {
                    new DoorSocket(int2.zero, DoorSide.West, isMainDoor: true),
                    new DoorSocket(new int2(1, 0), DoorSide.East),
                },
                int2.zero,
                DoorSide.West);

        public static readonly RoomTemplateDefinition BasementOnly =
            new(
                "basement_closet",
                FloorMask.Basement,
                1f,
                new[] { int2.zero },
                new[] { new DoorSocket(int2.zero, DoorSide.North, isMainDoor: true) },
                int2.zero,
                DoorSide.North);
    }
}

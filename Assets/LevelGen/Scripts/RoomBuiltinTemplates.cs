using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public static class RoomBuiltinTemplates
    {
        public static readonly RoomTemplateDefinition OneByOneNorth =
            new(
                "one_by_one_north",
                FloorMask.Main,
                0.25f,
                new[] { int2.zero },
                new[] { new DoorSocket(int2.zero, DoorSide.North, isMainDoor: true) },
                int2.zero,
                DoorSide.North);

        public static readonly RoomTemplateDefinition OneByOneSouth =
            new(
                "one_by_one_south",
                FloorMask.Main,
                0.25f,
                new[] { int2.zero },
                new[] { new DoorSocket(int2.zero, DoorSide.South, isMainDoor: true) },
                int2.zero,
                DoorSide.South);

        public static readonly RoomTemplateDefinition OneByOneEast =
            new(
                "one_by_one_east",
                FloorMask.Main,
                0.25f,
                new[] { int2.zero },
                new[] { new DoorSocket(int2.zero, DoorSide.East, isMainDoor: true) },
                int2.zero,
                DoorSide.East);

        public static readonly RoomTemplateDefinition TwoByOneHall =
            new(
                "two_by_one_hall",
                FloorMask.Main,
                1.2f,
                new[] { int2.zero, new int2(1, 0) },
                new[]
                {
                    new DoorSocket(int2.zero, DoorSide.West, isMainDoor: true),
                    new DoorSocket(new int2(1, 0), DoorSide.East),
                },
                int2.zero,
                DoorSide.West);

        public static readonly RoomTemplateDefinition TwoByTwoOffice =
            new(
                "two_by_two_office",
                FloorMask.Main,
                0.8f,
                new[] { int2.zero, new int2(1, 0), new int2(0, 1), new int2(1, 1) },
                new[]
                {
                    new DoorSocket(int2.zero, DoorSide.South, isMainDoor: true),
                    new DoorSocket(new int2(1, 0), DoorSide.East),
                },
                int2.zero,
                DoorSide.South);

        public static readonly RoomTemplateDefinition OneByTwoHallNorthSouth =
            new(
                "one_by_two_hall_ns",
                FloorMask.Main,
                1.4f,
                new[] { int2.zero, new int2(0, 1) },
                new[]
                {
                    new DoorSocket(int2.zero, DoorSide.South, isMainDoor: true),
                    new DoorSocket(new int2(0, 1), DoorSide.North),
                },
                int2.zero,
                DoorSide.South);

        public static readonly RoomTemplateDefinition OneByThreeHallEastWest =
            new(
                "one_by_three_hall_ew",
                FloorMask.Main,
                1.3f,
                new[] { int2.zero, new int2(1, 0), new int2(2, 0) },
                new[]
                {
                    new DoorSocket(int2.zero, DoorSide.West, isMainDoor: true),
                    new DoorSocket(new int2(2, 0), DoorSide.East),
                },
                int2.zero,
                DoorSide.West);

        public static readonly RoomTemplateDefinition ThreeWayTJunction =
            new(
                "three_way_t",
                FloorMask.Main,
                1.1f,
                new[] { int2.zero, new int2(1, 0), new int2(0, 1) },
                new[]
                {
                    new DoorSocket(int2.zero, DoorSide.West, isMainDoor: true),
                    new DoorSocket(new int2(1, 0), DoorSide.East),
                    new DoorSocket(new int2(0, 1), DoorSide.North),
                },
                int2.zero,
                DoorSide.West);

        public static readonly RoomTemplateDefinition FourWayCross =
            new(
                "four_way_cross",
                FloorMask.Main,
                0.9f,
                new[] { int2.zero, new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) },
                new[]
                {
                    new DoorSocket(int2.zero, DoorSide.South, isMainDoor: true),
                    new DoorSocket(new int2(1, 0), DoorSide.East),
                    new DoorSocket(new int2(-1, 0), DoorSide.West),
                    new DoorSocket(new int2(0, 1), DoorSide.North),
                },
                int2.zero,
                DoorSide.South);

        public static readonly RoomTemplateDefinition BasementCloset =
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

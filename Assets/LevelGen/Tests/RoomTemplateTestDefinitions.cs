using Unity.Mathematics;

namespace THPerfection.LevelGen.Tests
{
    static class RoomTemplateTestDefinitions
    {
        public static readonly RoomTemplateDefinition OneByOneNorth = RoomBuiltinTemplates.OneByOneNorth;
        public static readonly RoomTemplateDefinition OneByOneSouth = RoomBuiltinTemplates.OneByOneSouth;
        public static readonly RoomTemplateDefinition TwoByOneEast = RoomBuiltinTemplates.TwoByOneHall;
        public static readonly RoomTemplateDefinition BasementOnly = RoomBuiltinTemplates.BasementCloset;
    }
}

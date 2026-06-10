using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public readonly struct RoomTemplateDefinition
    {
        public readonly string TemplateId;
        public readonly FloorMask AllowedFloors;
        public readonly float BaseWeight;
        public readonly int2[] Cells;
        public readonly DoorSocket[] DoorSockets;
        public readonly int2 MainDoorCell;
        public readonly DoorSide MainDoorSide;

        public RoomTemplateDefinition(
            string templateId,
            FloorMask allowedFloors,
            float baseWeight,
            int2[] cells,
            DoorSocket[] doorSockets,
            int2 mainDoorCell,
            DoorSide mainDoorSide)
        {
            TemplateId      = templateId;
            AllowedFloors   = allowedFloors;
            BaseWeight      = baseWeight;
            Cells           = cells;
            DoorSockets     = doorSockets;
            MainDoorCell    = mainDoorCell;
            MainDoorSide    = mainDoorSide;
        }
    }
}

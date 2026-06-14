using System;
using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public readonly struct DoorEdgeKey : IEquatable<DoorEdgeKey>
    {
        public readonly FloorId Floor;
        public readonly int2 Cell;
        public readonly DoorSide Side;

        public DoorEdgeKey(FloorId floor, int2 cell, DoorSide side)
        {
            Floor = floor;
            Cell  = cell;
            Side  = side;
        }

        public DoorEdgeKey(in DoorwaySlot slot) : this(slot.Floor, slot.Cell, slot.Side) { }

        public DoorEdgeKey(in DoorSocket socket, FloorId floor) : this(floor, socket.Cell, socket.Side) { }

        public bool Equals(DoorEdgeKey other) =>
            Floor == other.Floor && Cell.Equals(other.Cell) && Side == other.Side;

        public override bool Equals(object obj) => obj is DoorEdgeKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Floor, Cell.x, Cell.y, Side);
    }
}

using System.Collections.Generic;
using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public sealed class FloorGrid
    {
        readonly Dictionary<int2, OccupiedCell> _cells = new();

        public IReadOnlyDictionary<int2, OccupiedCell> Cells => _cells;

        public bool IsOccupied(int2 cell) => _cells.ContainsKey(cell);

        public bool TryGet(int2 cell, out OccupiedCell occupied) =>
            _cells.TryGetValue(cell, out occupied);

        public void Clear() => _cells.Clear();

        public void StampRoom(
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation,
            int roomInstanceId)
        {
            foreach (int2 worldCell in RoomPlacementMath.GetWorldCells(template.Cells, origin, rotation))
                _cells[worldCell] = new OccupiedCell(roomInstanceId);

            foreach (DoorSocket worldSocket in RoomPlacementMath.GetWorldDoorSockets(template, origin, rotation))
            {
                if (!_cells.TryGetValue(worldSocket.Cell, out OccupiedCell occupied))
                    continue;

                occupied.Doors = GridTransforms.AddDoor(occupied.Doors, worldSocket.Side);
                _cells[worldSocket.Cell] = occupied;
            }
        }
    }
}

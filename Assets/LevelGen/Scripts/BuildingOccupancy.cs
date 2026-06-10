using System.Collections.Generic;

namespace THPerfection.LevelGen
{
    public sealed class BuildingOccupancy
    {
        readonly Dictionary<FloorId, FloorGrid> _floors = new();

        public FloorGrid GetOrCreateFloor(FloorId floor)
        {
            if (!_floors.TryGetValue(floor, out FloorGrid grid))
            {
                grid = new FloorGrid();
                _floors[floor] = grid;
            }

            return grid;
        }

        public bool TryGetFloor(FloorId floor, out FloorGrid grid) =>
            _floors.TryGetValue(floor, out grid);

        public void Clear() => _floors.Clear();
    }
}

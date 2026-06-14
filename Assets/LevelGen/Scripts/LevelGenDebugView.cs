using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    [DisallowMultipleComponent]
    public sealed class LevelGenDebugView : MonoBehaviour
    {
        [Header("Generation")]
        public BuildingGenConfig Config = BuildingGenConfig.Default;

        [Tooltip("When enabled, picks a new seed each time you generate. Disable to use Seed below.")]
        public bool RandomizeSeedOnGenerate = true;

        [Tooltip("Used when Randomize Seed On Generate is disabled. Reusable for reproducible layouts.")]
        public uint Seed = 1;

        [Header("Gizmo Colors")]
        public Color RoomFillColor = new(0.2f, 0.45f, 0.85f, 0.25f);
        public Color OpenDoorColor = new(1f, 0.85f, 0.1f, 1f);
        public Color ConnectedDoorColor = new(0.2f, 0.9f, 0.35f, 1f);
        public Color ClosedDoorColor = new(0.85f, 0.25f, 0.2f, 1f);

        [Header("Last Result")]
        [SerializeField] int _roomCount;
        [SerializeField] int _openFrontierCount;

        BuildingGenerationResult _result;

        public BuildingGenerationResult Result => _result;

        [ContextMenu("Randomize Seed")]
        public void RandomizeSeed()
        {
            Seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
        }

        [ContextMenu("Generate Main Floor")]
        public void GenerateMainFloor()
        {
            if (RandomizeSeedOnGenerate)
                RandomizeSeed();

            Config.Seed = Seed;
            RoomCatalog catalog = RoomCatalog.CreateDefaultMainFloor();
            _result = OfficeBuildingGenerator.GenerateMainFloor(Config, catalog);
            _roomCount = _result.Instances.Count;
            _openFrontierCount = _result.OpenFrontier.Count;
            Debug.Log($"[LevelGen] Seed {Seed}: {_roomCount} rooms, {_openFrontierCount} open doorways.");
        }

        void OnDrawGizmosSelected()
        {
            if (_result == null)
                return;

            if (!_result.Occupancy.TryGetFloor(_result.Floor, out FloorGrid grid))
                return;

            float cellSize = Mathf.Max(0.01f, Config.CellSize);
            float y = Config.FloorY + 0.05f;
            float doorY = Config.FloorY + 0.5f;

            foreach (var (gridCell, occupied) in grid.Cells)
            {
                var center = new Vector3(gridCell.x * cellSize, y, gridCell.y * cellSize);
                Gizmos.color = RoomFillColor;
                Gizmos.DrawCube(center, new Vector3(cellSize * 0.92f, 0.1f, cellSize * 0.92f));
            }

            foreach (RoomInstance instance in _result.Instances)
            {
                foreach (var (key, state) in instance.DoorStates)
                {
                    Gizmos.color = state switch
                    {
                        CellDoorState.Open      => OpenDoorColor,
                        CellDoorState.Connected => ConnectedDoorColor,
                        _                       => ClosedDoorColor,
                    };

                    Vector3 edgeCenter = DoorEdgeCenter(key.Cell, key.Side, cellSize, doorY);
                    Vector3 edgeSize = DoorEdgeSize(key.Side, cellSize);
                    Gizmos.DrawCube(edgeCenter, edgeSize);
                }
            }
        }

        static Vector3 DoorEdgeCenter(int2 cell, DoorSide side, float cellSize, float y)
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

        static Vector3 DoorEdgeSize(DoorSide side, float cellSize)
        {
            const float thickness = 0.15f;
            const float span = 0.7f;

            return side is DoorSide.North or DoorSide.South
                ? new Vector3(cellSize * span, 0.2f, cellSize * thickness)
                : new Vector3(cellSize * thickness, 0.2f, cellSize * span);
        }
    }
}

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    [DisallowMultipleComponent]
    public sealed class LevelGenDebugView : MonoBehaviour
    {
        [Header("Catalog")]
        [Tooltip("Authored room prefabs baked at generate time. When null, uses builtin templates only.")]
        public RoomCatalogAsset CatalogAsset;

        [Header("Generation")]
        public BuildingGenConfig Config = BuildingGenConfig.Default;

        [Tooltip("When enabled, picks a new seed each time you generate. Disable to use Seed below.")]
        public bool RandomizeSeedOnGenerate = true;

        [Tooltip("Used when Randomize Seed On Generate is disabled. Reusable for reproducible layouts.")]
        public uint Seed = 1;

        [Header("Expansion")]
        [Min(1)]
        public int AdditionalRoomsOnContinue = 4;

        [Header("Visual Spawn")]
        public bool SpawnVisualsOnGenerate = true;

        [Header("Gizmo Colors")]
        public Color RoomFillColor = new(0.2f, 0.45f, 0.85f, 0.25f);
        public Color OpenDoorColor = new(1f, 0.85f, 0.1f, 1f);
        public Color ConnectedDoorColor = new(0.2f, 0.9f, 0.35f, 1f);
        public Color ClosedDoorColor = new(0.85f, 0.25f, 0.2f, 1f);

        [Header("Last Result")]
        [SerializeField] int _roomCount;
        [SerializeField] int _openFrontierCount;

        BuildingGenerationResult _result;
        BuildingRunState _runState;
        LevelGenRoomSpawner _roomSpawner;
        RoomCatalog _lastCatalog;

        public BuildingGenerationResult Result => _result;
        public BuildingRunState RunState => _runState;
        public RoomCatalog LastCatalog => _lastCatalog;

        void Reset()
        {
            EnsureRoomSpawner();
        }

        void EnsureRoomSpawner()
        {
            if (_roomSpawner == null)
                _roomSpawner = GetComponent<LevelGenRoomSpawner>();

            if (_roomSpawner == null)
                _roomSpawner = gameObject.AddComponent<LevelGenRoomSpawner>();
        }

        public LevelGenRoomSpawner RoomSpawner
        {
            get
            {
                EnsureRoomSpawner();
                return _roomSpawner;
            }
        }

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
            _lastCatalog = ResolveCatalog();
            _runState = new BuildingRunState(Config);
            OfficeBuildingGenerator.GenerateMainFloorInto(_runState, _lastCatalog);
            _result = _runState.ToResult(CollectOpenFrontier());
            _roomCount = _result.Instances.Count;
            _openFrontierCount = _result.OpenFrontier.Count;
            Debug.Log($"[LevelGen] Seed {Seed}: {_roomCount} rooms, {_openFrontierCount} open doorways.");

            if (SpawnVisualsOnGenerate)
                SpawnVisuals(_lastCatalog);
        }

        [ContextMenu("Continue Expansion")]
        public void ContinueExpansion()
        {
            if (_runState == null || _runState.Instances.Count == 0)
            {
                Debug.LogWarning("[LevelGen] Generate a floor before continuing expansion.");
                return;
            }

            _lastCatalog ??= ResolveCatalog();
            ExpansionResult expansion = OfficeBuildingGenerator.ExpandMainFloor(
                _runState,
                _lastCatalog,
                AdditionalRoomsOnContinue,
                expansionSeed: Seed);

            _result = expansion.Snapshot;
            _roomCount = _result.Instances.Count;
            _openFrontierCount = _result.OpenFrontier.Count;
            Debug.Log(
                $"[LevelGen] Continued expansion (seed {Seed}): +{expansion.AddedInstances.Count} rooms "
                + $"({expansion.RoomsBefore} → {expansion.RoomsAfter}), {_openFrontierCount} open doorways.");

            if (SpawnVisualsOnGenerate)
            {
                if (expansion.AddedInstances.Count > 0)
                    RoomSpawner.SpawnAdditional(expansion.AddedInstances, Config, _lastCatalog);
                else
                    Debug.Log("[LevelGen] No new rooms were placed this pass.");
            }
        }

        IReadOnlyList<DoorwaySlot> CollectOpenFrontier()
        {
            if (_runState == null)
                return System.Array.Empty<DoorwaySlot>();

            _runState.RestoreFrontier(_lastCatalog ?? ResolveCatalog(), out DoorwayFrontier frontier, out _);
            return new List<DoorwaySlot>(frontier.OpenSlots);
        }

        public RoomCatalog ResolveCatalog() =>
            CatalogAsset != null ? CatalogAsset.BuildCatalog() : RoomCatalog.CreateDefaultMainFloor();

        public void SpawnVisuals()
        {
            SpawnVisuals(_lastCatalog ?? ResolveCatalog());
        }

        public void SpawnVisuals(RoomCatalog catalog)
        {
            if (_result == null)
            {
                Debug.LogWarning("[LevelGen] Generate a floor before spawning visuals.");
                return;
            }

            RoomSpawner.Spawn(_result, Config, catalog);
        }

        public void ClearVisuals()
        {
            RoomSpawner.ClearSpawned();
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

                    Vector3 edgeCenter = LevelGenWorldTransform.DoorEdgeCenter(key.Cell, key.Side, cellSize, doorY);
                    Vector3 edgeSize = LevelGenWorldTransform.DoorEdgeSize(key.Side, cellSize);
                    Gizmos.DrawCube(edgeCenter, edgeSize);
                }
            }
        }
    }
}

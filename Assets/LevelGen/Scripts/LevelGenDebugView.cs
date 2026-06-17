using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// Editor/dev wrapper around <see cref="BuildingRunDirector"/> with gizmos and seed randomization.
    /// Production runs should use BuildingRunDirector directly in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BuildingRunDirector))]
    public sealed class LevelGenDebugView : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("When enabled, picks a new seed each time you generate. Disable to pin Run Seed on the director.")]
        public bool RandomizeSeedOnGenerate = true;

        [Header("Gizmo Colors")]
        public Color RoomFillColor = new(0.2f, 0.45f, 0.85f, 0.25f);
        public Color OpenDoorColor = new(1f, 0.85f, 0.1f, 1f);
        public Color ConnectedDoorColor = new(0.2f, 0.9f, 0.35f, 1f);
        public Color ClosedDoorColor = new(0.85f, 0.25f, 0.2f, 1f);

        BuildingRunDirector _director;

        public BuildingRunDirector Director
        {
            get
            {
                if (_director == null)
                    _director = GetComponent<BuildingRunDirector>();
                return _director;
            }
        }

        public BuildingGenerationResult Result => Director.LastResult;
        public BuildingRunState RunState => Director.RunState;
        public LevelGenRoomSpawner RoomSpawner => Director.RoomSpawner;

        [ContextMenu("Randomize Seed")]
        public void RandomizeSeed()
        {
            Director.RunSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
        }

        [ContextMenu("Generate Main Floor")]
        public void GenerateMainFloor()
        {
            if (RandomizeSeedOnGenerate)
                RandomizeSeed();

            Director.StartRun(Director.RunSeed);
            LogRunSummary("Generate");
        }

        [ContextMenu("Continue Expansion")]
        public void ContinueExpansion()
        {
            ExpansionResult expansion = Director.ExpandRun(expansionSeedOverride: Director.RunSeed);

            Debug.Log(
                $"[LevelGen] Continued expansion (seed {Director.RunSeed}): +{expansion.AddedInstances.Count} rooms "
                + $"({expansion.RoomsBefore} → {expansion.RoomsAfter}).");
        }

        public void SpawnVisuals() => Director.RespawnAllRooms();

        public void ClearVisuals() => Director.ClearRun();

        void LogRunSummary(string label)
        {
            BuildingGenerationResult result = Director.LastResult;
            int rooms = result?.Instances.Count ?? 0;
            int frontier = result?.OpenFrontier.Count ?? 0;
            Debug.Log($"[LevelGen] {label} seed {Director.RunSeed}: {rooms} rooms, {frontier} open doorways.");
        }

        void OnDrawGizmosSelected()
        {
            BuildingGenerationResult result = Director.LastResult;
            if (result == null)
                return;

            if (!result.Occupancy.TryGetFloor(result.Floor, out FloorGrid grid))
                return;

            float cellSize = Mathf.Max(0.01f, Director.Config.CellSize);
            float y = Director.Config.FloorY + 0.05f;
            float doorY = Director.Config.FloorY + 0.5f;

            foreach (var (gridCell, occupied) in grid.Cells)
            {
                var center = new Vector3(gridCell.x * cellSize, y, gridCell.y * cellSize);
                Gizmos.color = RoomFillColor;
                Gizmos.DrawCube(center, new Vector3(cellSize * 0.92f, 0.1f, cellSize * 0.92f));
            }

            foreach (RoomInstance instance in result.Instances)
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

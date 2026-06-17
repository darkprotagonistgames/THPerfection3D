using System;
using System.Collections.Generic;
using UnityEngine;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// GameObject orchestrator for procedural building runs: catalog, generation,
    /// run state, and room prefab spawn. Gameplay ECS reads committed layout — it does not pick rooms.
    /// Room weights and special-case scoring live on <see cref="RoomTemplateBase"/> evaluators
    /// (prefab or catalog Evaluator Override), not on a separate policy asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingRunDirector : MonoBehaviour
    {
        [Header("Catalog")]
        [Tooltip("Authored room prefabs baked at runtime. When null, uses builtin templates.")]
        public RoomCatalogAsset CatalogAsset;

        [Header("Generation")]
        public BuildingGenConfig Config = BuildingGenConfig.Default;

        public uint RunSeed = 1;

        [Header("Spawn")]
        public bool SpawnRoomsOnCommit = true;

        [SerializeField] LevelGenRoomSpawner _roomSpawner;

        BuildingRunState _runState;
        BuildingGenerationResult _lastResult;
        RoomCatalog _lastCatalog;

        public BuildingRunState RunState => _runState;
        public BuildingGenerationResult LastResult => _lastResult;
        public RoomCatalog LastCatalog => _lastCatalog;
        public RoomDoorVisualPhase DoorVisualPhase { get; private set; } = RoomDoorVisualPhase.Gameplay;

        public event Action<BuildingGenerationResult> RunStarted;
        public event Action<ExpansionResult> RunExpanded;

        public LevelGenRoomSpawner RoomSpawner
        {
            get
            {
                EnsureRoomSpawner();
                return _roomSpawner;
            }
        }

        void Reset() => EnsureRoomSpawner();

        public RoomCatalog ResolveCatalog() =>
            CatalogAsset != null ? CatalogAsset.BuildCatalog() : RoomCatalog.CreateDefaultMainFloor();

        public void StartRun(uint? seedOverride = null)
        {
            if (seedOverride.HasValue)
                RunSeed = seedOverride.Value;

            Config.Seed = RunSeed;
            _lastCatalog = ResolveCatalog();
            _runState = new BuildingRunState(Config);
            OfficeBuildingGenerator.GenerateMainFloorInto(_runState, _lastCatalog);
            _lastResult = _runState.ToResult(CollectOpenFrontier());

            if (SpawnRoomsOnCommit)
            {
                RoomSpawner.Spawn(_lastResult, Config, _lastCatalog, RoomDoorVisualPhase.Spawning);
                EnterGameplayDoorPhase();
            }

            RunStarted?.Invoke(_lastResult);
        }

        /// <summary>
        /// Shows frontier (<see cref="CellDoorState.Open"/>) doors open for an expansion/spawn pass.
        /// Logical door states on <see cref="BuildingRunState"/> are unchanged.
        /// </summary>
        public void EnterSpawningDoorPhase()
        {
            DoorVisualPhase = RoomDoorVisualPhase.Spawning;
            RefreshDoorVisuals();
        }

        /// <summary>
        /// Closes unconnected frontier doors for active gameplay. Connected passages stay open.
        /// </summary>
        public void EnterGameplayDoorPhase()
        {
            DoorVisualPhase = RoomDoorVisualPhase.Gameplay;
            RefreshDoorVisuals();
        }

        public void RefreshDoorVisuals()
        {
            if (_runState == null)
                return;

            OfficeBuildingGenerator.ReclassifyDoorStates(_runState, _lastCatalog ?? ResolveCatalog());
            RoomSpawner.ApplyDoorVisualPhase(DoorVisualPhase, _runState.Instances);
        }

        public ExpansionResult ExpandRun(uint? expansionSeedOverride = null) =>
            ExpandRun(Config.AdditionalRoomsOnExpand, expansionSeedOverride);

        public ExpansionResult ExpandRun(int additionalRoomCount, uint? expansionSeedOverride = null)
        {
            if (_runState == null || _runState.Instances.Count == 0)
            {
                Debug.LogWarning("[BuildingRunDirector] Start a run before expanding.");
                return ExpansionResult.NoChange(
                    _runState ?? new BuildingRunState(Config),
                    Array.Empty<DoorwaySlot>());
            }

            _lastCatalog = ResolveCatalog();
            uint expansionSeed = expansionSeedOverride ?? RunSeed;

            if (SpawnRoomsOnCommit)
                EnterSpawningDoorPhase();

            ExpansionResult expansion = OfficeBuildingGenerator.ExpandMainFloor(
                _runState,
                _lastCatalog,
                additionalRoomCount,
                expansionSeed);

            _lastResult = expansion.Snapshot;

            if (SpawnRoomsOnCommit)
            {
                if (expansion.AddedInstances.Count > 0)
                    RoomSpawner.SpawnAdditional(
                        expansion.AddedInstances,
                        Config,
                        _lastCatalog,
                        RoomDoorVisualPhase.Spawning);

                EnterGameplayDoorPhase();
            }

            RunExpanded?.Invoke(expansion);
            return expansion;
        }

        public void ClearRun()
        {
            _runState = null;
            _lastResult = null;
            _lastCatalog = null;
            RoomSpawner.ClearSpawned();
        }

        public void RespawnAllRooms()
        {
            if (_lastResult == null)
            {
                Debug.LogWarning("[BuildingRunDirector] No layout to respawn.");
                return;
            }

            RoomSpawner.Spawn(_lastResult, Config, _lastCatalog ?? ResolveCatalog(), DoorVisualPhase);
        }

        IReadOnlyList<DoorwaySlot> CollectOpenFrontier()
        {
            if (_runState == null)
                return Array.Empty<DoorwaySlot>();

            _runState.RestoreFrontier(
                _lastCatalog ?? ResolveCatalog(),
                out DoorwayFrontier frontier,
                out _);

            return new List<DoorwaySlot>(frontier.OpenSlots);
        }

        void EnsureRoomSpawner()
        {
            if (_roomSpawner == null)
                _roomSpawner = GetComponent<LevelGenRoomSpawner>();

            if (_roomSpawner == null)
                _roomSpawner = gameObject.AddComponent<LevelGenRoomSpawner>();
        }
    }
}

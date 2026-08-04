using System.Collections.Generic;
using THPerfection.LevelGen;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Commits <see cref="BuildingRunState"/> into ECS: layout blob + baked room prefab instances.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BuildingRunDirector))]
public sealed class BuildingLayoutEcsBridge : MonoBehaviour
{
    BuildingRunDirector _director;
    EntityManager _entityManager;
    Entity _singletonEntity;
    uint _layoutVersion;
    bool _pendingFullCommit;

    void Awake()
    {
        _director = GetComponent<BuildingRunDirector>();
    }

    void Update()
    {
        if (!_pendingFullCommit)
            return;

        BuildingRunState runState = _director.RunState;
        if (runState == null || runState.Instances.Count == 0)
        {
            _pendingFullCommit = false;
            return;
        }

        if (!TryEnsureWorld(out _))
            return;

        _pendingFullCommit = false;
        CommitFull();
    }

    void OnEnable()
    {
        if (_director == null)
            _director = GetComponent<BuildingRunDirector>();

        _director.RunStarted += OnRunStarted;
        _director.RunExpanded += OnRunExpanded;
        _director.RunCleared += OnRunCleared;
        _director.DoorVisualPhaseChanged += OnDoorVisualPhaseChanged;
        _director.LayoutCommitRequested += OnLayoutCommitRequested;
    }

    void OnDisable()
    {
        if (_director == null)
            return;

        _director.RunStarted -= OnRunStarted;
        _director.RunExpanded -= OnRunExpanded;
        _director.RunCleared -= OnRunCleared;
        _director.DoorVisualPhaseChanged -= OnDoorVisualPhaseChanged;
        _director.LayoutCommitRequested -= OnLayoutCommitRequested;
    }

    void OnRunStarted(BuildingGenerationResult result) =>
        CommitFull();

    void OnRunExpanded(ExpansionResult expansion) =>
        CommitExpansion(expansion.AddedInstances);

    void OnRunCleared() =>
        ClearLayout();

    void OnDoorVisualPhaseChanged()
    {
        if (_director.RunState == null)
            return;

        ApplyDoorVisualPhase(_director.DoorVisualPhase, _director.RunState.Instances);
    }

    void OnLayoutCommitRequested() =>
        CommitFull();

    public void CommitFull()
    {
        BuildingRunState runState = _director.RunState;
        if (runState == null || runState.Instances.Count == 0)
            return;

        if (!TryEnsureWorld(out EntityManager entityManager))
        {
            _pendingFullCommit = true;
            return;
        }

        _pendingFullCommit = false;
        _entityManager = entityManager;
        EnsureSingleton(in _director.Config);

        DestroyAllRoomEntities();
        SpawnRoomEntities(runState.Instances);
        ApplyDoorVisualPhase(RoomDoorVisualPhase.Gameplay, runState.Instances);
        SwapLayoutBlob(runState);
    }

    public void CommitExpansion(IReadOnlyList<RoomInstance> addedInstances)
    {
        BuildingRunState runState = _director.RunState;
        if (runState == null)
            return;

        if (!TryEnsureWorld(out EntityManager entityManager))
            return;

        _entityManager = entityManager;
        EnsureSingleton(in _director.Config);

        if (addedInstances != null && addedInstances.Count > 0)
        {
            SpawnRoomEntities(addedInstances);
            ApplyDoorVisualPhase(RoomDoorVisualPhase.Gameplay, runState.Instances);
        }

        SwapLayoutBlob(runState);
    }

    public void ApplyDoorVisualPhase(
        RoomDoorVisualPhase phase,
        IReadOnlyList<RoomInstance> instances)
    {
        if (!TryEnsureWorld(out EntityManager entityManager))
            return;

        if (_singletonEntity == Entity.Null || !entityManager.Exists(_singletonEntity))
            return;

        if (!entityManager.HasBuffer<BuildingRoomEntityEntry>(_singletonEntity))
            return;

        DynamicBuffer<BuildingRoomEntityEntry> registry =
            entityManager.GetBuffer<BuildingRoomEntityEntry>(_singletonEntity);

        BuildingRoomVisualUtility.ApplyDoorVisualsForRun(
            entityManager, registry, instances, phase);
    }

    public void ClearLayout()
    {
        if (!TryEnsureWorld(out EntityManager entityManager))
            return;

        _entityManager = entityManager;

        if (_singletonEntity == Entity.Null || !entityManager.Exists(_singletonEntity))
            return;

        DestroyAllRoomEntities();

        if (entityManager.HasComponent<BuildingLayoutBlobRef>(_singletonEntity))
        {
            var blobRef = entityManager.GetComponentData<BuildingLayoutBlobRef>(_singletonEntity);
            DisposeBlob(ref blobRef);
            entityManager.SetComponentData(_singletonEntity, blobRef);
        }

        entityManager.SetComponentData(_singletonEntity, new BuildingLayoutBlobRef
        {
            Layout        = default,
            LayoutVersion = 0,
        });

        _layoutVersion = 0;
        SignalLayoutChanged(0);
    }

    bool TryEnsureWorld(out EntityManager entityManager)
    {
        if (World.DefaultGameObjectInjectionWorld == null || !World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            Debug.LogWarning("[BuildingLayoutEcsBridge] No default ECS world — layout commit skipped.");
            entityManager = default;
            return false;
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        return true;
    }

    void EnsureSingleton(in BuildingGenConfig config)
    {
        if (_singletonEntity != Entity.Null && _entityManager.Exists(_singletonEntity))
        {
            _entityManager.SetComponentData(_singletonEntity, new BuildingSpatialConfig
            {
                CellSize   = math.max(0.01f, config.CellSize),
                MainFloorY = config.FloorY,
            });
            return;
        }

        _singletonEntity = _entityManager.CreateEntity(
            typeof(BuildingLayoutSingletonTag),
            typeof(BuildingSpatialConfig),
            typeof(BuildingLayoutBlobRef),
            typeof(BuildingLayoutChanged));

        _entityManager.SetComponentData(_singletonEntity, new BuildingSpatialConfig
        {
            CellSize   = math.max(0.01f, config.CellSize),
            MainFloorY = config.FloorY,
        });
        _entityManager.SetComponentData(_singletonEntity, new BuildingLayoutBlobRef
        {
            Layout        = default,
            LayoutVersion = 0,
        });
        _entityManager.SetComponentData(_singletonEntity, new BuildingLayoutChanged { LayoutVersion = 0 });
        _entityManager.SetComponentEnabled<BuildingLayoutChanged>(_singletonEntity, false);
        _entityManager.AddBuffer<BuildingRoomEntityEntry>(_singletonEntity);
    }

    void SwapLayoutBlob(BuildingRunState runState)
    {
        var blobRef = _entityManager.GetComponentData<BuildingLayoutBlobRef>(_singletonEntity);
        DisposeBlob(ref blobRef);

        blobRef.Layout        = BuildingLayoutBlobBuilder.Build(runState);
        blobRef.LayoutVersion = ++_layoutVersion;
        _entityManager.SetComponentData(_singletonEntity, blobRef);
        SignalLayoutChanged(blobRef.LayoutVersion);
    }

    void SignalLayoutChanged(uint version)
    {
        _entityManager.SetComponentData(_singletonEntity, new BuildingLayoutChanged { LayoutVersion = version });
        _entityManager.SetComponentEnabled<BuildingLayoutChanged>(_singletonEntity, true);
    }

    static void DisposeBlob(ref BuildingLayoutBlobRef blobRef)
    {
        if (blobRef.Layout.IsCreated)
            blobRef.Layout.Dispose();

        blobRef.Layout = default;
    }

    void DestroyAllRoomEntities()
    {
        if (_singletonEntity == Entity.Null || !_entityManager.Exists(_singletonEntity))
            return;

        if (!_entityManager.HasBuffer<BuildingRoomEntityEntry>(_singletonEntity))
            return;

        DynamicBuffer<BuildingRoomEntityEntry> registry =
            _entityManager.GetBuffer<BuildingRoomEntityEntry>(_singletonEntity);

        for (int i = 0; i < registry.Length; i++)
        {
            Entity roomEntity = registry[i].RoomEntity;
            if (roomEntity != Entity.Null && _entityManager.Exists(roomEntity))
                _entityManager.DestroyEntity(roomEntity);
        }

        registry.Clear();
    }

    void SpawnRoomEntities(IReadOnlyList<RoomInstance> instances)
    {
        if (instances == null || instances.Count == 0)
            return;

        RoomDoorVisualPhase phase = _director.DoorVisualPhase;
        BuildingGenConfig config = _director.Config;
        var pendingEntries = new List<BuildingRoomEntityEntry>();

        for (int i = 0; i < instances.Count; i++)
        {
            RoomInstance instance = instances[i];
            DynamicBuffer<BuildingRoomEntityEntry> registry =
                _entityManager.GetBuffer<BuildingRoomEntityEntry>(_singletonEntity);

            if (TryFindRoomEntity(registry, instance.Id, out Entity existing))
            {
                BuildingRoomVisualUtility.ConfigureRoomEntity(
                    _entityManager,
                    existing,
                    instance,
                    math.max(0.01f, config.CellSize),
                    config.FloorY,
                    new FixedString64Bytes(instance.TemplateId));
                BuildingRoomVisualUtility.ApplyDoorVisuals(
                    _entityManager, existing, instance, phase);
                continue;
            }

            Entity roomEntity = BuildingRoomVisualUtility.SpawnRoomVisual(
                _entityManager, instance, in config, phase);

            pendingEntries.Add(new BuildingRoomEntityEntry
            {
                RoomInstanceId = instance.Id,
                RoomEntity     = roomEntity,
            });
        }

        if (pendingEntries.Count == 0)
            return;

        DynamicBuffer<BuildingRoomEntityEntry> writeRegistry =
            _entityManager.GetBuffer<BuildingRoomEntityEntry>(_singletonEntity);

        for (int i = 0; i < pendingEntries.Count; i++)
            writeRegistry.Add(pendingEntries[i]);
    }

    static bool TryFindRoomEntity(
        DynamicBuffer<BuildingRoomEntityEntry> registry,
        int roomInstanceId,
        out Entity roomEntity)
    {
        for (int i = 0; i < registry.Length; i++)
        {
            if (registry[i].RoomInstanceId != roomInstanceId)
                continue;

            roomEntity = registry[i].RoomEntity;
            return true;
        }

        roomEntity = Entity.Null;
        return false;
    }
}

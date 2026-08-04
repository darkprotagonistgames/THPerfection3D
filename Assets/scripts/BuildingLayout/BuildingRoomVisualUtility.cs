using System.Collections.Generic;
using THPerfection.LevelGen;
using THPerfection.LevelGen.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Instantiates baked room prefab entities and applies door socket visuals.
/// </summary>
public static class BuildingRoomVisualUtility
{
    public static bool TryGetPrefabEntity(
        EntityManager entityManager,
        FixedString64Bytes templateId,
        out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RoomVisualPrefabRegistryTag>(),
            ComponentType.ReadOnly<RoomVisualPrefabEntry>());

        if (query.IsEmpty)
            return false;

        Entity registry = query.GetSingletonEntity();
        DynamicBuffer<RoomVisualPrefabEntry> entries =
            entityManager.GetBuffer<RoomVisualPrefabEntry>(registry);

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].TemplateId != templateId)
                continue;

            prefabEntity = entries[i].Prefab;
            return prefabEntity != Entity.Null;
        }

        return false;
    }

    public static Entity SpawnRoomVisual(
        EntityManager entityManager,
        RoomInstance instance,
        in BuildingGenConfig config,
        RoomDoorVisualPhase phase)
    {
        float cellSize = math.max(0.01f, config.CellSize);
        float floorY = config.FloorY;
        var templateId = new FixedString64Bytes(instance.TemplateId);

        Entity roomEntity;
        if (TryGetPrefabEntity(entityManager, templateId, out Entity prefabEntity))
        {
            roomEntity = entityManager.Instantiate(prefabEntity);
        }
        else
        {
            Debug.LogWarning(
                $"[BuildingLayout] No ECS prefab baked for template '{instance.TemplateId}'. "
                + "Add it to RoomCatalogAsset on BuildingRunDirector and rebake the subscene.");
            roomEntity = entityManager.CreateEntity(
                typeof(BuildingRoomTag),
                typeof(BuildingRoomInstance),
                typeof(LocalTransform));
        }

        ConfigureRoomEntity(entityManager, roomEntity, instance, cellSize, floorY, templateId);
        ApplyDoorVisuals(entityManager, roomEntity, instance, phase);
        return roomEntity;
    }

    public static void ConfigureRoomEntity(
        EntityManager entityManager,
        Entity roomEntity,
        RoomInstance instance,
        float cellSize,
        float floorY,
        FixedString64Bytes templateId)
    {
        if (!entityManager.HasComponent<BuildingRoomTag>(roomEntity))
            entityManager.AddComponent<BuildingRoomTag>(roomEntity);

        if (!entityManager.HasComponent<BuildingRoomInstance>(roomEntity))
            entityManager.AddComponent<BuildingRoomInstance>(roomEntity);

        entityManager.SetComponentData(roomEntity, new BuildingRoomInstance
        {
            RoomInstanceId = instance.Id,
            Floor          = instance.Floor,
            Origin         = instance.Origin,
            Rotation       = instance.Rotation,
            TemplateId     = templateId,
        });

        float3 position = new(instance.Origin.x * cellSize, floorY, instance.Origin.y * cellSize);
        quaternion rotation = quaternion.RotateY((int)instance.Rotation * math.PI * 0.5f);
        entityManager.SetComponentData(roomEntity, LocalTransform.FromPositionRotation(position, rotation));
    }

    public static void ApplyDoorVisuals(
        EntityManager entityManager,
        Entity roomEntity,
        RoomInstance instance,
        RoomDoorVisualPhase phase)
    {
        if (entityManager.HasComponent<BuildingRoomVisualPhaseState>(roomEntity))
            entityManager.SetComponentData(roomEntity, new BuildingRoomVisualPhaseState { Phase = phase });
        else
            entityManager.AddComponentData(roomEntity, new BuildingRoomVisualPhaseState { Phase = phase });

        if (!entityManager.HasBuffer<LinkedEntityGroup>(roomEntity))
            return;

        DynamicBuffer<LinkedEntityGroup> linked = entityManager.GetBuffer<LinkedEntityGroup>(roomEntity);
        int linkedCount = linked.Length;
        if (linkedCount == 0)
            return;

        // Snapshot children before SetVisualEnabled structural changes invalidate the buffer handle.
        var children = new Entity[linkedCount];
        for (int i = 0; i < linkedCount; i++)
            children[i] = linked[i].Value;

        for (int i = 0; i < linkedCount; i++)
        {
            Entity child = children[i];
            if (TryApplyViaCompanion(entityManager, child, instance, phase))
                continue;

            if (!entityManager.HasComponent<RoomDoorSocketBaked>(child))
                continue;

            RoomDoorSocketBaked socket = entityManager.GetComponentData<RoomDoorSocketBaked>(child);
            int2 worldCell = instance.Origin
                + GridTransforms.RotateCell(socket.LocalCell, instance.Rotation);
            DoorSide worldSide = GridTransforms.RotateSide(socket.Side, instance.Rotation);
            var key = new DoorEdgeKey(instance.Floor, worldCell, worldSide);

            bool showOpen = instance.TryGetDoorState(key, out CellDoorState logical)
                && RoomDoorVisualRules.ShouldShowOpen(logical, phase);

            SetDoorVisualGroup(
                entityManager,
                roomEntity,
                child,
                socket.OpenVisual,
                socket.ClosedVisual,
                showOpen);
        }
    }

    static bool TryApplyViaCompanion(
        EntityManager entityManager,
        Entity socketEntity,
        RoomInstance instance,
        RoomDoorVisualPhase phase)
    {
        return false;
    }

    static void SetDoorVisualGroup(
        EntityManager entityManager,
        Entity roomEntity,
        Entity socketEntity,
        Entity openVisualRoot,
        Entity closedVisualRoot,
        bool showOpen)
    {
        if (entityManager.HasBuffer<RoomDoorOpenVisualEntity>(socketEntity))
        {
            DynamicBuffer<RoomDoorOpenVisualEntity> openEntities =
                entityManager.GetBuffer<RoomDoorOpenVisualEntity>(socketEntity);
            int count = openEntities.Length;
            if (count > 0)
            {
                var temp = new Entity[count];
                for (int i = 0; i < count; i++)
                    temp[i] = openEntities[i].Entity;

                for (int i = 0; i < count; i++)
                    SetRenderingEnabled(entityManager, temp[i], showOpen);
            }
        }
        else
        {
            SetVisualSubtreeEnabled(entityManager, roomEntity, openVisualRoot, showOpen);
        }

        if (entityManager.HasBuffer<RoomDoorClosedVisualEntity>(socketEntity))
        {
            DynamicBuffer<RoomDoorClosedVisualEntity> closedEntities =
                entityManager.GetBuffer<RoomDoorClosedVisualEntity>(socketEntity);
            int count = closedEntities.Length;
            if (count > 0)
            {
                var temp = new Entity[count];
                for (int i = 0; i < count; i++)
                    temp[i] = closedEntities[i].Entity;

                for (int i = 0; i < count; i++)
                    SetRenderingEnabled(entityManager, temp[i], !showOpen);
            }
        }
        else
        {
            SetVisualSubtreeEnabled(entityManager, roomEntity, closedVisualRoot, !showOpen);
        }
    }

    static void SetVisualSubtreeEnabled(
        EntityManager entityManager,
        Entity roomEntity,
        Entity visualRoot,
        bool enabled)
    {
        if (visualRoot == Entity.Null || !entityManager.Exists(visualRoot))
            return;

        if (entityManager.HasBuffer<LinkedEntityGroup>(roomEntity))
        {
            DynamicBuffer<LinkedEntityGroup> roomLinked = entityManager.GetBuffer<LinkedEntityGroup>(roomEntity);
            int count = roomLinked.Length;
            var entities = new Entity[count];
            for (int i = 0; i < count; i++)
                entities[i] = roomLinked[i].Value;

            for (int i = 0; i < count; i++)
            {
                Entity candidate = entities[i];
                if (IsSameOrDescendantOf(entityManager, candidate, visualRoot))
                    SetRenderingEnabled(entityManager, candidate, enabled);
            }

            return;
        }

        SetVisualEnabled(entityManager, visualRoot, enabled);
    }

    static bool IsSameOrDescendantOf(EntityManager entityManager, Entity entity, Entity ancestor)
    {
        if (entity == ancestor)
            return true;

        if (!entityManager.HasComponent<Parent>(entity))
            return false;

        Entity parent = entityManager.GetComponentData<Parent>(entity).Value;
        if (parent == Entity.Null || !entityManager.Exists(parent))
            return false;

        return IsSameOrDescendantOf(entityManager, parent, ancestor);
    }

    public static void ApplyDoorVisualsForRun(
        EntityManager entityManager,
        DynamicBuffer<BuildingRoomEntityEntry> registry,
        IReadOnlyList<RoomInstance> instances,
        RoomDoorVisualPhase phase)
    {
        if (instances == null || instances.Count == 0)
            return;

        var instanceById = new Dictionary<int, RoomInstance>();
        for (int i = 0; i < instances.Count; i++)
            instanceById[instances[i].Id] = instances[i];

        var entries = new BuildingRoomEntityEntry[registry.Length];
        for (int i = 0; i < registry.Length; i++)
            entries[i] = registry[i];

        for (int i = 0; i < entries.Length; i++)
        {
            BuildingRoomEntityEntry entry = entries[i];
            if (!instanceById.TryGetValue(entry.RoomInstanceId, out RoomInstance instance))
                continue;

            if (entry.RoomEntity == Entity.Null || !entityManager.Exists(entry.RoomEntity))
                continue;

            ApplyDoorVisuals(entityManager, entry.RoomEntity, instance, phase);
        }
    }

    static void SetVisualEnabled(EntityManager entityManager, Entity visualEntity, bool enabled)
    {
        if (visualEntity == Entity.Null || !entityManager.Exists(visualEntity))
            return;

        if (entityManager.HasBuffer<LinkedEntityGroup>(visualEntity))
        {
            DynamicBuffer<LinkedEntityGroup> linked = entityManager.GetBuffer<LinkedEntityGroup>(visualEntity);
            int count = linked.Length;
            var entities = new Entity[count];
            for (int i = 0; i < count; i++)
                entities[i] = linked[i].Value;

            for (int i = 0; i < count; i++)
                SetRenderingEnabled(entityManager, entities[i], enabled);
            return;
        }

        SetRenderingEnabled(entityManager, visualEntity, enabled);
    }

    static void SetRenderingEnabled(EntityManager entityManager, Entity entity, bool enabled)
    {
        if (entity == Entity.Null || !entityManager.Exists(entity))
            return;

        if (enabled)
        {
            if (entityManager.HasComponent<DisableRendering>(entity))
                entityManager.RemoveComponent<DisableRendering>(entity);
        }
        else if (!entityManager.HasComponent<DisableRendering>(entity))
        {
            entityManager.AddComponent<DisableRendering>(entity);
        }
    }
}

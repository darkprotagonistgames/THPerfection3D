using THPerfection.LevelGen.Authoring;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Bakes door socket markers on room prefabs for ECS door open/closed toggling.
/// </summary>
public sealed class DoorSocketMarkerBaker : Baker<DoorSocketMarker>
{
    public override void Bake(DoorSocketMarker authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        Entity openVisual = Entity.Null;
        Entity closedVisual = Entity.Null;
        DynamicBuffer<RoomDoorOpenVisualEntity> openEntities = AddBuffer<RoomDoorOpenVisualEntity>(entity);
        DynamicBuffer<RoomDoorClosedVisualEntity> closedEntities = AddBuffer<RoomDoorClosedVisualEntity>(entity);

        if (authoring.OpenVisual != null)
        {
            DependsOn(authoring.OpenVisual);
            openVisual = GetEntity(authoring.OpenVisual, VisualTransformUsage);
            AppendOpenHierarchy(authoring.OpenVisual, openEntities);
        }

        if (authoring.ClosedVisual != null)
        {
            DependsOn(authoring.ClosedVisual);
            closedVisual = GetEntity(authoring.ClosedVisual, VisualTransformUsage);
            AppendClosedHierarchy(authoring.ClosedVisual, closedEntities);
        }

        AddComponent(entity, new RoomDoorSocketBaked
        {
            LocalCell    = authoring.LocalCell,
            Side         = authoring.Side,
            OpenVisual   = openVisual,
            ClosedVisual = closedVisual,
        });
    }

    const TransformUsageFlags VisualTransformUsage =
        TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable;

    void AppendOpenHierarchy(GameObject root, DynamicBuffer<RoomDoorOpenVisualEntity> buffer)
    {
        if (root == null)
            return;

        buffer.Add(new RoomDoorOpenVisualEntity { Entity = GetEntity(root, VisualTransformUsage) });

        Transform transform = root.transform;
        for (int i = 0; i < transform.childCount; i++)
            AppendOpenHierarchy(transform.GetChild(i).gameObject, buffer);
    }

    void AppendClosedHierarchy(GameObject root, DynamicBuffer<RoomDoorClosedVisualEntity> buffer)
    {
        if (root == null)
            return;

        buffer.Add(new RoomDoorClosedVisualEntity { Entity = GetEntity(root, VisualTransformUsage) });

        Transform transform = root.transform;
        for (int i = 0; i < transform.childCount; i++)
            AppendClosedHierarchy(transform.GetChild(i).gameObject, buffer);
    }
}

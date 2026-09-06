using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Inactive door visual GameObjects receive <see cref="Disabled"/> during bake.
/// Runtime door toggling only adds/removes <c>DisableRendering</c>, which cannot
/// show Disabled entities. Strip Disabled from baked door visuals so instances
/// can be revealed after spawn.
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateInGroup(typeof(PostBakingSystemGroup))]
public partial struct RoomDoorVisualBakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityQuery query = SystemAPI.QueryBuilder()
            .WithAll<RoomDoorSocketBaked>()
            .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
            .Build();

        NativeArray<Entity> sockets = query.ToEntityArray(Allocator.Temp);
        var toEnable = new NativeList<Entity>(Allocator.Temp);
        EntityManager entityManager = state.EntityManager;

        for (int i = 0; i < sockets.Length; i++)
            CollectVisuals(entityManager, sockets[i], ref toEnable);

        for (int i = 0; i < toEnable.Length; i++)
        {
            Entity visual = toEnable[i];
            if (visual == Entity.Null || !entityManager.Exists(visual))
                continue;

            if (entityManager.HasComponent<Disabled>(visual))
                entityManager.RemoveComponent<Disabled>(visual);
        }

        toEnable.Dispose();
        sockets.Dispose();
    }

    static void CollectVisuals(
        EntityManager entityManager,
        Entity socket,
        ref NativeList<Entity> toEnable)
    {
        if (entityManager.HasComponent<RoomDoorSocketBaked>(socket))
        {
            RoomDoorSocketBaked baked = entityManager.GetComponentData<RoomDoorSocketBaked>(socket);
            if (baked.OpenVisual != Entity.Null)
                toEnable.Add(baked.OpenVisual);
            if (baked.ClosedVisual != Entity.Null)
                toEnable.Add(baked.ClosedVisual);
        }

        if (entityManager.HasBuffer<RoomDoorOpenVisualEntity>(socket))
        {
            DynamicBuffer<RoomDoorOpenVisualEntity> openEntities =
                entityManager.GetBuffer<RoomDoorOpenVisualEntity>(socket);
            for (int i = 0; i < openEntities.Length; i++)
                toEnable.Add(openEntities[i].Entity);
        }

        if (entityManager.HasBuffer<RoomDoorClosedVisualEntity>(socket))
        {
            DynamicBuffer<RoomDoorClosedVisualEntity> closedEntities =
                entityManager.GetBuffer<RoomDoorClosedVisualEntity>(socket);
            for (int i = 0; i < closedEntities.Length; i++)
                toEnable.Add(closedEntities[i].Entity);
        }
    }
}

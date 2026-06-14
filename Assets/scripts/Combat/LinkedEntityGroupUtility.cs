using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Links runtime spawns under a controller via a child spawn anchor and that anchor's
/// <see cref="LinkedEntityGroup"/> (not the controller root's group). Individual spawns can
/// expire via <see cref="TtlSystem"/> without affecting siblings; controller death still
/// cascades because the anchor is a child in the controller's <see cref="LinkedEntityGroup"/>.
/// </summary>
public static class LinkedEntityGroupUtility
{
    /// <summary>
    /// Walks transform parents to the hierarchy root that owns the baked <see cref="LinkedEntityGroup"/>.
    /// </summary>
    public static Entity FindGroupRoot(in EntityManager entityManager, Entity entity)
    {
        Entity root = entity;
        while (entityManager.HasComponent<Parent>(root))
            root = entityManager.GetComponentData<Parent>(root).Value;

        if (entityManager.HasBuffer<LinkedEntityGroup>(root))
            return root;

        return entity;
    }

    /// <summary>
    /// Instantiates <paramref name="prefab"/> at <paramref name="transform"/>, parents it under a
    /// per-controller spawn anchor, and registers it on the anchor's <see cref="LinkedEntityGroup"/>.
    /// </summary>
    public static Entity InstantiateAsSpawnChild(
        in EntityManager entityManager,
        Entity controllerEntity,
        Entity prefab,
        in LocalTransform transform)
    {
        Entity groupRoot = FindGroupRoot(entityManager, controllerEntity);
        Entity anchor = EnsureSpawnAnchor(entityManager, groupRoot);

        Entity instance = entityManager.Instantiate(prefab);
        entityManager.SetComponentData(instance, transform);
        AddChild(entityManager, anchor, instance);

        DynamicBuffer<LinkedEntityGroup> anchorGroup = entityManager.GetBuffer<LinkedEntityGroup>(anchor);
        anchorGroup.Add(new LinkedEntityGroup { Value = instance });

        return instance;
    }

    /// <summary>
    /// Like <see cref="InstantiateAsSpawnChild"/> but also sets <see cref="AttackSpawnContext"/> on the instance.
    /// </summary>
    public static Entity InstantiateAsSpawnChildWithContext(
        in EntityManager entityManager,
        Entity controllerEntity,
        Entity prefab,
        in LocalTransform transform,
        Entity originEntity,
        Entity targetEntity)
    {
        Entity instance = InstantiateAsSpawnChild(entityManager, controllerEntity, prefab, transform);

        var context = new AttackSpawnContext
        {
            OriginEntity = originEntity,
            TargetEntity = targetEntity,
        };

        if (entityManager.HasComponent<AttackSpawnContext>(instance))
            entityManager.SetComponentData(instance, context);
        else
            entityManager.AddComponentData(instance, context);

        return instance;
    }

    static Entity EnsureSpawnAnchor(in EntityManager entityManager, Entity groupRoot)
    {
        if (entityManager.HasBuffer<Child>(groupRoot))
        {
            DynamicBuffer<Child> children = entityManager.GetBuffer<Child>(groupRoot);
            for (int i = 0; i < children.Length; i++)
            {
                Entity child = children[i].Value;
                if (entityManager.HasComponent<SpawnGroupAnchorTag>(child))
                    return child;
            }
        }

        Entity anchor = entityManager.CreateEntity();
        entityManager.AddComponent<SpawnGroupAnchorTag>(anchor);
        entityManager.AddComponentData(anchor, LocalTransform.Identity);
        AddChild(entityManager, groupRoot, anchor);

        DynamicBuffer<LinkedEntityGroup> anchorGroup = entityManager.AddBuffer<LinkedEntityGroup>(anchor);
        anchorGroup.Add(anchor);

        if (entityManager.HasBuffer<LinkedEntityGroup>(groupRoot))
        {
            DynamicBuffer<LinkedEntityGroup> rootGroup = entityManager.GetBuffer<LinkedEntityGroup>(groupRoot);
            rootGroup.Add(new LinkedEntityGroup { Value = anchor });
        }

        return anchor;
    }

    static void AddChild(in EntityManager entityManager, Entity parent, Entity child)
    {
        entityManager.AddComponentData(child, new Parent { Value = parent });

        if (!entityManager.HasBuffer<Child>(parent))
            entityManager.AddBuffer<Child>(parent);

        entityManager.GetBuffer<Child>(parent).Add(new Child { Value = child });
    }
}

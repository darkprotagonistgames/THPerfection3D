using Rukhanka;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

/// <summary>Helpers for DeathSystem to preserve tagged visuals and remove the character root.</summary>
public static class DeathHierarchyUtility
{
    static readonly FastAnimatorParameter DeathTrigger = new("death");
    public static Entity FindKeepAfterDeathDescendant(
        Entity root,
        EntityManager entityManager,
        ComponentLookup<KeepAfterDeathTag> keepTagLookup,
        BufferLookup<Child> childLookup)
    {
        if (!childLookup.HasBuffer(root))
            return Entity.Null;

        var pending = new NativeList<Entity>(Allocator.Temp);
        foreach (Child child in childLookup[root])
            pending.Add(child.Value);

        Entity found = Entity.Null;

        while (pending.Length > 0)
        {
            Entity current = pending[pending.Length - 1];
            pending.RemoveAtSwapBack(pending.Length - 1);

            if (keepTagLookup.HasComponent(current))
            {
                found = current;
                break;
            }

            if (childLookup.HasBuffer(current))
            {
                foreach (Child child in childLookup[current])
                    pending.Add(child.Value);
            }
        }

        pending.Dispose();
        return found;
    }

    public static void PreserveVisualAndDestroyCharacter(
        Entity dead,
        Entity keepEntity,
        EntityManager entityManager,
        EntityCommandBuffer ecb)
    {
        if (keepEntity != Entity.Null && entityManager.Exists(keepEntity))
        {
            var keepSubtree = new NativeHashSet<Entity>(32, Allocator.Temp);
            CollectDescendants(keepEntity, entityManager, keepSubtree);
            RemoveFromLinkedEntityGroup(dead, keepSubtree, entityManager);
            keepSubtree.Dispose();

            DetachPreserveWorldTransform(keepEntity, entityManager, ecb);
            TryTriggerDeathAnimation(keepEntity, entityManager);
        }

        // Baked prefab roots use LinkedEntityGroup; destroying any member destroys the whole
        // group, so the keep subtree must be removed first. Then one destroy clears the rest.
        if (entityManager.HasBuffer<LinkedEntityGroup>(dead))
            ecb.DestroyEntity(dead);
        else
            DestroyEntityAndChildren(dead, entityManager, ecb);
    }

    static void CollectDescendants(Entity root, EntityManager entityManager, NativeHashSet<Entity> entities)
    {
        if (!entities.Add(root))
            return;

        if (!entityManager.HasBuffer<Child>(root))
            return;

        foreach (Child child in entityManager.GetBuffer<Child>(root))
            CollectDescendants(child.Value, entityManager, entities);
    }

    static void RemoveFromLinkedEntityGroup(
        Entity groupRoot,
        NativeHashSet<Entity> entitiesToRemove,
        EntityManager entityManager)
    {
        if (!entityManager.HasBuffer<LinkedEntityGroup>(groupRoot))
            return;

        DynamicBuffer<LinkedEntityGroup> group = entityManager.GetBuffer<LinkedEntityGroup>(groupRoot);
        for (int i = group.Length - 1; i >= 0; i--)
        {
            if (entitiesToRemove.Contains(group[i].Value))
                group.RemoveAtSwapBack(i);
        }
    }

    static void DetachPreserveWorldTransform(Entity entity, EntityManager entityManager, EntityCommandBuffer ecb)
    {
        if (!entityManager.HasComponent<Parent>(entity))
            return;

        LocalToWorld worldTransform = entityManager.GetComponentData<LocalToWorld>(entity);
        LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);
        Entity parent = entityManager.GetComponentData<Parent>(entity).Value;

        if (entityManager.HasBuffer<Child>(parent))
        {
            DynamicBuffer<Child> siblings = entityManager.GetBuffer<Child>(parent);
            for (int i = 0; i < siblings.Length; i++)
            {
                if (siblings[i].Value != entity)
                    continue;

                siblings.RemoveAtSwapBack(i);
                break;
            }
        }

        ecb.RemoveComponent<Parent>(entity);
        ecb.SetComponent(entity, LocalTransform.FromPositionRotationScale(
            worldTransform.Position,
            worldTransform.Rotation,
            localTransform.Scale));
    }

    static void TryTriggerDeathAnimation(Entity entity, EntityManager entityManager)
    {
        if (!entityManager.HasBuffer<AnimatorControllerParameterComponent>(entity))
            return;
        if (!entityManager.HasComponent<AnimatorControllerParameterIndexTableComponent>(entity))
            return;

        DynamicBuffer<AnimatorControllerParameterComponent> parameters =
            entityManager.GetBuffer<AnimatorControllerParameterComponent>(entity);
        AnimatorControllerParameterIndexTableComponent indexTable =
            entityManager.GetComponentData<AnimatorControllerParameterIndexTableComponent>(entity);

        var animatorParameters = new AnimatorParametersAspect(parameters, indexTable);
        if (animatorParameters.HasParameter(DeathTrigger))
            animatorParameters.SetTrigger(DeathTrigger);
    }

    static void DestroyEntityAndChildren(Entity root, EntityManager entityManager, EntityCommandBuffer ecb)
    {
        if (entityManager.HasBuffer<Child>(root))
        {
            DynamicBuffer<Child> children = entityManager.GetBuffer<Child>(root);
            for (int i = children.Length - 1; i >= 0; i--)
                DestroyEntityAndChildren(children[i].Value, entityManager, ecb);
        }

        ecb.DestroyEntity(root);
    }
}

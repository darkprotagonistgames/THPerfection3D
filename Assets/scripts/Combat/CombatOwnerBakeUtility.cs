using THPerfection.GeneratedEvents;
using Unity.Entities;
using UnityEngine;

/// <summary>Shared bake helpers for combat collider authoring.</summary>
public static class CombatOwnerBakeUtility
{
    public static Entity ResolveOwnerEntity<TAuthoring>(
        Baker<TAuthoring> baker,
        Transform ownerOverride,
        TAuthoring authoring)
        where TAuthoring : Component
    {
        Transform ownerTransform = ownerOverride;
        if (ownerTransform == null)
            ownerTransform = FindCharacterRootTransform(authoring.transform);

        if (ownerTransform == null)
        {
            throw new System.InvalidOperationException(
                $"{authoring.GetType().Name} on '{authoring.gameObject.name}' needs OwnerOverride or a parent with CharacterSettings.");
        }

        baker.DependsOn(ownerTransform.gameObject);
        if (ownerTransform.TryGetComponent<CharacterSettings>(out var characterSettings))
            baker.DependsOn(characterSettings);

        return baker.GetEntity(ownerTransform, TransformUsageFlags.Dynamic);
    }

    static Transform FindCharacterRootTransform(Transform from)
    {
        Transform t = from.parent;
        while (t != null)
        {
            if (t.GetComponent<CharacterSettings>() != null)
                return t;

            t = t.parent;
        }

        Transform root = from.root;
        if (root != null && root.GetComponent<CharacterSettings>() != null)
            return root;

        return null;
    }

    public static void AddTargetableTag<TAuthoring>(Baker<TAuthoring> baker, Entity entity, targetable category)
        where TAuthoring : Component
    {
        switch (category)
        {
            case targetable.wall:
                baker.AddComponent(entity, new targetablewallTag());
                break;
            case targetable.zombi:
                baker.AddComponent(entity, new targetablezombiTag());
                break;
            case targetable.player:
                baker.AddComponent(entity, new targetableplayerTag());
                break;
        }
    }
}

using THPerfection.GeneratedEvents;
using Unity.Collections;
using Unity.Entities;

/// <summary>Resolves <see cref="targetable"/> for damage events from victim entities.</summary>
public static class CombatTargetableUtility
{
    public static targetable ResolveCategory(EntityManager entityManager, EntityQuery hurtboxQuery, Entity victim)
    {
        if (entityManager.HasComponent<targetableplayerTag>(victim))
            return targetable.player;

        if (entityManager.HasComponent<targetablezombiTag>(victim))
            return targetable.zombi;

        if (entityManager.HasComponent<targetablewallTag>(victim))
            return targetable.wall;

        var entities = hurtboxQuery.ToEntityArray(Allocator.Temp);
        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity hurtboxEntity = entities[i];
                if (entityManager.GetComponentData<HurtboxOwner>(hurtboxEntity).Value != victim)
                    continue;

                return entityManager.GetComponentData<HurtboxData>(hurtboxEntity).Category;
            }
        }
        finally
        {
            entities.Dispose();
        }

        return targetable.zombi;
    }
}

using Unity.Entities;

/// <summary>
/// Origin and target entities for a spawned attack instance (projectile, hitbox volume, etc.).
/// Set at runtime by attack spawn systems such as <see cref="TargetedAttackSystem"/>.
/// </summary>
public struct AttackSpawnContext : IComponentData
{
    public Entity OriginEntity;
    public Entity TargetEntity;
}

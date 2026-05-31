using THPerfection.GeneratedEvents;
using Unity.Entities;

/// <summary>Attack volume data baked onto trigger collider entities.</summary>
public struct HitboxData : IComponentData
{
    public float Damage;
    public wepon WeaponType;
}

/// <summary>Character root that owns this hitbox (attacker); used for self-hit checks.</summary>
public struct HitboxOwner : IComponentData
{
    public Entity Value;
}

/// <summary>Passive receiver volume baked onto trigger collider entities.</summary>
public struct HurtboxData : IComponentData
{
    public targetable Category;
}

/// <summary>Character root that owns this hurtbox; becomes <see cref="damageEvent.Sender"/>.</summary>
public struct HurtboxOwner : IComponentData
{
    public Entity Value;
}

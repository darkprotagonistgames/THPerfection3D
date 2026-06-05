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
    /// <summary>Per-attacker re-hit cooldown tracked via <see cref="HurtboxInvulnerabilityLink"/> TTL entities.</summary>
    public float InvulnerabilitySeconds;
}

/// <summary>Points at a TTL invulnerability record entity owned by a hurtbox.</summary>
public struct HurtboxInvulnerabilityLink : IBufferElementData
{
    public Entity RecordEntity;
}

/// <summary>
/// TTL entity for an active invulnerability window; <see cref="Target"/> is the attacker root.
/// Not parented to the hurtbox so transform systems ignore it.
/// </summary>
public struct HurtboxInvulnerabilityRecord : IComponentData
{
    public Entity Target;
}

/// <summary>Character root that owns this hurtbox; becomes <see cref="damageEvent.Sender"/>.</summary>
public struct HurtboxOwner : IComponentData
{
    public Entity Value;
}

/// <summary>
/// Temporary tag added at spawn to prevent instant death from overlapping hitboxes
/// (e.g. if the player's hitbox is active at the spawn origin).
/// </summary>
public struct SpawnInvulnerabilityTag : IComponentData { }

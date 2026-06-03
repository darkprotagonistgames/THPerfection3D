using UnityEngine;

/// <summary>Layer masks for hitbox / hurtbox physics filtering (see Project Settings → Physics).</summary>
public static class CombatLayers
{
    public const int HurtBoxLayer = (int)Layers.HurtBox;
    public const int HitBoxLayer = (int)Layers.HitBox;
    public const int PlayerHurtBoxLayer = (int)Layers.PlayerHurtBox;
    public const int PlayerHitBoxLayer = (int)Layers.PlayerHitBox;

    /// <summary>Enemy hurtbox: only overlap <see cref="Layers.PlayerHitBox"/>.</summary>
    public static readonly int HurtBoxIncludeMask = 1 << PlayerHitBoxLayer;

    /// <summary>Enemy hitbox: only overlap <see cref="Layers.PlayerHurtBox"/>.</summary>
    public static readonly int HitBoxIncludeMask = 1 << PlayerHurtBoxLayer;

    /// <summary>Player hurtbox: only overlap <see cref="Layers.HitBox"/>.</summary>
    public static readonly int PlayerHurtBoxIncludeMask = 1 << HitBoxLayer;

    /// <summary>Player hitbox: only overlap <see cref="Layers.HurtBox"/>.</summary>
    public static readonly int PlayerHitBoxIncludeMask = 1 << HurtBoxLayer;

    public static int GetHitBoxIncludeMask(int hitBoxLayer) =>
        hitBoxLayer == PlayerHitBoxLayer ? PlayerHitBoxIncludeMask : HitBoxIncludeMask;

    public static int GetHurtBoxIncludeMask(int hurtBoxLayer) =>
        hurtBoxLayer == PlayerHurtBoxLayer ? PlayerHurtBoxIncludeMask : HurtBoxIncludeMask;

    /// <summary>Enemy <see cref="Layers.HurtBox"/> bodies (zombies, etc.).</summary>
    public static readonly LayerMask EnemyHurtboxLayers = 1 << HurtBoxLayer;

    /// <summary>Player <see cref="Layers.PlayerHurtBox"/> bodies.</summary>
    public static readonly LayerMask PlayerHurtboxLayers = 1 << PlayerHurtBoxLayer;

    /// <summary>All combat hurtbox layers.</summary>
    public static readonly LayerMask AllHurtboxLayers = EnemyHurtboxLayers | PlayerHurtboxLayers;
}

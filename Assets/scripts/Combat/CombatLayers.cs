/// <summary>Layer masks for hitbox / hurtbox physics filtering (see Project Settings → Physics).</summary>
public static class CombatLayers
{
    public const int HurtBoxLayer = (int)Layers.HurtBox;
    public const int HitBoxLayer = (int)Layers.HitBox;

    /// <summary>Only collide with <see cref="Layers.HitBox"/>.</summary>
    public static readonly int HurtBoxIncludeMask = 1 << HitBoxLayer;

    /// <summary>Only collide with <see cref="Layers.HurtBox"/>.</summary>
    public static readonly int HitBoxIncludeMask = 1 << HurtBoxLayer;
}

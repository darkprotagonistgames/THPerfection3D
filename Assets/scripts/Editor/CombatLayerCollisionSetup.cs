using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures combat hitbox / hurtbox layers only collide across teams
/// (HitBox ↔ PlayerHurtBox, PlayerHitBox ↔ HurtBox).
/// </summary>
[InitializeOnLoad]
public static class CombatLayerCollisionSetup
{
    static CombatLayerCollisionSetup()
    {
        Apply();
    }

    [MenuItem("THPerfection/Combat/Apply Combat Layer Matrix")]
    public static void ApplyFromMenu() => Apply();

    static void Apply()
    {
        SetLayerCollisions(CombatLayers.HurtBoxLayer, CombatLayers.PlayerHitBoxLayer);
        SetLayerCollisions(CombatLayers.HitBoxLayer, CombatLayers.PlayerHurtBoxLayer);
        SetLayerCollisions(CombatLayers.PlayerHurtBoxLayer, CombatLayers.HitBoxLayer);
        SetLayerCollisions(CombatLayers.PlayerHitBoxLayer, CombatLayers.HurtBoxLayer);
    }

    static void SetLayerCollisions(int layer, int collidesWithLayer)
    {
        if (layer < 0 || collidesWithLayer < 0)
            return;

        for (int other = 0; other < 32; other++)
            Physics.IgnoreLayerCollision(layer, other, other != collidesWithLayer);
    }
}

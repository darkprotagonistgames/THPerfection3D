using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures HitBox and HurtBox layers only collide with each other (not with themselves or Default).
/// </summary>
[InitializeOnLoad]
public static class CombatLayerCollisionSetup
{
    static CombatLayerCollisionSetup()
    {
        Apply();
    }

    [MenuItem("THPerfection/Combat/Apply HitBox ↔ HurtBox Layer Matrix")]
    public static void ApplyFromMenu() => Apply();

    static void Apply()
    {
        int hurt = CombatLayers.HurtBoxLayer;
        int hit = CombatLayers.HitBoxLayer;

        if (hurt < 0 || hit < 0)
            return;

        for (int layer = 0; layer < 32; layer++)
        {
            Physics.IgnoreLayerCollision(hurt, layer, layer != hit);
            Physics.IgnoreLayerCollision(hit, layer, layer != hurt);
        }
    }
}

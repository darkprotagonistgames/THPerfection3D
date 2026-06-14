using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Runtime targeted-attack config baked from <see cref="TargetedAttackAuthoring"/>.
/// </summary>
public struct TargetedAttackData : IComponentData
{
    public Entity AttackPrefab;
    public float Range;
    public float Cooldown;
    public float CooldownRemaining;

    /// <summary>Bit mask of physics layers (1 &lt;&lt; layer index) for valid hurtbox targets.</summary>
    public uint TargetLayerMask;
}

/// <summary>
/// Auto-spawns <see cref="TargetedAttackData.AttackPrefab"/> at this entity's position and rotation when the
/// closest hurtbox on a matching physics layer is within <see cref="TargetedAttackData.Range"/>.
/// Spawned entities receive <see cref="AttackSpawnContext"/> with origin and target entity references.
/// Uses <see cref="TargetedAttackData.Cooldown"/> between spawns.
/// </summary>
public class TargetedAttackAuthoring : MonoBehaviour
{
    [Tooltip("Entity prefab to instantiate (projectile, hitbox volume, etc.).")]
    public GameObject AttackPrefab;

    [Header("Targeting")]
    [Min(0f)]
    [Tooltip("Maximum horizontal distance to a target hurtbox (XZ plane).")]
    public float Range = 8f;

    [Tooltip("Physics layers for hurtbox colliders that count as targets (e.g. HurtBox, PlayerHurtBox).")]
    public LayerMask TargetLayers = CombatLayers.AllHurtboxLayers;

    [Header("Timing")]
    [Min(0f)]
    [Tooltip("Seconds between successful spawns.")]
    public float Cooldown = 1f;

    void OnValidate()
    {
        if (TargetLayers.value == 0)
            TargetLayers = CombatLayers.AllHurtboxLayers;
    }

    public class Baker : Baker<TargetedAttackAuthoring>
    {
        public override void Bake(TargetedAttackAuthoring authoring)
        {
            DependsOn(authoring.AttackPrefab);

            uint layerMask = (uint)authoring.TargetLayers.value;
            if (layerMask == 0)
                layerMask = (uint)CombatLayers.AllHurtboxLayers.value;

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TargetedAttackData
            {
                AttackPrefab = GetEntity(authoring.AttackPrefab, TransformUsageFlags.Dynamic),
                Range = math.max(0f, authoring.Range),
                Cooldown = math.max(0f, authoring.Cooldown),
                CooldownRemaining = 0f,
                TargetLayerMask = layerMask,
            });
        }
    }
}

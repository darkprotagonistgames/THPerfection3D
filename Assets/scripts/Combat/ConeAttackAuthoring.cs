using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Runtime cone-attack config baked from <see cref="ConeAttackAuthoring"/>.
/// </summary>
public struct ConeAttackData : IComponentData
{
    public Entity ConeAttackPrefab;
    public float Range;
    public float HalfAngleRadians;
    public float Cooldown;
    public float CooldownRemaining;

    /// <summary>Bit mask of physics layers (1 &lt;&lt; layer index) for valid hurtbox targets.</summary>
    public uint TargetLayerMask;
}

/// <summary>
/// Auto-spawns <see cref="ConeAttackPrefab"/> at this entity's position and rotation when a
/// hurtbox on a matching physics layer is within range and forward cone. Spawned entities are
/// not linked to the spawner. Uses <see cref="Cooldown"/> between spawns.
/// </summary>
public class ConeAttackAuthoring : MonoBehaviour
{
    [Tooltip("Entity prefab to instantiate (projectile, hitbox volume, etc.).")]
    [UnityEngine.Serialization.FormerlySerializedAs("AttackPrefab")]
    public GameObject ConeAttackPrefab;

    [Header("Targeting")]
    [Min(0f)]
    [Tooltip("Maximum horizontal distance to a target hurtbox (XZ plane).")]
    public float Range = 8f;

    [Range(0f, 360f)]
    [Tooltip("Total arc in degrees centered on model forward (-Z, toward the camera).")]
    public float ConeAngleDegrees = 90f;

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

    public class Baker : Baker<ConeAttackAuthoring>
    {
        public override void Bake(ConeAttackAuthoring authoring)
        {
            DependsOn(authoring.ConeAttackPrefab);

            uint layerMask = (uint)authoring.TargetLayers.value;
            if (layerMask == 0)
                layerMask = (uint)CombatLayers.AllHurtboxLayers.value;

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ConeAttackData
            {
                ConeAttackPrefab = GetEntity(authoring.ConeAttackPrefab, TransformUsageFlags.Dynamic),
                Range = math.max(0f, authoring.Range),
                HalfAngleRadians = math.radians(math.clamp(authoring.ConeAngleDegrees, 0f, 360f) * 0.5f),
                Cooldown = math.max(0f, authoring.Cooldown),
                CooldownRemaining = 0f,
                TargetLayerMask = layerMask,
            });
        }
    }
}

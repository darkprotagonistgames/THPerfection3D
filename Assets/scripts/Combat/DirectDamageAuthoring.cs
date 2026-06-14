using THPerfection.GeneratedEvents;
using Unity.Entities;
using UnityEngine;

/// <summary>One-shot direct damage config baked from <see cref="DirectDamageAuthoring"/>.</summary>
public struct DirectDamageData : IComponentData
{
    public float Damage;
    public wepon WeaponType;
}

/// <summary>
/// Applies configured damage once using <see cref="AttackSpawnContext"/> from a targeting spawn
/// (e.g. <see cref="TargetedAttackSystem"/>), then destroys this entity.
/// </summary>
public class DirectDamageAuthoring : MonoBehaviour
{
    [Tooltip("Damage amount written to damageEvent.amount.")]
    public float Damage = 10f;

    [Tooltip("Weapon type stored on damageEvent.sourceType.")]
    public wepon WeaponType = wepon.gun;

    public class Baker : Baker<DirectDamageAuthoring>
    {
        public override void Bake(DirectDamageAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DirectDamageData
            {
                Damage = authoring.Damage,
                WeaponType = authoring.WeaponType,
            });
        }
    }
}

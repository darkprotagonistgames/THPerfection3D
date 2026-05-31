using THPerfection.GeneratedEvents;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Active attack trigger on a collider child. Put this GameObject on the <see cref="Layers.HitBox"/> layer.
/// Use the collider's Include Layers so it only overlaps <see cref="Layers.HurtBox"/>.
/// Requires a trigger collider and a kinematic Rigidbody on the same GameObject (separate physics body from the character root).
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class HitboxAuthoring : MonoBehaviour
{
    [Tooltip("Damage dealt when this hitbox overlaps a hurtbox.")]
    public float Damage = 10f;

    [Tooltip("Weapon type stored on damageEvent.sourceType.")]
    public wepon WeaponType = wepon.bat;

    [Tooltip("Character root; if null, uses parent CharacterSettings.")]
    public Transform OwnerOverride;

    void OnValidate()
    {
        if (gameObject.layer != CombatLayers.HitBoxLayer)
            gameObject.layer = CombatLayers.HitBoxLayer;

        if (TryGetComponent<SphereCollider>(out var sphere))
        {
            sphere.isTrigger = true;
            sphere.includeLayers = CombatLayers.HitBoxIncludeMask;
        }
        else if (TryGetComponent<BoxCollider>(out var box))
        {
            box.isTrigger = true;
            box.includeLayers = CombatLayers.HitBoxIncludeMask;
        }
        else if (TryGetComponent<CapsuleCollider>(out var capsule))
        {
            capsule.isTrigger = true;
            capsule.includeLayers = CombatLayers.HitBoxIncludeMask;
        }
    }

    public class Baker : Baker<HitboxAuthoring>
    {
        public override void Bake(HitboxAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            Entity ownerEntity = CombatOwnerBakeUtility.ResolveOwnerEntity(this, authoring.OwnerOverride, authoring);

            AddComponent(entity, new HitboxData
            {
                Damage = authoring.Damage,
                WeaponType = authoring.WeaponType,
            });
            AddComponent(entity, new HitboxOwner { Value = ownerEntity });
        }
    }
}

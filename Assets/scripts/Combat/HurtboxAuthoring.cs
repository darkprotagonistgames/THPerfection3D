using THPerfection.GeneratedEvents;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Passive damage receiver on a collider child. Put this GameObject on the <see cref="Layers.HurtBox"/> layer.
/// Use the collider's Include Layers so it only overlaps <see cref="Layers.HitBox"/>.
/// Requires a trigger collider and a kinematic Rigidbody on the same GameObject.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class HurtboxAuthoring : MonoBehaviour
{
    [Tooltip("Stored on damageEvent.targetType (not used for physics filtering).")]
    public targetable Category = targetable.zombi;

    [Tooltip("Character root (future health owner); if null, uses parent CharacterSettings.")]
    public Transform OwnerOverride;

    void OnValidate()
    {
        if (gameObject.layer != CombatLayers.HurtBoxLayer)
            gameObject.layer = CombatLayers.HurtBoxLayer;

        if (TryGetComponent<SphereCollider>(out var sphere))
        {
            sphere.isTrigger = true;
            sphere.includeLayers = CombatLayers.HurtBoxIncludeMask;
        }
        else if (TryGetComponent<BoxCollider>(out var box))
        {
            box.isTrigger = true;
            box.includeLayers = CombatLayers.HurtBoxIncludeMask;
        }
        else if (TryGetComponent<CapsuleCollider>(out var capsule))
        {
            capsule.isTrigger = true;
            capsule.includeLayers = CombatLayers.HurtBoxIncludeMask;
        }
    }

    public class Baker : Baker<HurtboxAuthoring>
    {
        public override void Bake(HurtboxAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            Entity ownerEntity = CombatOwnerBakeUtility.ResolveOwnerEntity(this, authoring.OwnerOverride, authoring);

            AddComponent(entity, new HurtboxData { Category = authoring.Category });
            AddComponent(entity, new HurtboxOwner { Value = ownerEntity });
            CombatOwnerBakeUtility.AddTargetableTag(this, entity, authoring.Category);
        }
    }
}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Baked local pose of a combat collider child. Used to re-apply the offset each frame before
/// physics so kinematic child bodies follow the character root.
/// </summary>
public struct CombatColliderLocalOffset : IComponentData
{
    public float3 LocalPosition;
    public quaternion LocalRotation;
    public float LocalScale;
}

public static class CombatColliderBakeUtility
{
    public static void BakeLocalOffset<TAuthoring>(Baker<TAuthoring> baker, TAuthoring authoring, Entity entity)
        where TAuthoring : Component
    {
        Transform transform = authoring.transform;
        baker.AddComponent(entity, new CombatColliderLocalOffset
        {
            LocalPosition = transform.localPosition,
            LocalRotation = transform.localRotation,
            LocalScale = transform.localScale.y,
        });
    }

    public static void EnsureKinematicRigidbody(GameObject gameObject)
    {
        if (!gameObject.TryGetComponent<Rigidbody>(out var rigidbody))
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }
}

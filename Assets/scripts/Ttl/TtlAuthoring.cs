using Unity.Entities;
using UnityEngine;

/// <summary>
/// Bakes <see cref="TtlData"/> so <see cref="TtlSystem"/> destroys this entity after the configured lifetime.
/// </summary>
[DisallowMultipleComponent]
public class TtlAuthoring : MonoBehaviour
{
    [Min(0.01f)]
    [Tooltip("World seconds before this entity is destroyed.")]
    public float Seconds = 5f;

    public class Baker : Baker<TtlAuthoring>
    {
        public override void Bake(TtlAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TtlData { SecondsRemaining = authoring.Seconds });
        }
    }
}

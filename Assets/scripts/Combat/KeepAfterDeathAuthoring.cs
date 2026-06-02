using Unity.Entities;
using UnityEngine;

/// <summary>
/// Marks a child entity that should survive character death. DeathSystem detaches it in world
/// space and destroys the character root without destroying this entity or its descendants.
/// </summary>
[DisallowMultipleComponent]
public class KeepAfterDeathAuthoring : MonoBehaviour
{
    public class Baker : Baker<KeepAfterDeathAuthoring>
    {
        public override void Bake(KeepAfterDeathAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<KeepAfterDeathTag>(entity);
        }
    }
}

/// <summary>Baked from <see cref="KeepAfterDeathAuthoring"/>; searched under dying character roots.</summary>
public struct KeepAfterDeathTag : IComponentData { }

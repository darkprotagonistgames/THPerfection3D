using Unity.Entities;
using UnityEngine;

/// <summary>
/// Bakes <see cref="SnapToPlayerTag"/> so <see cref="SnapToPlayerSystem"/> keeps this entity's
/// transform aligned with the player each frame. Use on root-level followers (VFX, props, etc.).
/// </summary>
[DisallowMultipleComponent]
public class SnapToPlayerAuthoring : MonoBehaviour
{
    public class Baker : Baker<SnapToPlayerAuthoring>
    {
        public override void Bake(SnapToPlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<SnapToPlayerTag>(entity);
        }
    }
}

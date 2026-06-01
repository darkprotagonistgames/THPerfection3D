using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Bakes the ECS <see cref="MainEntityCamera"/> tag on the config entity (same GameObject as grid config).
/// </summary>
public class CameraRigAuthoring : MonoBehaviour
{
    public class Baker : Baker<CameraRigAuthoring>
    {
        public override void Bake(CameraRigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<MainEntityCamera>(entity);
            AddComponent(entity, LocalTransform.Identity);
        }
    }
}

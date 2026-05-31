using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Authoring component for player movement. Attach this to a GameObject
/// that represents the player to add DOTS movement components on bake.
/// </summary>
public class PlayerMovementAuthoring : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 20f;
    public float maxSpeed = 5f;

    public class Baker : Baker<PlayerMovementAuthoring>
    {
        public override void Bake(PlayerMovementAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerMovementData
            {
                Acceleration = authoring.acceleration,
                MaxSpeed = authoring.maxSpeed
            });

            AddComponent(entity, new PlayerInputData
            {
                Move = float2.zero
            });
        }
    }
}


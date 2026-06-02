using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Shared authoring hook for all character entities. Add future character-wide
/// ECS components here so every character prefab has one place to configure them.
/// </summary>
public class CharacterSettings : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Rotate the character so its forward faces its XZ velocity.")]
    public bool FaceVelocity = true;

    [Tooltip("Minimum horizontal speed before rotation updates (units/s).")]
    public float MinSpeedToRotate = 0.1f;

    [Header("Health")]
    public float Health = 10f;

    public class Baker : Baker<CharacterSettings>
    {
        public override void Bake(CharacterSettings authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CharacterTag());

            if (authoring.FaceVelocity)
            {
                float minSpeed = math.max(0f, authoring.MinSpeedToRotate);
                AddComponent(entity, new FaceVelocityRotation
                {
                    MinSpeedSq = minSpeed * minSpeed,
                });
            }

            AddComponent(entity, new Health
            {
                Value = math.max(0f, authoring.Health),
            });
        }
    }
}

/// <summary>Marks an entity as a character (player, enemy, NPC, etc.).</summary>
public struct CharacterTag : IComponentData { }

/// <summary>When present, <see cref="FaceVelocityRotationSystem"/> aligns rotation to XZ velocity.</summary>
public struct FaceVelocityRotation : IComponentData
{
    public float MinSpeedSq;
}

/// <summary>Current hit points for a character. Baked from <see cref="CharacterSettings"/>.</summary>
public struct Health : IComponentData
{
    public float Value;
}

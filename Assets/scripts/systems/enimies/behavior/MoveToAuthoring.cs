using Unity.Entities;
using UnityEngine;

/// <summary>
/// Authoring component that bakes <see cref="MoveToStatsReference"/> and
/// <see cref="MoveToTarget"/> onto an entity.
/// Place on any GameObject inside a sub-scene that should use the move-to behavior.
/// The baker allocates one <see cref="MoveToStats"/> blob per unique set of values;
/// Unity's blob deduplication means identical stats share the same allocation.
/// </summary>
public class MoveToAuthoring : MonoBehaviour
{
    [Tooltip("Rate at which velocity ramps up toward MaxMoveSpeed (units/s²).")]
    public float Acceleration = 20f;

    [Tooltip("Maximum speed this entity can reach while chasing a target (units/s).")]
    public float MaxMoveSpeed = 4f;

    [Tooltip("When enabled, this entity will automatically follow the player via FollowPlayerSystem.")]
    public bool FollowPlayer = false;

    public class Baker : Baker<MoveToAuthoring>
    {
        public override void Bake(MoveToAuthoring authoring)
        {
            // Build the blob asset
            var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref MoveToStats root = ref builder.ConstructRoot<MoveToStats>();
            root.Acceleration = authoring.Acceleration;
            root.MaxMoveSpeed = authoring.MaxMoveSpeed;

            BlobAssetReference<MoveToStats> blobRef =
                builder.CreateBlobAssetReference<MoveToStats>(Unity.Collections.Allocator.Persistent);

            builder.Dispose();

            // Register so the baker can deduplicate identical blobs across entities
            AddBlobAsset(ref blobRef, out _);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MoveToStatsReference { Stats = blobRef });
            AddComponent(entity, new MoveToTarget { IsActive = false });

            if (authoring.FollowPlayer)
                AddComponent(entity, new FollowPlayerTag());
        }
    }
}

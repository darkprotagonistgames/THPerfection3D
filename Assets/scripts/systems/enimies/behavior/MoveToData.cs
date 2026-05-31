using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Shared, read-only movement stats stored in a blob so every entity sharing
/// the same stats pays no per-instance memory cost beyond the reference itself.
/// </summary>
public struct MoveToStats
{
    /// <summary>Rate at which velocity approaches MaxMoveSpeed (units/s²).</summary>
    public float Acceleration;

    /// <summary>Top speed the entity can reach while moving toward a target (units/s).</summary>
    public float MaxMoveSpeed;
}

/// <summary>
/// Reference to the shared <see cref="MoveToStats"/> blob.
/// Attach this to any entity that should move toward a target.
/// </summary>
public struct MoveToStatsReference : IComponentData
{
    public BlobAssetReference<MoveToStats> Stats;
}

/// <summary>
/// Per-entity runtime state for the move-to behavior.
/// Stores the world-space target position and the entity's current 2-D velocity.
/// </summary>
public struct MoveToTarget : IComponentData
{
    /// <summary>World-space position the entity should move toward.</summary>
    public float2 TargetPosition;

    /// <summary>True while this entity should be actively seeking its target.</summary>
    public bool IsActive;
}

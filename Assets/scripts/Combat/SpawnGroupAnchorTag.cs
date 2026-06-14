using Unity.Entities;

/// <summary>
/// Marks a runtime-created child of a controller root that owns spawned attack instances.
/// Spawns are parented here and listed on this entity's <see cref="LinkedEntityGroup"/> only.
/// </summary>
public struct SpawnGroupAnchorTag : IComponentData { }

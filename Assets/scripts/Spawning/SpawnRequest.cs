using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Request the EcsSpawnBridge to instantiate a prefab near a world position.
/// Push into the SpawnRequest singleton buffer from any ECS system.
/// </summary>
public struct SpawnRequest : IBufferElementData
{
    /// <summary>World-space centre to search around for a valid placement.</summary>
    public float3 OriginPosition;

    /// <summary>Radius around OriginPosition to sample a random candidate from.</summary>
    public float SearchRadius;

    /// <summary>Index into EcsSpawnBridge.RegisteredPrefabs identifying the prefab.</summary>
    public int PrefabIndex;
}

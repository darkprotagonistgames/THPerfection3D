using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Spawns camera anchor entities at runtime when the subscene loads.
/// Matches <see cref="SafeRandomSpawnerSystem"/> timing so anchors exist before gameplay.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct CameraAnchorSpawnSystem : ISystem
{
    EntityQuery _existingAnchorsQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CameraAnchorGridConfig>();
        _existingAnchorsQuery = state.GetEntityQuery(ComponentType.ReadOnly<CameraAnchor>());
    }

    public void OnDestroy(ref SystemState state)
    {
        _existingAnchorsQuery.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_existingAnchorsQuery.CalculateEntityCount() > 0)
        {
            state.Enabled = false;
            return;
        }

        CameraAnchorGridConfig config = SystemAPI.GetSingleton<CameraAnchorGridConfig>();
        quaternion anchorRotation = quaternion.Euler(math.radians(config.AnchorEulerDegrees));
        float3 samplePosition = new(config.WorldWidth * 0.5f, config.AnchorY, config.WorldDepth * 0.5f);

        int2 gridCounts = CameraAnchorCoverage.ComputeGridCounts(
            config.WorldWidth,
            config.WorldDepth,
            samplePosition,
            anchorRotation,
            config.VerticalFovDegrees,
            config.Aspect,
            config.OverlapFactor);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        for (var zIndex = 0; zIndex < gridCounts.y; zIndex++)
        {
            for (var xIndex = 0; xIndex < gridCounts.x; xIndex++)
            {
                float x = config.WorldWidth * (xIndex + 0.5f) / gridCounts.x;
                float z = config.WorldDepth * (zIndex + 0.5f) / gridCounts.y;
                float3 position = new(x, config.AnchorY, z);

                Entity anchorEntity = ecb.CreateEntity();
                ecb.AddComponent(anchorEntity, new CameraAnchor());
                ecb.AddComponent(anchorEntity, LocalTransform.FromPositionRotation(position, anchorRotation));
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        state.Enabled = false;
    }
}

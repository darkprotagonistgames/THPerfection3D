using THPerfection.LevelGen;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

/// <summary>
/// Pins <see cref="FloorLockedMotion"/> characters to their floor Y and drops
/// vertical and angular velocity so the physics step cannot drop them under walls.
/// </summary>
[BurstCompile]
[WithAll(typeof(FloorLockedMotion))]
partial struct FloorPlaneMotionJob : IJobEntity
{
    public bool HasFloorHeight;
    public BuildingSpatialConfig Config;
    [ReadOnly] public ComponentLookup<GridCellLocation> CellLocations;

    void Execute(Entity entity, ref LocalTransform transform, ref PhysicsVelocity velocity)
    {
        if (HasFloorHeight)
        {
            FloorId floor = FloorId.Main;
            if (CellLocations.HasComponent(entity))
                floor = CellLocations[entity].Floor;

            transform.Position.y = FloorHeights.WorldY(floor, Config);
        }

        velocity.Linear.y = 0f;
        velocity.Angular = float3.zero;
        transform.Rotation = TopDownPlane.UprightYaw(transform.Rotation);
    }
}

/// <summary>
/// Applies <see cref="FloorPlaneMotionJob"/> before the physics world is built,
/// so the step starts on the floor with no vertical velocity.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial struct FloorPlaneBeforePhysicsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FloorLockedMotion>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        bool hasFloorHeight = false;
        var config = default(BuildingSpatialConfig);
        if (SystemAPI.TryGetSingleton(out BuildingSpatialConfig spatial))
        {
            hasFloorHeight = true;
            config = spatial;
        }

        var cellLocations = SystemAPI.GetComponentLookup<GridCellLocation>(true);
        cellLocations.Update(ref state);

        state.Dependency = new FloorPlaneMotionJob
        {
            HasFloorHeight = hasFloorHeight,
            Config = config,
            CellLocations = cellLocations,
        }.ScheduleParallel(state.Dependency);
    }
}

/// <summary>
/// Applies <see cref="FloorPlaneMotionJob"/> after physics export so a contact
/// impulse cannot leave the character below the floor.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct FloorPlaneAfterPhysicsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FloorLockedMotion>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        bool hasFloorHeight = false;
        var config = default(BuildingSpatialConfig);
        if (SystemAPI.TryGetSingleton(out BuildingSpatialConfig spatial))
        {
            hasFloorHeight = true;
            config = spatial;
        }

        var cellLocations = SystemAPI.GetComponentLookup<GridCellLocation>(true);
        cellLocations.Update(ref state);

        state.Dependency = new FloorPlaneMotionJob
        {
            HasFloorHeight = hasFloorHeight,
            Config = config,
            CellLocations = cellLocations,
        }.ScheduleParallel(state.Dependency);
    }
}

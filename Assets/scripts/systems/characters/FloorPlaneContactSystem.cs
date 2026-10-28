using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

/// <summary>
/// Keeps contacts on <see cref="FloorLockedMotion"/> bodies in the XZ plane.
/// Vertical contacts (floors, undersides of walls) are disabled. Tilted wall
/// contacts lose their Y component so the solver cannot push a character under a wall.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(PhysicsSimulationGroup))]
[UpdateAfter(typeof(PhysicsCreateContactsGroup))]
[UpdateBefore(typeof(PhysicsCreateJacobiansGroup))]
public partial struct FloorPlaneContactSystem : ISystem
{
    const float MinHorizontalNormalSq = 1e-4f;

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
        var floorLocked = SystemAPI.GetComponentLookup<FloorLockedMotion>(true);
        floorLocked.Update(ref state);

        var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        state.Dependency = new FloorPlaneContactJob
        {
            FloorLocked = floorLocked,
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), ref world, state.Dependency);
    }

    [BurstCompile]
    struct FloorPlaneContactJob : IContactsJob
    {
        [ReadOnly] public ComponentLookup<FloorLockedMotion> FloorLocked;

        public void Execute(ref ModifiableContactHeader header, ref ModifiableContactPoint contact)
        {
            if ((header.JacobianFlags & JacobianFlags.IsTrigger) != 0)
                return;
            if (!FloorLocked.HasComponent(header.EntityA) && !FloorLocked.HasComponent(header.EntityB))
                return;

            float3 normal = header.Normal;
            float horizontalSq = normal.x * normal.x + normal.z * normal.z;
            if (horizontalSq < MinHorizontalNormalSq)
            {
                header.JacobianFlags |= JacobianFlags.Disabled;
                return;
            }

            header.Normal = new float3(normal.x, 0f, normal.z) * math.rsqrt(horizontalSq);
        }
    }
}

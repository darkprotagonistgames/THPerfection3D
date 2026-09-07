using THPerfection.GeneratedEvents;
using THPerfection.LevelGen;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Updates <see cref="GridCellLocation"/> / <see cref="RoomLocation"/> from <see cref="LocalTransform"/>
/// using the committed building layout blob. Enables change tags and emits frame events on transitions.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
[UpdateBefore(typeof(EnableAllEcsEventsSystem))]
public partial struct SpatialOccupancyUpdateSystem : ISystem
{
    ComponentLookup<PlayerMovementData> _playerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BuildingLayoutBlobRef>();
        state.RequireForUpdate<BuildingSpatialConfig>();
        state.RequireForUpdate<TracksSpatialOccupancy>();
        _playerLookup = state.GetComponentLookup<PlayerMovementData>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton(out BuildingSpatialConfig config))
            return;
        if (!SystemAPI.TryGetSingleton(out BuildingLayoutBlobRef blobRef) || !blobRef.Layout.IsCreated)
            return;

        _playerLookup.Update(ref state);

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new SpatialOccupancyUpdateJob
        {
            CellSize = config.CellSize,
            Layout = blobRef.Layout,
            PlayerLookup = _playerLookup,
            ECB = ecb.AsParallelWriter(),
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    [WithAll(typeof(TracksSpatialOccupancy))]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    partial struct SpatialOccupancyUpdateJob : IJobEntity
    {
        public float CellSize;
        [ReadOnly] public BlobAssetReference<BuildingLayoutBlob> Layout;
        [ReadOnly] public ComponentLookup<PlayerMovementData> PlayerLookup;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute(
            [ChunkIndexInQuery] int sortKey,
            Entity entity,
            in LocalTransform transform,
            ref GridCellLocation cellLocation,
            ref RoomLocation roomLocation,
            ref GridCellChanged gridCellChanged,
            EnabledRefRW<GridCellChanged> gridCellChangedEnabled,
            ref RoomChanged roomChanged,
            EnabledRefRW<RoomChanged> roomChangedEnabled)
        {
            float2 xz = TopDownPlane.FromPosition(transform.Position);
            int2 cell = BuildingLayoutLookup.WorldToCell(xz, CellSize);
            FloorId floor = FloorId.Main;

            int roomId = 0;
            ref BuildingLayoutBlob layout = ref Layout.Value;
            if (BuildingLayoutLookup.TryGetCell(ref layout, floor, cell, out BuildingLayoutCellBlob cellBlob))
                roomId = cellBlob.RoomInstanceId;

            bool cellChanged = cellLocation.Floor != floor || cellLocation.Cell.x != cell.x || cellLocation.Cell.y != cell.y;
            if (cellChanged)
            {
                gridCellChanged = new GridCellChanged
                {
                    PreviousFloor = cellLocation.Floor,
                    PreviousCell = cellLocation.Cell,
                    CurrentFloor = floor,
                    CurrentCell = cell,
                };
                cellLocation = new GridCellLocation
                {
                    Floor = floor,
                    Cell = cell,
                };
                gridCellChangedEnabled.ValueRW = true;
            }

            bool roomChangedThisFrame = roomLocation.RoomInstanceId != roomId;
            if (roomChangedThisFrame)
            {
                int previousRoomId = roomLocation.RoomInstanceId;
                roomChanged = new RoomChanged
                {
                    PreviousRoomInstanceId = previousRoomId,
                    CurrentRoomInstanceId = roomId,
                };
                roomLocation = new RoomLocation { RoomInstanceId = roomId };
                roomChangedEnabled.ValueRW = true;

                entity.CreateroomChangedEvent(ECB, sortKey, previousRoomId, roomId);
                if (PlayerLookup.HasComponent(entity))
                    entity.CreateplayerRoomChangedEvent(ECB, sortKey, previousRoomId, roomId);
            }
        }
    }
}

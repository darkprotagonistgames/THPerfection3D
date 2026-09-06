using THPerfection.LevelGen;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// ECS snapshot of the committed building layout. Populated by <see cref="BuildingLayoutEcsBridge"/>.
/// </summary>
public struct BuildingSpatialConfig : IComponentData
{
    public float CellSize;
    public float MainFloorY;
}

public struct BuildingLayoutBlobRef : IComponentData
{
    public BlobAssetReference<BuildingLayoutBlob> Layout;
    public uint LayoutVersion;
}

/// <summary>
/// Enabled for one frame after each layout commit. Disabled by <see cref="BuildingLayoutChangedCleanupSystem"/>.
/// </summary>
public struct BuildingLayoutChanged : IComponentData, IEnableableComponent
{
    public uint LayoutVersion;
}

public struct BuildingLayoutSingletonTag : IComponentData
{
}

public struct BuildingRoomTag : IComponentData
{
}

public struct BuildingRoomInstance : IComponentData
{
    public int RoomInstanceId;
    public FloorId Floor;
    public int2 Origin;
    public Rotation90 Rotation;
    public FixedString64Bytes TemplateId;
}

public struct BuildingRoomDoorState : IBufferElementData
{
    public int2 WorldCell;
    public DoorSide Side;
    public CellDoorState State;
}

public struct BuildingRoomVisualPhaseState : IComponentData
{
    public RoomDoorVisualPhase Phase;
}

public struct RoomVisualPrefabRegistryTag : IComponentData
{
}

public struct RoomVisualPrefabEntry : IBufferElementData
{
    public FixedString64Bytes TemplateId;
    public Entity Prefab;
}

/// <summary>Baked on door marker entities inside room prefabs.</summary>
public struct RoomDoorSocketBaked : IComponentData
{
    public int2 LocalCell;
    public DoorSide Side;
    public Entity OpenVisual;
    public Entity ClosedVisual;
}

public struct RoomDoorOpenVisualEntity : IBufferElementData
{
    public Entity Entity;
}

public struct RoomDoorClosedVisualEntity : IBufferElementData
{
    public Entity Entity;
}

public struct BuildingRoomEntityEntry : IBufferElementData
{
    public int RoomInstanceId;
    public Entity RoomEntity;
}

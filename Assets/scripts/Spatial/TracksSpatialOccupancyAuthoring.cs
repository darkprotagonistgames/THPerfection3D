using Unity.Entities;
using UnityEngine;

/// <summary>
/// Bakes <see cref="TracksSpatialOccupancy"/> and cached location / change components.
/// Change tags start disabled.
/// </summary>
public class TracksSpatialOccupancyAuthoring : MonoBehaviour
{
    class Baker : Baker<TracksSpatialOccupancyAuthoring>
    {
        public override void Bake(TracksSpatialOccupancyAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<TracksSpatialOccupancy>(entity);
            AddComponent(entity, new GridCellLocation());
            AddComponent(entity, new RoomLocation());
            AddComponent(entity, new GridCellChanged());
            AddComponent(entity, new RoomChanged());
            SetComponentEnabled<GridCellChanged>(entity, false);
            SetComponentEnabled<RoomChanged>(entity, false);
        }
    }
}

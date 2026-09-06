using System.Collections.Generic;
using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public sealed class RoomInstance
    {
        public int Id { get; }
        public string TemplateId { get; }
        public FloorId Floor { get; }
        public int2 Origin { get; }
        public Rotation90 Rotation { get; }

        readonly Dictionary<DoorEdgeKey, CellDoorState> _doorStates = new();

        public IReadOnlyDictionary<DoorEdgeKey, CellDoorState> DoorStates => _doorStates;

        public RoomInstance(
            int id,
            in RoomTemplateDefinition template,
            FloorId floor,
            int2 origin,
            Rotation90 rotation)
        {
            Id         = id;
            TemplateId = template.TemplateId;
            Floor      = floor;
            Origin     = origin;
            Rotation   = rotation;
        }

        public void SetDoorState(in DoorEdgeKey key, CellDoorState state) =>
            _doorStates[key] = state;

        public bool TryGetDoorState(in DoorEdgeKey key, out CellDoorState state) =>
            _doorStates.TryGetValue(key, out state);
    }
}

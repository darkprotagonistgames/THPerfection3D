using System;
using System.Collections.Generic;

namespace THPerfection.LevelGen
{
    public sealed class ExpansionResult
    {
        public IReadOnlyList<RoomInstance> AddedInstances { get; }
        public BuildingGenerationResult Snapshot { get; }
        public int RoomsBefore { get; }
        public int RoomsAfter => Snapshot.Instances.Count;

        public ExpansionResult(
            IReadOnlyList<RoomInstance> addedInstances,
            BuildingGenerationResult snapshot,
            int roomsBefore)
        {
            AddedInstances = addedInstances ?? Array.Empty<RoomInstance>();
            Snapshot       = snapshot;
            RoomsBefore    = roomsBefore;
        }

        public static ExpansionResult NoChange(BuildingRunState state, IReadOnlyList<DoorwaySlot> openFrontier) =>
            new(Array.Empty<RoomInstance>(), state.ToResult(openFrontier), state.Instances.Count);
    }
}

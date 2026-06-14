using System.Collections.Generic;

namespace THPerfection.LevelGen
{
    public readonly struct RoomCatalogEntry
    {
        public readonly RoomTemplateDefinition Template;
        public readonly IRoomPlacementEvaluator Evaluator;

        public RoomCatalogEntry(RoomTemplateDefinition template, IRoomPlacementEvaluator evaluator = null)
        {
            Template   = template;
            Evaluator  = evaluator ?? HardRulesOnlyEvaluator.Instance;
        }
    }

    public sealed class RoomCatalog
    {
        readonly List<RoomCatalogEntry> _entries = new();

        public IReadOnlyList<RoomCatalogEntry> Entries => _entries;

        public void Add(in RoomCatalogEntry entry) => _entries.Add(entry);

        public IEnumerable<RoomCatalogEntry> ForFloor(FloorId floor)
        {
            FloorMask mask = GridTransforms.ToMask(floor);
            for (int i = 0; i < _entries.Count; i++)
            {
                if ((_entries[i].Template.AllowedFloors & mask) != 0)
                    yield return _entries[i];
            }
        }

        public static RoomCatalog CreateDefaultMainFloor()
        {
            var catalog = new RoomCatalog();
            var eval = HardRulesOnlyEvaluator.Instance;

            // Branching templates (2+ doors) — required for expansion to continue.
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.TwoByOneHall, eval));
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.OneByTwoHallNorthSouth, eval));
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.OneByThreeHallEastWest, eval));
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.ThreeWayTJunction, eval));
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.TwoByTwoOffice, eval));
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.FourWayCross, eval));

            // Dead-end caps (single door) — low weight so they terminate branches, not the frontier.
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.OneByOneNorth, eval));
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.OneByOneSouth, eval));
            catalog.Add(new RoomCatalogEntry(RoomBuiltinTemplates.OneByOneEast, eval));

            return catalog;
        }
    }
}

using System.Collections.Generic;
using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public readonly struct PlacementCandidate
    {
        public readonly RoomCatalogEntry Entry;
        public readonly int2 Origin;
        public readonly Rotation90 Rotation;
        public readonly float Weight;

        public PlacementCandidate(
            in RoomCatalogEntry entry,
            int2 origin,
            Rotation90 rotation,
            float weight)
        {
            Entry    = entry;
            Origin   = origin;
            Rotation = rotation;
            Weight   = weight;
        }

        public RoomTemplateDefinition Template => Entry.Template;
    }

    public static class WeightedSelection
    {
        public static bool TryPick(
            ref Random rng,
            IReadOnlyList<PlacementCandidate> candidates,
            out PlacementCandidate picked)
        {
            picked = default;
            if (candidates == null || candidates.Count == 0)
                return false;

            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
                total += candidates[i].Weight;

            if (total <= 0f)
                return false;

            float roll = rng.NextFloat(0f, total);
            float cumulative = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += candidates[i].Weight;
                if (roll <= cumulative)
                {
                    picked = candidates[i];
                    return true;
                }
            }

            picked = candidates[candidates.Count - 1];
            return true;
        }
    }
}

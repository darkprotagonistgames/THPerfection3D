using System;
using System.Collections.Generic;
using THPerfection.LevelGen.Authoring;
using UnityEngine;

namespace THPerfection.LevelGen
{
    [Serializable]
    public struct RoomCatalogSourceEntry
    {
        [Tooltip("Room prefab root with RoomTemplateAuthoring + cell/door markers.")]
        public GameObject RoomPrefab;

        [Tooltip("Optional override. When null, uses Evaluator on RoomTemplateAuthoring, then hard rules only.")]
        public RoomTemplateBase EvaluatorOverride;
    }

    [CreateAssetMenu(fileName = "RoomCatalog", menuName = "TH Perfection/Level Gen/Room Catalog")]
    public sealed class RoomCatalogAsset : ScriptableObject
    {
        [Tooltip("When enabled, builtin procedural templates fill gaps not covered by authored prefabs.")]
        public bool IncludeBuiltinFallback = true;

        public List<RoomCatalogSourceEntry> Entries = new();

        public RoomCatalog BuildCatalog()
        {
            var catalog = new RoomCatalog();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < Entries.Count; i++)
            {
                RoomCatalogSourceEntry source = Entries[i];
                if (source.RoomPrefab == null)
                    continue;

                var authoring = source.RoomPrefab.GetComponent<RoomTemplateAuthoring>();
                if (authoring == null)
                {
                    Debug.LogWarning(
                        $"[LevelGen] Room catalog entry '{source.RoomPrefab.name}' has no RoomTemplateAuthoring. Skipping.",
                        source.RoomPrefab);
                    continue;
                }

                if (!authoring.TryBake(out RoomTemplateDefinition template, out string error))
                {
                    Debug.LogWarning(
                        $"[LevelGen] Failed to bake '{source.RoomPrefab.name}': {error}",
                        source.RoomPrefab);
                    continue;
                }

                if (!seenIds.Add(template.TemplateId))
                {
                    Debug.LogWarning(
                        $"[LevelGen] Duplicate TemplateId '{template.TemplateId}' in catalog. Skipping '{source.RoomPrefab.name}'.",
                        source.RoomPrefab);
                    continue;
                }

                IRoomPlacementEvaluator evaluator = ResolveEvaluator(source, authoring);
                catalog.Add(new RoomCatalogEntry(template, evaluator, source.RoomPrefab));
            }

            if (IncludeBuiltinFallback)
                MergeBuiltinFallback(catalog, seenIds);

            return catalog;
        }

        static IRoomPlacementEvaluator ResolveEvaluator(
            in RoomCatalogSourceEntry source,
            RoomTemplateAuthoring authoring)
        {
            if (source.EvaluatorOverride != null)
                return new ScriptableRoomEvaluator(source.EvaluatorOverride);

            if (authoring.Evaluator != null)
                return new ScriptableRoomEvaluator(authoring.Evaluator);

            return HardRulesOnlyEvaluator.Instance;
        }

        static void MergeBuiltinFallback(RoomCatalog catalog, HashSet<string> seenIds)
        {
            RoomCatalog builtin = RoomCatalog.CreateDefaultMainFloor();
            for (int i = 0; i < builtin.Entries.Count; i++)
            {
                RoomCatalogEntry entry = builtin.Entries[i];
                if (seenIds.Add(entry.Template.TemplateId))
                    catalog.Add(entry);
            }
        }
    }
}

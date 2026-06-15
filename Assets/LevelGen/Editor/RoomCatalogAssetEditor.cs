using THPerfection.LevelGen.Authoring;
using UnityEditor;
using UnityEngine;

namespace THPerfection.LevelGen.Editor
{
    [CustomEditor(typeof(RoomCatalogAsset))]
    public sealed class RoomCatalogAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var asset = (RoomCatalogAsset)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate All Entries"))
                ValidateAll(asset);

            if (GUILayout.Button("Preview Built Catalog"))
                PreviewCatalog(asset);
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        static void ValidateAll(RoomCatalogAsset asset)
        {
            int valid = 0;
            int invalid = 0;

            for (int i = 0; i < asset.Entries.Count; i++)
            {
                RoomCatalogSourceEntry entry = asset.Entries[i];
                if (entry.RoomPrefab == null)
                    continue;

                var authoring = entry.RoomPrefab.GetComponent<RoomTemplateAuthoring>();
                if (authoring == null)
                {
                    invalid++;
                    Debug.LogError(
                        $"[LevelGen] Entry {i}: '{entry.RoomPrefab.name}' missing RoomTemplateAuthoring.",
                        entry.RoomPrefab);
                    continue;
                }

                if (authoring.TryBake(out RoomTemplateDefinition template, out string error))
                {
                    valid++;
                    Debug.Log(
                        $"[LevelGen] Entry {i}: OK — {RoomTemplateBaker.Describe(template)}",
                        entry.RoomPrefab);
                }
                else
                {
                    invalid++;
                    Debug.LogError(
                        $"[LevelGen] Entry {i}: '{entry.RoomPrefab.name}' — {error}",
                        entry.RoomPrefab);
                }
            }

            Debug.Log($"[LevelGen] Catalog validation complete: {valid} valid, {invalid} invalid.", asset);
        }

        static void PreviewCatalog(RoomCatalogAsset asset)
        {
            RoomCatalog catalog = asset.BuildCatalog();
            Debug.Log($"[LevelGen] Built catalog with {catalog.Entries.Count} entries.", asset);

            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                RoomCatalogEntry entry = catalog.Entries[i];
                string prefabName = entry.Prefab != null ? entry.Prefab.name : "(builtin/procedural)";
                Debug.Log($"  [{i}] {RoomTemplateBaker.Describe(entry.Template)} prefab={prefabName}");
            }
        }
    }
}

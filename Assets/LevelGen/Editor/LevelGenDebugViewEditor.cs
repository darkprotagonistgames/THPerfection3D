using UnityEditor;
using UnityEngine;

namespace THPerfection.LevelGen.Editor
{
    [CustomEditor(typeof(LevelGenDebugView))]
    public sealed class LevelGenDebugViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var view = (LevelGenDebugView)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomizeSeedOnGenerate"));

            using (new EditorGUI.DisabledScope(view.RandomizeSeedOnGenerate))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Seed"));
            }

            if (view.RandomizeSeedOnGenerate)
                EditorGUILayout.HelpBox("Seed randomizes on each generate. Uncheck above to pin a specific seed.", MessageType.Info);

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Randomize Seed"))
            {
                Undo.RecordObject(view, "Randomize Level Gen Seed");
                view.RandomizeSeed();
                EditorUtility.SetDirty(view);
            }

            if (GUILayout.Button("Generate Main Floor"))
            {
                Undo.RecordObject(view, "Generate Main Floor");
                view.GenerateMainFloor();
                EditorUtility.SetDirty(view);
                SceneView.RepaintAll();
            }

            using (new EditorGUI.DisabledScope(view.RunState == null || view.RunState.Instances.Count == 0))
            {
                if (GUILayout.Button("Continue Expansion"))
                {
                    Undo.RecordObject(view, "Continue Level Gen Expansion");
                    view.ContinueExpansion();
                    EditorUtility.SetDirty(view);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("AdditionalRoomsOnContinue"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Visuals"))
            {
                view.SpawnVisuals();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Clear Visuals"))
            {
                view.ClearVisuals();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Visual Spawn", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnVisualsOnGenerate"));

            var spawner = view.RoomSpawner;
            if (spawner != null)
            {
                using (var so = new SerializedObject(spawner))
                {
                    so.Update();
                    EditorGUILayout.PropertyField(so.FindProperty("DefaultRoomPrefab"));
                    EditorGUILayout.PropertyField(so.FindProperty("PrefabMappings"), true);
                    EditorGUILayout.PropertyField(so.FindProperty("UseProceduralFallback"));
                    so.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Catalog", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CatalogAsset"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Config"), includeChildren: true);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Gizmo Colors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RoomFillColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OpenDoorColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ConnectedDoorColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ClosedDoorColor"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Room Count", view.Result?.Instances.Count ?? 0);
                EditorGUILayout.IntField("Open Frontier", view.Result?.OpenFrontier.Count ?? 0);
                EditorGUILayout.IntField("Pass Count", view.RunState?.PassCount ?? 0);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

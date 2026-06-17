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
            BuildingRunDirector director = view.Director;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomizeSeedOnGenerate"));

            using (new EditorGUI.DisabledScope(view.RandomizeSeedOnGenerate))
            {
                if (director != null)
                {
                    using (var directorSo = new SerializedObject(director))
                    {
                        directorSo.Update();
                        EditorGUILayout.PropertyField(directorSo.FindProperty("RunSeed"));
                        directorSo.ApplyModifiedProperties();
                    }
                }
            }

            if (view.RandomizeSeedOnGenerate)
                EditorGUILayout.HelpBox("Seed randomizes on each generate. Uncheck above to pin Run Seed on the director.", MessageType.Info);

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Randomize Seed"))
            {
                Undo.RecordObject(director, "Randomize Level Gen Seed");
                view.RandomizeSeed();
                EditorUtility.SetDirty(director);
            }

            if (GUILayout.Button("Generate Main Floor"))
            {
                Undo.RecordObject(director, "Generate Main Floor");
                view.GenerateMainFloor();
                EditorUtility.SetDirty(director);
                SceneView.RepaintAll();
            }

            using (new EditorGUI.DisabledScope(view.RunState == null || view.RunState.Instances.Count == 0))
            {
                if (GUILayout.Button("Continue Expansion"))
                {
                    Undo.RecordObject(director, "Continue Level Gen Expansion");
                    view.ContinueExpansion();
                    EditorUtility.SetDirty(director);
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
            EditorGUILayout.LabelField("Director", EditorStyles.boldLabel);

            if (director != null)
            {
                using (var directorSo = new SerializedObject(director))
                {
                    directorSo.Update();
                    EditorGUILayout.PropertyField(directorSo.FindProperty("CatalogAsset"));
                    EditorGUILayout.PropertyField(directorSo.FindProperty("Config"), includeChildren: true);
                    EditorGUILayout.PropertyField(directorSo.FindProperty("SpawnRoomsOnCommit"));

                    var spawner = director.RoomSpawner;
                    if (spawner != null)
                    {
                        EditorGUILayout.Space(4f);
                        EditorGUILayout.LabelField("Room Spawner", EditorStyles.boldLabel);
                        using (var spawnerSo = new SerializedObject(spawner))
                        {
                            spawnerSo.Update();
                            EditorGUILayout.PropertyField(spawnerSo.FindProperty("DefaultRoomPrefab"));
                            EditorGUILayout.PropertyField(spawnerSo.FindProperty("PrefabMappings"), true);
                            EditorGUILayout.PropertyField(spawnerSo.FindProperty("UseProceduralFallback"));
                            spawnerSo.ApplyModifiedProperties();
                        }
                    }

                    directorSo.ApplyModifiedProperties();
                }
            }

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

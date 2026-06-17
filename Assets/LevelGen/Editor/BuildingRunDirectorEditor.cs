using UnityEditor;
using UnityEngine;

namespace THPerfection.LevelGen.Editor
{
    [CustomEditor(typeof(BuildingRunDirector))]
    public sealed class BuildingRunDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var director = (BuildingRunDirector)target;

            DrawPropertiesExcluding(serializedObject, "_roomSpawner");

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Start Run"))
            {
                Undo.RecordObject(director, "Start Building Run");
                director.StartRun();
                EditorUtility.SetDirty(director);
                SceneView.RepaintAll();
            }

            using (new EditorGUI.DisabledScope(director.RunState == null || director.RunState.Instances.Count == 0))
            {
                if (GUILayout.Button("Expand +4"))
                {
                    Undo.RecordObject(director, "Expand Building Run");
                    director.ExpandRun(4, director.RunSeed);
                    EditorUtility.SetDirty(director);
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Clear Run"))
            {
                Undo.RecordObject(director, "Clear Building Run");
                director.ClearRun();
                EditorUtility.SetDirty(director);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Room Count", director.LastResult?.Instances.Count ?? 0);
                EditorGUILayout.IntField("Open Frontier", director.LastResult?.OpenFrontier.Count ?? 0);
                EditorGUILayout.IntField("Pass Count", director.RunState?.PassCount ?? 0);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace THPerfection.LevelGen.Editor
{
    public static class LevelGenMenu
    {
        [MenuItem("TH Perfection/Level Gen/Generate Test Floor In Scene")]
        static void GenerateTestFloorInScene()
        {
            var existing = Object.FindFirstObjectByType<LevelGenDebugView>();
            LevelGenDebugView view = existing != null
                ? existing
                : new GameObject("LevelGenDebugView").AddComponent<LevelGenDebugView>();

            if (existing == null)
                Undo.RegisterCreatedObjectUndo(view.gameObject, "Create LevelGenDebugView");

            view.GenerateMainFloor();
            Selection.activeGameObject = view.gameObject;
            SceneView.RepaintAll();
        }
    }
}

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

        [MenuItem("TH Perfection/Level Gen/Create One-By-One Placeholder Prefab")]
        static void CreateOneByOnePlaceholderPrefab()
        {
            const float cellSize = 4f;
            const string templateId = "one_by_one_north";
            const string prefabPath = "Assets/LevelGen/Prefabs/OneByOneNorthPlaceholder.prefab";

            var root = new GameObject("OneByOneNorthPlaceholder");
            try
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Floor";
                floor.transform.SetParent(root.transform, false);
                floor.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                floor.transform.localScale = new Vector3(cellSize * 0.92f, 0.2f, cellSize * 0.92f);

                var doorOpen = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doorOpen.name = "Door_Open";
                doorOpen.transform.SetParent(root.transform, false);
                doorOpen.transform.localPosition = new Vector3(0f, 0.55f, cellSize * 0.45f);
                doorOpen.transform.localScale = new Vector3(cellSize * 0.7f, 0.15f, cellSize * 0.12f);

                var doorClosed = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doorClosed.name = "Door_Closed";
                doorClosed.transform.SetParent(root.transform, false);
                doorClosed.transform.localPosition = new Vector3(0f, 0.55f, cellSize * 0.5f);
                doorClosed.transform.localScale = new Vector3(cellSize * 0.7f, 0.25f, cellSize * 0.08f);
                doorClosed.SetActive(false);

                var authoring = root.AddComponent<LevelGenRoomVisualAuthoring>();

                var so = new SerializedObject(authoring);
                so.FindProperty("_doorVisuals").arraySize = 1;
                var element = so.FindProperty("_doorVisuals").GetArrayElementAtIndex(0);
                element.FindPropertyRelative("LocalCell").FindPropertyRelative("x").intValue = 0;
                element.FindPropertyRelative("LocalCell").FindPropertyRelative("y").intValue = 0;
                element.FindPropertyRelative("Side").enumValueIndex = (int)DoorSide.North;
                element.FindPropertyRelative("OpenVisual").objectReferenceValue = doorOpen;
                element.FindPropertyRelative("ClosedVisual").objectReferenceValue = doorClosed;
                so.ApplyModifiedPropertiesWithoutUndo();

                System.IO.Directory.CreateDirectory("Assets/LevelGen/Prefabs");
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                var view = Object.FindFirstObjectByType<LevelGenDebugView>();
                if (view != null)
                {
                    var spawner = view.RoomSpawner;
                    var spawnerSo = new SerializedObject(spawner);
                    var mappings = spawnerSo.FindProperty("PrefabMappings");
                    mappings.arraySize = 1;
                    var map = mappings.GetArrayElementAtIndex(0);
                    map.FindPropertyRelative("TemplateId").stringValue = templateId;
                    map.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
                    spawnerSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(spawner);
                }

                Debug.Log(
                    $"[LevelGen] Created placeholder prefab at {prefabPath}. "
                    + "Map more templates on LevelGenRoomSpawner → Prefab Mappings.");
                Selection.activeObject = prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

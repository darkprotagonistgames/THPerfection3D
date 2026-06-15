using THPerfection.LevelGen.Authoring;
using Unity.Mathematics;
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

        [MenuItem("TH Perfection/Level Gen/Create Room Catalog Asset")]
        static void CreateRoomCatalogAsset()
        {
            const string path = "Assets/LevelGen/RoomCatalog.asset";
            System.IO.Directory.CreateDirectory("Assets/LevelGen");

            var existing = AssetDatabase.LoadAssetAtPath<RoomCatalogAsset>(path);
            if (existing != null)
            {
                Selection.activeObject = existing;
                Debug.Log($"[LevelGen] Room catalog already exists at {path}.");
                return;
            }

            var asset = ScriptableObject.CreateInstance<RoomCatalogAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            Debug.Log($"[LevelGen] Created room catalog at {path}. Assign authored prefabs to Entries.");
        }

        [MenuItem("TH Perfection/Level Gen/Create Room Template Prefab (1×1 North)")]
        static void CreateOneByOneNorthTemplatePrefab()
        {
            CreateRoomTemplatePrefab(
                prefabName: "Room_OneByOneNorth",
                templateId: "one_by_one_north",
                prefabPath: "Assets/LevelGen/RoomTemplates/Room_OneByOneNorth.prefab",
                cellCount: 1,
                mainDoorSide: DoorSide.North);
        }

        [MenuItem("TH Perfection/Level Gen/Create Room Template Prefab (2×1 Hall)")]
        static void CreateTwoByOneHallTemplatePrefab()
        {
            CreateRoomTemplatePrefab(
                prefabName: "Room_TwoByOneHall",
                templateId: "two_by_one_hall",
                prefabPath: "Assets/LevelGen/RoomTemplates/Room_TwoByOneHall.prefab",
                cellCount: 2,
                mainDoorSide: DoorSide.West,
                extraDoors: new[] { (new Vector2Int(1, 0), DoorSide.East, false) },
                cellOffsets: new[] { Vector2Int.zero, new Vector2Int(1, 0) });
        }

        public static GameObject CreateRoomTemplatePrefab(
            string prefabName,
            string templateId,
            string prefabPath,
            int cellCount,
            DoorSide mainDoorSide,
            (Vector2Int cell, DoorSide side, bool isMain)[] extraDoors = null,
            Vector2Int[] cellOffsets = null)
        {
            float cellSize = BuildingGenConfig.DefaultCellSize;
            var root = new GameObject(prefabName);

            try
            {
                var authoring = root.AddComponent<RoomTemplateAuthoring>();
                authoring.TemplateId = templateId;
                authoring.AllowedFloors = FloorMask.Main;
                authoring.BaseWeight = 1f;
                authoring.CellSize = cellSize;

                var markersRoot = new GameObject("Markers").transform;
                markersRoot.SetParent(root.transform, false);
                authoring.MarkersRoot = markersRoot;

                var cellsRoot = new GameObject("Cells").transform;
                cellsRoot.SetParent(markersRoot, false);

                if (cellOffsets == null || cellOffsets.Length == 0)
                {
                    cellOffsets = new Vector2Int[cellCount];
                    for (int i = 0; i < cellCount; i++)
                        cellOffsets[i] = new Vector2Int(i, 0);
                }

                var artRoot = new GameObject("Art").transform;
                artRoot.SetParent(root.transform, false);

                for (int i = 0; i < cellOffsets.Length; i++)
                {
                    Vector2Int offset = cellOffsets[i];
                    var cellGo = new GameObject($"Cell_{offset.x}_{offset.y}");
                    cellGo.transform.SetParent(cellsRoot, false);
                    cellGo.transform.localPosition = new Vector3(offset.x * cellSize, 0f, offset.y * cellSize);
                    cellGo.AddComponent<RoomCellMarker>();

                    var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    floor.name = "Floor";
                    floor.transform.SetParent(artRoot, false);
                    floor.transform.localPosition = new Vector3(offset.x * cellSize, 0.1f, offset.y * cellSize);
                    floor.transform.localScale = new Vector3(cellSize * 0.92f, 0.2f, cellSize * 0.92f);
                }

                var doorsRoot = new GameObject("Doors").transform;
                doorsRoot.SetParent(markersRoot, false);

                AddDoor(doorsRoot, artRoot, cellSize, Vector2Int.zero, mainDoorSide, isMain: true);

                if (extraDoors != null)
                {
                    for (int i = 0; i < extraDoors.Length; i++)
                    {
                        var (cell, side, isMain) = extraDoors[i];
                        AddDoor(doorsRoot, artRoot, cellSize, cell, side, isMain);
                    }
                }

                if (!authoring.TryBake(out _, out string error))
                    Debug.LogWarning($"[LevelGen] Prefab created but bake validation failed: {error}");

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath)!);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                TryAppendToCatalog(prefab);
                Selection.activeObject = prefab;
                Debug.Log($"[LevelGen] Created room template prefab at {prefabPath}.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void AddDoor(
            Transform doorsRoot,
            Transform artRoot,
            float cellSize,
            Vector2Int cell,
            DoorSide side,
            bool isMain)
        {
            var doorGo = new GameObject($"Door_{side}_{cell.x}_{cell.y}");
            doorGo.transform.SetParent(doorsRoot, false);

            var marker = doorGo.AddComponent<DoorSocketMarker>();
            marker.LocalCell = new int2(cell.x, cell.y);
            marker.Side = side;
            marker.IsMainDoor = isMain;

            var doorOpen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorOpen.name = "OpenVisual";
            doorOpen.transform.SetParent(artRoot, false);
            doorOpen.transform.localPosition = LevelGenWorldTransform.DoorEdgeCenter(
                marker.LocalCell, side, cellSize, 0.55f);
            doorOpen.transform.localScale = LevelGenWorldTransform.DoorEdgeSize(side, cellSize);

            var doorClosed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorClosed.name = "ClosedVisual";
            doorClosed.transform.SetParent(artRoot, false);
            doorClosed.transform.localPosition = doorOpen.transform.localPosition;
            doorClosed.transform.localScale = Vector3.Scale(
                doorOpen.transform.localScale,
                new Vector3(1f, 1.4f, 0.6f));
            doorClosed.SetActive(false);

            marker.OpenVisual = doorOpen;
            marker.ClosedVisual = doorClosed;
        }

        static void TryAppendToCatalog(GameObject prefab)
        {
            const string catalogPath = "Assets/LevelGen/RoomCatalog.asset";
            RoomCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<RoomCatalogAsset>(catalogPath);
            if (catalog == null)
                return;

            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                if (catalog.Entries[i].RoomPrefab == prefab)
                    return;
            }

            catalog.Entries.Add(new RoomCatalogSourceEntry { RoomPrefab = prefab });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelGen] Added '{prefab.name}' to {catalogPath}.");
        }
    }
}

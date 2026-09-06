using System.Collections.Generic;
using System.IO;
using THPerfection.LevelGen.Authoring;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace THPerfection.LevelGen.Editor
{
  public static class RoomTemplatePrefabBuilder
  {
    const string DefaultTilePath = "Assets/voxFIles/rooms/tile.vox";
    const string DefaultWallPath = "Assets/voxFIles/rooms/wall.vox";
    const string DefaultDoorOpenPath = "Assets/voxFIles/rooms/doorOpen.vox";
    const string DefaultDoorClosedPath = "Assets/voxFIles/rooms/doorClosed.vox";

    public static GameObject BuildSceneHierarchy(RoomTemplateDesign design)
    {
      ResolveDefaultPrefabs(design);

      string roomId = ResolveRoomId(design);
      var root = new GameObject(roomId);
      var authoring = root.AddComponent<RoomTemplateAuthoring>();
      authoring.TemplateId = roomId;
      authoring.AllowedFloors = design.AllowedFloors;
      authoring.BaseWeight = design.BaseWeight;
      authoring.CellSize = design.CellSize;

      Transform markersRoot = CreateChild(root.transform, "Markers");
      Transform cellsRoot = CreateChild(markersRoot, "Cells");
      Transform doorsRoot = CreateChild(markersRoot, "Doors");
      Transform artRoot = CreateChild(root.transform, "Art");

      authoring.MarkersRoot = markersRoot;

      var cellSet = new HashSet<Vector2Int>(design.Cells);
      design.RebuildExteriorEdges();

      foreach (Vector2Int cell in design.Cells)
      {
        Vector3 cellLocal = CellLocalPosition(cell, design.CellSize);
        string cellName = $"Cell_{cell.x}_{cell.y}";

        Transform markerCell = CreateChild(cellsRoot, cellName);
        markerCell.localPosition = cellLocal;
        var cellMarker = markerCell.gameObject.AddComponent<RoomCellMarker>();
        cellMarker.SnapLocalCellFromTransform = true;

        Transform artCell = CreateChild(artRoot, cellName);
        artCell.localPosition = cellLocal;

        CreateFloorSlot(artCell, design);

        foreach (DoorSide side in AllSides())
        {
          Vector2Int neighbor = cell + SideOffset(side);
          if (cellSet.Contains(neighbor))
            continue;

          if (!design.TryGetEdge(cell, side, out RoomTemplateEdge edge))
            edge = new RoomTemplateEdge { Cell = cell, Side = side, Kind = RoomEdgeKind.Wall };

          Transform wallSlot = CreateWallSlot(artCell, side);

          if (edge.Kind == RoomEdgeKind.Door)
          {
            GameObject openGo = CreateDoorVisualChild(wallSlot, design.DoorOpenPrefab, "doorOpen", design.InstantiatePlaceholderPrefabs);
            GameObject closedGo = CreateDoorVisualChild(wallSlot, design.DoorClosedPrefab, "doorClosed", design.InstantiatePlaceholderPrefabs);
            // Keep both active. Inactive doorClosed bakes with Disabled and ECS
            // Instantiate/DisableRendering cannot show those meshes at runtime.

            Transform doorMarker = CreateChild(doorsRoot, $"Door_{side}_{cell.x}_{cell.y}");
            doorMarker.localPosition = Vector3.zero;
            var socket = doorMarker.gameObject.AddComponent<DoorSocketMarker>();
            socket.LocalCell = new int2(cell.x, cell.y);
            socket.Side = side;
            socket.IsMainDoor = edge.IsMainDoor;
            socket.OpenVisual = openGo;
            socket.ClosedVisual = closedGo;
          }
          else
          {
            CreateWallVisualChild(wallSlot, design.WallPrefab, design.InstantiatePlaceholderPrefabs);
          }
        }
      }

      if (!authoring.TryBake(out _, out string error))
        Debug.LogWarning($"[LevelGen] Generated hierarchy failed bake validation: {error}", root);

      return root;
    }

    public static GameObject GeneratePrefab(RoomTemplateDesign design, out string prefabPath)
    {
      GameObject root = BuildSceneHierarchy(design);
      try
      {
        Directory.CreateDirectory(design.OutputFolder);
        prefabPath = Path.Combine(design.OutputFolder, $"{ResolveRoomId(design)}.prefab").Replace('\\', '/');
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        if (design.AddToCatalog)
          TryAppendToCatalog(prefab);

        return prefab;
      }
      finally
      {
        Object.DestroyImmediate(root);
      }
    }

    public static void ImportFromPrefab(RoomTemplateDesign design, GameObject prefab)
    {
      var authoring = prefab.GetComponent<RoomTemplateAuthoring>();
      if (authoring == null)
      {
        Debug.LogError("[LevelGen] Prefab has no RoomTemplateAuthoring.", prefab);
        return;
      }

      design.TemplateId = !string.IsNullOrWhiteSpace(authoring.TemplateId)
        ? authoring.TemplateId.Trim()
        : prefab.name;
      design.AllowedFloors = authoring.AllowedFloors;
      design.BaseWeight = authoring.BaseWeight;
      design.CellSize = authoring.CellSize;
      design.Cells.Clear();
      design.Edges.Clear();

      foreach (RoomCellMarker marker in authoring.GetCellMarkers())
      {
        int2 local = marker.ResolveLocalCell(authoring.CellSize);
        design.Cells.Add(new Vector2Int(local.x, local.y));
      }

      foreach (DoorSocketMarker door in authoring.GetDoorMarkers())
      {
        design.Edges.Add(new RoomTemplateEdge
        {
          Cell = new Vector2Int(door.LocalCell.x, door.LocalCell.y),
          Side = door.Side,
          Kind = RoomEdgeKind.Door,
          IsMainDoor = door.IsMainDoor,
        });
      }

      design.RebuildExteriorEdges();

      foreach (DoorSocketMarker door in authoring.GetDoorMarkers())
      {
        design.SetEdgeKind(new Vector2Int(door.LocalCell.x, door.LocalCell.y), door.Side, RoomEdgeKind.Door);
        if (door.IsMainDoor)
          design.SetMainDoor(new Vector2Int(door.LocalCell.x, door.LocalCell.y), door.Side);
      }

      EditorUtility.SetDirty(design);
    }

    static string ResolveRoomId(RoomTemplateDesign design) =>
      string.IsNullOrWhiteSpace(design.TemplateId) ? "new_room" : design.TemplateId.Trim();

    static void ResolveDefaultPrefabs(RoomTemplateDesign design)
    {
      design.FloorPrefab ??= LoadPrefab(DefaultTilePath);
      design.WallPrefab ??= LoadPrefab(DefaultWallPath);
      design.DoorOpenPrefab ??= LoadPrefab(DefaultDoorOpenPath);
      design.DoorClosedPrefab ??= LoadPrefab(DefaultDoorClosedPath);
    }

    static GameObject LoadPrefab(string path) =>
      AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static Transform CreateChild(Transform parent, string name)
    {
      var go = new GameObject(name);
      go.transform.SetParent(parent, false);
      return go.transform;
    }

    static Vector3 CellLocalPosition(Vector2Int cell, float cellSize) =>
      new(cell.x * cellSize, 0f, cell.y * cellSize);

    static Transform CreateWallSlot(Transform artCell, DoorSide side)
    {
      Transform wallSlot = CreateChild(artCell, $"Wall_{side}");
      wallSlot.localRotation = WallRotation(side);
      return wallSlot;
    }

    static void CreateFloorSlot(Transform artCell, RoomTemplateDesign design)
    {
      Transform floorSlot = CreateChild(artCell, "tile");
      if (design.InstantiatePlaceholderPrefabs && design.FloorPrefab != null)
        PrefabUtility.InstantiatePrefab(design.FloorPrefab, floorSlot);
    }

    static void CreateWallVisualChild(Transform wallSlot, GameObject wallPrefab, bool instantiate)
    {
      if (instantiate && wallPrefab != null)
        PrefabUtility.InstantiatePrefab(wallPrefab, wallSlot);
    }

    static GameObject CreateDoorVisualChild(
      Transform wallSlot,
      GameObject prefab,
      string childName,
      bool instantiate)
    {
      if (instantiate && prefab != null)
      {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wallSlot);
        if (instance != null)
          instance.name = childName;
        return instance;
      }

      return CreateChild(wallSlot, childName).gameObject;
    }

    public static Quaternion WallRotation(DoorSide side) => side switch
    {
      DoorSide.South => Quaternion.identity,
      DoorSide.North => Quaternion.Euler(0f, 180f, 0f),
      DoorSide.West => Quaternion.Euler(0f, 90f, 0f),
      DoorSide.East => Quaternion.Euler(0f, -90f, 0f),
      _ => Quaternion.identity,
    };

    static Vector2Int SideOffset(DoorSide side) => side switch
    {
      DoorSide.North => new Vector2Int(0, 1),
      DoorSide.East => new Vector2Int(1, 0),
      DoorSide.South => new Vector2Int(0, -1),
      DoorSide.West => new Vector2Int(-1, 0),
      _ => Vector2Int.zero,
    };

    static IEnumerable<DoorSide> AllSides()
    {
      yield return DoorSide.North;
      yield return DoorSide.East;
      yield return DoorSide.South;
      yield return DoorSide.West;
    }

    static void TryAppendToCatalog(GameObject prefab)
    {
      const string catalogPath = "Assets/LevelGen/RoomCatalog.asset";
      var catalog = AssetDatabase.LoadAssetAtPath<RoomCatalogAsset>(catalogPath);
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
    }
  }
}

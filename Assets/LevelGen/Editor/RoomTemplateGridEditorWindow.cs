using THPerfection.LevelGen.Authoring;
using UnityEditor;
using UnityEngine;

namespace THPerfection.LevelGen.Editor
{
  public sealed class RoomTemplateGridEditorWindow : EditorWindow
  {
    enum PaintTool
    {
      Cells,
      Edges,
    }

    RoomTemplateDesign _design;
    PaintTool _tool = PaintTool.Cells;
    Vector2 _scroll;
    float _cellPixelSize = 44f;
    bool _showSettings = true;
    string _bakeStatus = "Click Validate Bake to check template rules.";

    [MenuItem("TH Perfection/Level Gen/Room Template Grid Builder")]
    public static void Open()
    {
      var window = GetWindow<RoomTemplateGridEditorWindow>("Room Grid Builder");
      window.minSize = new Vector2(420f, 520f);
      window.Show();
    }

    void OnGUI()
    {
      DrawToolbar();

      if (_design == null)
      {
        EditorGUILayout.HelpBox(
          "Assign or create a Room Template Design asset to paint the room shape.",
          MessageType.Info);
        DrawDesignPicker();
        return;
      }

      DrawDesignPicker();
      EditorGUILayout.Space(6f);

      if (_showSettings)
        DrawSettings();

      EditorGUILayout.Space(6f);
      DrawToolBar();
      EditorGUILayout.Space(4f);
      DrawLegend();
      EditorGUILayout.Space(6f);

      _scroll = EditorGUILayout.BeginScrollView(_scroll);
      DrawGrid();
      EditorGUILayout.EndScrollView();

      DrawBakeStatus();

      EditorGUILayout.Space(8f);
      DrawActions();
    }

    void DrawToolbar()
    {
      EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
      if (GUILayout.Button("New Design Asset", EditorStyles.toolbarButton))
        CreateDesignAsset();

      if (GUILayout.Button("1×1 North", EditorStyles.toolbarButton))
        ApplyPresetOneByOneNorth();

      if (GUILayout.Button("2×1 Hall", EditorStyles.toolbarButton))
        ApplyPresetTwoByOneHall();

      _showSettings = GUILayout.Toggle(_showSettings, "Settings", EditorStyles.toolbarButton);
      EditorGUILayout.EndHorizontal();
    }

    void DrawDesignPicker()
    {
      EditorGUILayout.BeginHorizontal();
      _design = (RoomTemplateDesign)EditorGUILayout.ObjectField(
        "Design",
        _design,
        typeof(RoomTemplateDesign),
        false);

      if (GUILayout.Button("Create", GUILayout.Width(64f)))
        CreateDesignAsset();

      EditorGUILayout.EndHorizontal();
    }

    void DrawSettings()
    {
      EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);
      _design.TemplateId = EditorGUILayout.TextField(
        new GUIContent("Room Id", "Used for catalog template id and generated prefab filename."),
        _design.TemplateId);
      _design.CellSize = EditorGUILayout.FloatField("Cell Size", _design.CellSize);
      _design.BaseWeight = EditorGUILayout.FloatField("Base Weight", _design.BaseWeight);
      _design.AllowedFloors = (FloorMask)EditorGUILayout.EnumFlagsField("Allowed Floors", _design.AllowedFloors);

      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
      _design.GridWidth = Mathf.Clamp(EditorGUILayout.IntField("Width", _design.GridWidth), 1, 24);
      _design.GridHeight = Mathf.Clamp(EditorGUILayout.IntField("Height", _design.GridHeight), 1, 24);
      _cellPixelSize = EditorGUILayout.Slider("Cell Zoom", _cellPixelSize, 28f, 72f);

      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
      _design.OutputFolder = EditorGUILayout.TextField("Output Folder", _design.OutputFolder);
      _design.InstantiatePlaceholderPrefabs = EditorGUILayout.Toggle(
        "Instantiate Placeholder Prefabs",
        _design.InstantiatePlaceholderPrefabs);
      _design.AddToCatalog = EditorGUILayout.Toggle("Add To Room Catalog", _design.AddToCatalog);

      _design.FloorPrefab = (GameObject)EditorGUILayout.ObjectField(
        "Floor Prefab", _design.FloorPrefab, typeof(GameObject), false);
      _design.WallPrefab = (GameObject)EditorGUILayout.ObjectField(
        "Wall Prefab", _design.WallPrefab, typeof(GameObject), false);
      _design.DoorOpenPrefab = (GameObject)EditorGUILayout.ObjectField(
        "Door Open Prefab", _design.DoorOpenPrefab, typeof(GameObject), false);
      _design.DoorClosedPrefab = (GameObject)EditorGUILayout.ObjectField(
        "Door Closed Prefab", _design.DoorClosedPrefab, typeof(GameObject), false);

      if (GUI.changed)
        EditorUtility.SetDirty(_design);
    }

    void DrawToolBar()
    {
      EditorGUILayout.BeginHorizontal();
      _tool = (PaintTool)GUILayout.Toolbar((int)_tool, new[] { "Paint Cells", "Paint Edges" });
      if (GUILayout.Button("Clear Grid", GUILayout.Width(80f)))
      {
        Undo.RecordObject(_design, "Clear Room Grid");
        _design.Cells.Clear();
        _design.Edges.Clear();
        EditorUtility.SetDirty(_design);
      }

      EditorGUILayout.EndHorizontal();
    }

    void DrawLegend()
    {
      EditorGUILayout.HelpBox(
        "Cells: click to toggle occupancy.\n"
        + "Edges: click exterior edge to cycle Wall → Door. Alt+click a door edge to set Main door.\n"
        + "Generated prefabs match your examples: Markers/Cells, Markers/Doors, Art/Cell_x_y/Wall_Side (rotated slots).",
        MessageType.None);
    }

    void DrawGrid()
    {
      float pad = 12f;
      float gridW = _design.GridWidth * _cellPixelSize;
      float gridH = _design.GridHeight * _cellPixelSize;
      var gridRect = GUILayoutUtility.GetRect(gridW + pad * 2f, gridH + pad * 2f);

      if (Event.current.type == EventType.Repaint)
        EditorGUI.DrawRect(gridRect, new Color(0.16f, 0.16f, 0.16f, 1f));

      var origin = new Vector2(gridRect.x + pad, gridRect.y + pad);

      for (int y = 0; y < _design.GridHeight; y++)
      {
        for (int x = 0; x < _design.GridWidth; x++)
        {
          var cell = new Vector2Int(x, y);
          var cellRect = CellRect(origin, x, y);
          DrawCell(cell, cellRect);
        }
      }
    }

    void DrawCell(Vector2Int cell, Rect cellRect)
    {
      bool occupied = _design.IsCellOccupied(cell);

      if (Event.current.type == EventType.Repaint)
      {
        Color fill = occupied ? new Color(0.25f, 0.5f, 0.85f, 0.85f) : new Color(0.22f, 0.22f, 0.22f, 1f);
        EditorGUI.DrawRect(cellRect, fill);
        Handles.color = new Color(0f, 0f, 0f, 0.35f);
        Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.black);
      }

      if (_tool == PaintTool.Cells)
      {
        if (GUI.Button(cellRect, GUIContent.none, GUIStyle.none))
        {
          Undo.RecordObject(_design, "Paint Room Cell");
          _design.ToggleCell(cell);
          EditorUtility.SetDirty(_design);
          Event.current.Use();
        }
      }
      else if (occupied)
      {
        DrawEdgeButtons(cell, cellRect);
      }

      var labelRect = new Rect(cellRect.x + 2f, cellRect.y + 2f, cellRect.width, 14f);
      GUI.Label(labelRect, $"{cell.x},{cell.y}", EditorStyles.miniLabel);
    }

    void DrawEdgeButtons(Vector2Int cell, Rect cellRect)
    {
      float t = Mathf.Clamp(_cellPixelSize * 0.22f, 8f, 16f);

      DrawEdge(cell, DoorSide.North, Inset(cellRect, 0f, 0f, 0f, cellRect.height - t));
      DrawEdge(cell, DoorSide.South, Inset(cellRect, 0f, cellRect.height - t, 0f, 0f));
      DrawEdge(cell, DoorSide.West, Inset(cellRect, 0f, 0f, cellRect.width - t, 0f));
      DrawEdge(cell, DoorSide.East, Inset(cellRect, cellRect.width - t, 0f, 0f, 0f));
    }

    void DrawEdge(Vector2Int cell, DoorSide side, Rect edgeRect)
    {
      if (!_design.TryGetEdge(cell, side, out RoomTemplateEdge edge))
        return;

      if (Event.current.type == EventType.Repaint)
      {
        Color color = edge.Kind switch
        {
          RoomEdgeKind.Door when edge.IsMainDoor => new Color(0.2f, 0.95f, 0.35f, 0.95f),
          RoomEdgeKind.Door => new Color(1f, 0.85f, 0.1f, 0.95f),
          _ => new Color(0.85f, 0.25f, 0.2f, 0.85f),
        };
        EditorGUI.DrawRect(edgeRect, color);
      }

      if (GUI.Button(edgeRect, GUIContent.none, GUIStyle.none))
      {
        Undo.RecordObject(_design, "Paint Room Edge");
        if (Event.current.alt && edge.Kind == RoomEdgeKind.Door)
          _design.SetMainDoor(cell, side);
        else
          _design.CycleEdgeKind(cell, side);

        EditorUtility.SetDirty(_design);
        Event.current.Use();
      }
    }

    void DrawBakeStatus()
    {
      EditorGUILayout.LabelField("Bake Preview", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(_bakeStatus, MessageType.None);
      if (GUILayout.Button("Validate Bake"))
        ValidateBake();
    }

    void ValidateBake()
    {
      if (_design == null)
        return;

      _design.RebuildExteriorEdges();
      GameObject temp = RoomTemplatePrefabBuilder.BuildSceneHierarchy(_design);
      try
      {
        var authoring = temp.GetComponent<RoomTemplateAuthoring>();
        if (authoring.TryBake(out var template, out string error))
          _bakeStatus = $"OK — {RoomTemplateBaker.Describe(template)}";
        else
          _bakeStatus = error;
      }
      finally
      {
        DestroyImmediate(temp);
      }
    }

    void DrawActions()
    {
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button("Import From Selected Prefab", GUILayout.Height(28f)))
        ImportSelectedPrefab();

      if (GUILayout.Button("Preview In Scene", GUILayout.Height(28f)))
        PreviewInScene();

      if (GUILayout.Button("Generate Prefab", GUILayout.Height(28f)))
      {
        ValidateBake();
        GeneratePrefab();
      }

      EditorGUILayout.EndHorizontal();
    }

    void CreateDesignAsset()
    {
      string path = EditorUtility.SaveFilePanelInProject(
        "Create Room Template Design",
        "RoomTemplateDesign",
        "asset",
        "Choose a location for the design asset.",
        "Assets/LevelGen");

      if (string.IsNullOrEmpty(path))
        return;

      var asset = CreateInstance<RoomTemplateDesign>();
      AssetDatabase.CreateAsset(asset, path);
      AssetDatabase.SaveAssets();
      _design = asset;
      Selection.activeObject = asset;
    }

    void ApplyPresetOneByOneNorth()
    {
      EnsureDesign();
      Undo.RecordObject(_design, "Apply 1x1 North Preset");
      _design.TemplateId = "one_by_one_north";
      _design.Cells.Clear();
      _design.SetCellOccupied(new Vector2Int(0, 0), true);
      _design.SetEdgeKind(new Vector2Int(0, 0), DoorSide.North, RoomEdgeKind.Door);
      _design.SetMainDoor(new Vector2Int(0, 0), DoorSide.North);
      EditorUtility.SetDirty(_design);
    }

    void ApplyPresetTwoByOneHall()
    {
      EnsureDesign();
      Undo.RecordObject(_design, "Apply 2x1 Hall Preset");
      _design.TemplateId = "two_by_one_hall";
      _design.Cells.Clear();
      _design.SetCellOccupied(new Vector2Int(0, 0), true);
      _design.SetCellOccupied(new Vector2Int(1, 0), true);
      _design.SetEdgeKind(new Vector2Int(0, 0), DoorSide.West, RoomEdgeKind.Door);
      _design.SetMainDoor(new Vector2Int(0, 0), DoorSide.West);
      _design.SetEdgeKind(new Vector2Int(1, 0), DoorSide.East, RoomEdgeKind.Door);
      EditorUtility.SetDirty(_design);
    }

    void ImportSelectedPrefab()
    {
      EnsureDesign();
      GameObject prefab = Selection.activeObject as GameObject;
      if (prefab == null || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
      {
        string path = EditorUtility.OpenFilePanel(
          "Import Room Prefab",
          "Assets/LevelGen/RoomTemplates",
          "prefab");
        if (string.IsNullOrEmpty(path))
          return;

        path = FileUtil.GetProjectRelativePath(path);
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      }

      if (prefab == null)
      {
        EditorUtility.DisplayDialog("Import Failed", "Select a room template prefab.", "OK");
        return;
      }

      Undo.RecordObject(_design, "Import Room Prefab");
      RoomTemplatePrefabBuilder.ImportFromPrefab(_design, prefab);
    }

    void PreviewInScene()
    {
      EnsureDesign();
      GameObject root = RoomTemplatePrefabBuilder.BuildSceneHierarchy(_design);
      Selection.activeGameObject = root;
      SceneView.lastActiveSceneView?.FrameSelected();
    }

    void GeneratePrefab()
    {
      EnsureDesign();
      _design.RebuildExteriorEdges();
      ValidateBake();

      if (!_bakeStatus.StartsWith("OK"))
      {
        EditorUtility.DisplayDialog("Generate Blocked", _bakeStatus, "OK");
        return;
      }

      GameObject prefab = RoomTemplatePrefabBuilder.GeneratePrefab(_design, out string path);
      if (prefab == null)
      {
        EditorUtility.DisplayDialog("Generate Failed", "Could not create prefab.", "OK");
        return;
      }

      EditorUtility.DisplayDialog(
        "Room Template Generated",
        $"Saved to:\n{path}",
        "OK");
      Selection.activeObject = prefab;
      EditorGUIUtility.PingObject(prefab);
    }

    void EnsureDesign()
    {
      if (_design != null)
        return;

      CreateDesignAsset();
    }

    Rect CellRect(Vector2 origin, int x, int y)
    {
      float px = origin.x + x * _cellPixelSize;
      float py = origin.y + (_design.GridHeight - 1 - y) * _cellPixelSize;
      return new Rect(px, py, _cellPixelSize, _cellPixelSize);
    }

    static Rect Inset(Rect rect, float left, float top, float right, float bottom) =>
      new(rect.x + left, rect.y + top, rect.width - left - right, rect.height - top - bottom);
  }
}

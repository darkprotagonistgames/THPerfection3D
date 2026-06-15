using System;
using System.Collections.Generic;
using UnityEngine;

namespace THPerfection.LevelGen.Authoring
{
  public enum RoomEdgeKind : byte
  {
    Wall = 0,
    Door = 1,
  }

  [Serializable]
  public struct RoomTemplateEdge
  {
    public Vector2Int Cell;
    public DoorSide Side;
    public RoomEdgeKind Kind;
    public bool IsMainDoor;
  }

  [CreateAssetMenu(fileName = "RoomTemplateDesign", menuName = "TH Perfection/Level Gen/Room Template Design")]
  public sealed class RoomTemplateDesign : ScriptableObject
  {
    [Header("Identity")]
    public string TemplateId = "new_room";
    public string PrefabName = "Room_New";
    public FloorMask AllowedFloors = FloorMask.Main;
    public float BaseWeight = 1f;
    public float CellSize = BuildingGenConfig.DefaultCellSize;

    [Header("Grid")]
    public int GridWidth = 8;
    public int GridHeight = 8;

    [Tooltip("Grid origin is bottom-left. Serialized occupied cells.")]
    public List<Vector2Int> Cells = new();

    [Tooltip("Exterior edges only (wall or door).")]
    public List<RoomTemplateEdge> Edges = new();

    [Header("Prefab generation")]
    public string OutputFolder = "Assets/LevelGen/RoomTemplates";
    public bool InstantiatePlaceholderPrefabs = true;
    public bool AddToCatalog = true;

    [Header("Optional art prefabs (defaults loaded from vox rooms folder)")]
    public GameObject FloorPrefab;
    public GameObject WallPrefab;
    public GameObject DoorOpenPrefab;
    public GameObject DoorClosedPrefab;

    public bool IsCellOccupied(Vector2Int cell) => Cells.Contains(cell);

    public void SetCellOccupied(Vector2Int cell, bool occupied)
    {
      if (occupied)
      {
        if (!Cells.Contains(cell))
          Cells.Add(cell);
      }
      else
      {
        Cells.Remove(cell);
        Edges.RemoveAll(e => e.Cell == cell);
      }

      RebuildExteriorEdges();
    }

    public void ToggleCell(Vector2Int cell) => SetCellOccupied(cell, !IsCellOccupied(cell));

    public void RebuildExteriorEdges()
    {
      var cellSet = new HashSet<Vector2Int>(Cells);
      var existing = new Dictionary<(int x, int y, DoorSide side), RoomTemplateEdge>();
      foreach (RoomTemplateEdge edge in Edges)
        existing[(edge.Cell.x, edge.Cell.y, edge.Side)] = edge;

      Edges.Clear();
      int mainCount = 0;
      RoomTemplateEdge? mainEdge = null;

      foreach (Vector2Int cell in Cells)
      {
        TryAddEdge(Edges, cell, DoorSide.North, cellSet, existing, ref mainCount, ref mainEdge);
        TryAddEdge(Edges, cell, DoorSide.East, cellSet, existing, ref mainCount, ref mainEdge);
        TryAddEdge(Edges, cell, DoorSide.South, cellSet, existing, ref mainCount, ref mainEdge);
        TryAddEdge(Edges, cell, DoorSide.West, cellSet, existing, ref mainCount, ref mainEdge);
      }

      if (mainCount == 0 && Edges.Count > 0)
      {
        RoomTemplateEdge first = Edges[0];
        first.Kind = RoomEdgeKind.Door;
        first.IsMainDoor = true;
        Edges[0] = first;
      }
      else if (mainCount > 1 && mainEdge.HasValue)
      {
        for (int i = 0; i < Edges.Count; i++)
        {
          RoomTemplateEdge edge = Edges[i];
          bool isMain = edge.Cell == mainEdge.Value.Cell
            && edge.Side == mainEdge.Value.Side;
          edge.IsMainDoor = isMain;
          Edges[i] = edge;
        }
      }
    }

    static void TryAddEdge(
      List<RoomTemplateEdge> edges,
      Vector2Int cell,
      DoorSide side,
      HashSet<Vector2Int> cellSet,
      Dictionary<(int x, int y, DoorSide side), RoomTemplateEdge> existing,
      ref int mainCount,
      ref RoomTemplateEdge? mainEdge)
    {
      Vector2Int neighbor = cell + ToOffset(side);
      if (cellSet.Contains(neighbor))
        return;

      var key = (cell.x, cell.y, side);
      RoomTemplateEdge edge = existing.TryGetValue(key, out RoomTemplateEdge kept)
        ? kept
        : new RoomTemplateEdge { Cell = cell, Side = side, Kind = RoomEdgeKind.Wall };

      if (edge.IsMainDoor)
      {
        mainCount++;
        mainEdge = edge;
      }

      // keep at most one main door when rebuilding
      if (edge.Kind == RoomEdgeKind.Door && edge.IsMainDoor)
        mainEdge = edge;

      edges.Add(edge);
    }

    public bool TryGetEdge(Vector2Int cell, DoorSide side, out RoomTemplateEdge edge)
    {
      for (int i = 0; i < Edges.Count; i++)
      {
        if (Edges[i].Cell == cell && Edges[i].Side == side)
        {
          edge = Edges[i];
          return true;
        }
      }

      edge = default;
      return false;
    }

    public void SetEdgeKind(Vector2Int cell, DoorSide side, RoomEdgeKind kind)
    {
      for (int i = 0; i < Edges.Count; i++)
      {
        if (Edges[i].Cell != cell || Edges[i].Side != side)
          continue;

        RoomTemplateEdge edge = Edges[i];
        edge.Kind = kind;
        if (kind == RoomEdgeKind.Wall)
          edge.IsMainDoor = false;
        Edges[i] = edge;
        return;
      }
    }

    public void SetMainDoor(Vector2Int cell, DoorSide side)
    {
      for (int i = 0; i < Edges.Count; i++)
      {
        RoomTemplateEdge edge = Edges[i];
        edge.IsMainDoor = edge.Cell == cell && edge.Side == side && edge.Kind == RoomEdgeKind.Door;
        Edges[i] = edge;
      }
    }

    public void CycleEdgeKind(Vector2Int cell, DoorSide side)
    {
      if (!TryGetEdge(cell, side, out RoomTemplateEdge edge))
        return;

      edge.Kind = edge.Kind == RoomEdgeKind.Wall ? RoomEdgeKind.Door : RoomEdgeKind.Wall;
      if (edge.Kind == RoomEdgeKind.Wall)
        edge.IsMainDoor = false;

      for (int i = 0; i < Edges.Count; i++)
      {
        if (Edges[i].Cell == cell && Edges[i].Side == side)
        {
          Edges[i] = edge;
          break;
        }
      }

      if (edge.Kind == RoomEdgeKind.Door && !Edges.Exists(e => e.IsMainDoor))
        SetMainDoor(cell, side);
    }

    static Vector2Int ToOffset(DoorSide side) => side switch
    {
      DoorSide.North => new Vector2Int(0, 1),
      DoorSide.East => new Vector2Int(1, 0),
      DoorSide.South => new Vector2Int(0, -1),
      DoorSide.West => new Vector2Int(-1, 0),
      _ => Vector2Int.zero,
    };
  }
}

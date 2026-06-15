using System;
using System.Collections.Generic;
using System.Text;
using THPerfection.LevelGen.Authoring;
using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public static class RoomTemplateBaker
    {
        static readonly int2[] NeighborDirs =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        };

        public static bool TryBakeFromAuthoring(
            RoomTemplateAuthoring authoring,
            out RoomTemplateDefinition definition,
            out string error)
        {
            definition = default;
            error = null;

            if (authoring == null)
            {
                error = "RoomTemplateAuthoring is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(authoring.TemplateId))
            {
                error = "TemplateId is required.";
                return false;
            }

            if (authoring.CellSize <= 0f)
            {
                error = "CellSize must be greater than zero.";
                return false;
            }

            RoomCellMarker[] cellMarkers = authoring.GetCellMarkers();
            DoorSocketMarker[] doorMarkers = authoring.GetDoorMarkers();

            if (cellMarkers.Length == 0)
            {
                error = "Add at least one RoomCellMarker under MarkersRoot.";
                return false;
            }

            var cells = new int2[cellMarkers.Length];
            for (int i = 0; i < cellMarkers.Length; i++)
                cells[i] = cellMarkers[i].ResolveLocalCell(authoring.CellSize);

            DoorSocket[] doorSockets = BuildDoorSockets(doorMarkers);

            if (!TryValidate(
                    authoring.TemplateId.Trim(),
                    authoring.AllowedFloors,
                    authoring.BaseWeight,
                    cells,
                    doorSockets,
                    out error,
                    out int2 mainDoorCell,
                    out DoorSide mainDoorSide))
                return false;

            definition = new RoomTemplateDefinition(
                authoring.TemplateId.Trim(),
                authoring.AllowedFloors,
                authoring.BaseWeight,
                cells,
                doorSockets,
                mainDoorCell,
                mainDoorSide);

            return true;
        }

        public static bool TryValidate(
            string templateId,
            FloorMask allowedFloors,
            float baseWeight,
            int2[] cells,
            DoorSocket[] doorSockets,
            out string error,
            out int2 mainDoorCell,
            out DoorSide mainDoorSide)
        {
            definitionDefaults(out mainDoorCell, out mainDoorSide);
            error = null;

            if (string.IsNullOrWhiteSpace(templateId))
            {
                error = "TemplateId is required.";
                return false;
            }

            if (allowedFloors == FloorMask.None)
            {
                error = "AllowedFloors must include at least one floor.";
                return false;
            }

            if (baseWeight < 0f)
            {
                error = "BaseWeight cannot be negative.";
                return false;
            }

            if (cells == null || cells.Length == 0)
            {
                error = "At least one cell is required.";
                return false;
            }

            var cellSet = new HashSet<int2>();
            for (int i = 0; i < cells.Length; i++)
            {
                if (!cellSet.Add(cells[i]))
                {
                    error = $"Duplicate cell at ({cells[i].x}, {cells[i].y}).";
                    return false;
                }
            }

            if (!IsOrthogonallyConnected(cellSet))
            {
                error = "Cells must form one connected polyomino.";
                return false;
            }

            if (doorSockets == null || doorSockets.Length == 0)
            {
                error = "At least one door socket is required.";
                return false;
            }

            int mainDoorCount = 0;
            var doorKeys = new HashSet<(int x, int y, DoorSide side)>();

            for (int i = 0; i < doorSockets.Length; i++)
            {
                DoorSocket socket = doorSockets[i];

                if (!cellSet.Contains(socket.Cell))
                {
                    error = $"Door on ({socket.Cell.x}, {socket.Cell.y}) {socket.Side} is not on a marked cell.";
                    return false;
                }

                if (!doorKeys.Add((socket.Cell.x, socket.Cell.y, socket.Side)))
                {
                    error = $"Duplicate door on ({socket.Cell.x}, {socket.Cell.y}) {socket.Side}.";
                    return false;
                }

                int2 neighbor = socket.Cell + GridTransforms.Direction(socket.Side);
                if (cellSet.Contains(neighbor))
                {
                    error = $"Door on ({socket.Cell.x}, {socket.Cell.y}) {socket.Side} must face outward (not into another cell).";
                    return false;
                }

                if (socket.IsMainDoor)
                {
                    mainDoorCount++;
                    mainDoorCell = socket.Cell;
                    mainDoorSide = socket.Side;
                }
            }

            if (mainDoorCount != 1)
            {
                error = $"Exactly one main door is required (found {mainDoorCount}).";
                return false;
            }

            return true;
        }

        static void definitionDefaults(out int2 mainDoorCell, out DoorSide mainDoorSide)
        {
            mainDoorCell = int2.zero;
            mainDoorSide = DoorSide.North;
        }

        static DoorSocket[] BuildDoorSockets(DoorSocketMarker[] doorMarkers)
        {
            if (doorMarkers == null || doorMarkers.Length == 0)
                return System.Array.Empty<DoorSocket>();

            var sockets = new DoorSocket[doorMarkers.Length];
            for (int i = 0; i < doorMarkers.Length; i++)
            {
                DoorSocketMarker marker = doorMarkers[i];
                sockets[i] = new DoorSocket(marker.LocalCell, marker.Side, marker.IsMainDoor);
            }

            return sockets;
        }

        static bool IsOrthogonallyConnected(HashSet<int2> cellSet)
        {
            if (cellSet.Count <= 1)
                return true;

            var visited = new HashSet<int2>();
            var queue = new Queue<int2>();

            using var enumerator = cellSet.GetEnumerator();
            enumerator.MoveNext();
            int2 start = enumerator.Current;

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                int2 current = queue.Dequeue();

                for (int i = 0; i < NeighborDirs.Length; i++)
                {
                    int2 next = current + NeighborDirs[i];
                    if (!cellSet.Contains(next) || !visited.Add(next))
                        continue;

                    queue.Enqueue(next);
                }
            }

            return visited.Count == cellSet.Count;
        }

        public static string Describe(in RoomTemplateDefinition template)
        {
            var sb = new StringBuilder();
            sb.Append(template.TemplateId);
            sb.Append(" | cells=");
            sb.Append(template.Cells.Length);
            sb.Append(" doors=");
            sb.Append(template.DoorSockets.Length);
            sb.Append(" main=");
            sb.Append(template.MainDoorSide);
            sb.Append('@');
            sb.Append('(').Append(template.MainDoorCell.x).Append(',').Append(template.MainDoorCell.y).Append(')');
            return sb.ToString();
        }
    }
}

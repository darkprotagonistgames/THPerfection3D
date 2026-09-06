using System.Text;
using THPerfection.LevelGen.Authoring;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace THPerfection.LevelGen.Editor
{
    [CustomEditor(typeof(RoomTemplateAuthoring))]
    public sealed class RoomTemplateAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var authoring = (RoomTemplateAuthoring)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Bake Preview", EditorStyles.boldLabel);

            if (authoring.TryBake(out RoomTemplateDefinition template, out string error))
            {
                EditorGUILayout.HelpBox(RoomTemplateBaker.Describe(template), MessageType.Info);
                EditorGUILayout.LabelField("Cells", FormatCells(template.Cells));
                EditorGUILayout.LabelField("Doors", FormatDoors(template.DoorSockets));
            }
            else
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Validate Template"))
                ValidateAndPing(authoring);

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            var authoring = (RoomTemplateAuthoring)target;
            float cellSize = Mathf.Max(0.01f, authoring.CellSize);

            Handles.color = new Color(0.2f, 0.7f, 1f, 0.35f);
            foreach (RoomCellMarker marker in authoring.GetCellMarkers())
            {
                int2 cell = marker.ResolveLocalCell(cellSize);
                Vector3 world = authoring.transform.TransformPoint(
                    new Vector3(cell.x * cellSize, 0.05f, cell.y * cellSize));
                Handles.DrawWireCube(world, new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f));
            }

            Handles.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            foreach (DoorSocketMarker door in authoring.GetDoorMarkers())
            {
                Vector3 local = LevelGenWorldTransform.DoorEdgeCenter(
                    door.LocalCell, door.Side, cellSize, 0.5f);
                Vector3 world = authoring.transform.TransformPoint(local);
                Handles.SphereHandleCap(0, world, Quaternion.identity, cellSize * 0.08f, EventType.Repaint);
            }
        }

        static void ValidateAndPing(RoomTemplateAuthoring authoring)
        {
            if (authoring.TryBake(out _, out string error))
            {
                Debug.Log($"[LevelGen] Template '{authoring.TemplateId}' is valid.", authoring);
            }
            else
            {
                Debug.LogError($"[LevelGen] Template '{authoring.name}' invalid: {error}", authoring);
                EditorGUIUtility.PingObject(authoring);
            }
        }

        static string FormatCells(int2[] cells)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('(').Append(cells[i].x).Append(',').Append(cells[i].y).Append(')');
            }

            return sb.ToString();
        }

        static string FormatDoors(DoorSocket[] doors)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < doors.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(doors[i].Side);
                sb.Append('@');
                sb.Append('(').Append(doors[i].Cell.x).Append(',').Append(doors[i].Cell.y).Append(')');
                if (doors[i].IsMainDoor) sb.Append("*");
            }

            return sb.ToString();
        }
    }
}

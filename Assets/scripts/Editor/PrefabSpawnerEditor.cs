using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrefabSpawnerAuthoring))]
public class PrefabSpawnerEditor : Editor
{
    private const float LabelOffset = 0.6f;

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
    private static void DrawPrefabPreview(PrefabSpawnerAuthoring spawner, GizmoType gizmoType)
    {
        if (spawner.Prefab == null)
            return;

        if (Event.current.type != EventType.Repaint)
            return;

        bool isSelected = (gizmoType & GizmoType.Selected) != 0;
        Gizmos.color = isSelected
            ? new Color(0.45f, 0.75f, 1f, 0.9f)
            : new Color(0.45f, 0.75f, 1f, 0.35f);
        Gizmos.matrix = spawner.transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
    }

    private void OnSceneGUI()
    {
        var spawner = (PrefabSpawnerAuthoring)target;
        if (spawner.Prefab == null)
            return;

        Handles.Label(
            spawner.transform.position + Vector3.up * LabelOffset,
            $"Spawns: {spawner.Prefab.name}",
            EditorStyles.boldLabel);
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SafeRandomSpawner))]
public class SafeRandomSpawnerEditor : Editor
{
    private static readonly Color GizmoColor = new Color(0.3f, 1f, 0.4f, 0.8f);

    private void OnSceneGUI()
    {
        var spawner = (SafeRandomSpawner)target;

        float z       = spawner.transform.position.z;
        float centerX = (spawner.BoundsMin.x + spawner.BoundsMax.x) * 0.5f;
        float centerY = (spawner.BoundsMin.y + spawner.BoundsMax.y) * 0.5f;
        float sizeX   = spawner.BoundsMax.x - spawner.BoundsMin.x;
        float sizeY   = spawner.BoundsMax.y - spawner.BoundsMin.y;

        var center = new Vector3(centerX, centerY, z);
        var size   = new Vector3(sizeX, sizeY, 0f);

        using (new Handles.DrawingScope(GizmoColor))
        {
            Handles.DrawWireCube(center, size);
        }
    }
}

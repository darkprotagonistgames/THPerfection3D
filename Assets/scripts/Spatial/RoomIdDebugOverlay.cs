using THPerfection.LevelGen;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Draws each committed layout cell's room instance id at the cell's visual center
/// (<c>cell * CellSize</c>, matching room floor pivots).
/// Toggle via <see cref="drawRoomIds"/>. Enable the Game view <b>Gizmos</b> button to see labels.
/// </summary>
[DisallowMultipleComponent]
public class RoomIdDebugOverlay : MonoBehaviour
{
    [Tooltip("When enabled, draws room instance ids at the center of each stamped layout cell. Enable Game view Gizmos to see them.")]
    public bool drawRoomIds;

    [Tooltip("Raise labels above the floor so they stay visible.")]
    public float labelHeightOffset = 2f;

#if UNITY_EDITOR
    GUIStyle _labelStyle;
#endif

    void OnDrawGizmos()
    {
        if (!drawRoomIds)
            return;

#if !UNITY_EDITOR
        return;
#else
        if (!TryGetLayout(out BuildingSpatialConfig config, out BlobAssetReference<BuildingLayoutBlob> layoutRef))
            return;

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
            };
            _labelStyle.normal.textColor = Color.yellow;
        }

        float cellSize = math.max(0.01f, config.CellSize);
        float y = config.MainFloorY + labelHeightOffset;
        ref BuildingLayoutBlob layout = ref layoutRef.Value;

        Handles.BeginGUI();
        for (int i = 0; i < layout.Cells.Length; i++)
        {
            ref BuildingLayoutCellBlob cell = ref layout.Cells[i];
            Vector3 worldPos = LevelGenWorldTransform.CellCenter(
                new int2(cell.CellX, cell.CellY),
                cellSize,
                y);

            string text = cell.RoomInstanceId.ToString();
            Vector2 size = _labelStyle.CalcSize(new GUIContent(text));
            // HandleUtility accounts for Scene/Game view correctly; center the rect on the point.
            Vector2 gui = HandleUtility.WorldToGUIPoint(worldPos);
            var rect = new Rect(gui.x - size.x * 0.5f, gui.y - size.y * 0.5f, size.x, size.y);
            GUI.Label(rect, text, _labelStyle);
        }
        Handles.EndGUI();
#endif
    }

    static bool TryGetLayout(
        out BuildingSpatialConfig config,
        out BlobAssetReference<BuildingLayoutBlob> layoutRef)
    {
        config = default;
        layoutRef = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        using var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingLayoutSingletonTag>(),
            ComponentType.ReadOnly<BuildingSpatialConfig>(),
            ComponentType.ReadOnly<BuildingLayoutBlobRef>());

        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity singleton = query.GetSingletonEntity();
        config = em.GetComponentData<BuildingSpatialConfig>(singleton);
        BuildingLayoutBlobRef blobRef = em.GetComponentData<BuildingLayoutBlobRef>(singleton);
        if (!blobRef.Layout.IsCreated)
            return false;

        layoutRef = blobRef.Layout;
        return true;
    }
}

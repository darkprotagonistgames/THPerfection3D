using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen.Authoring
{
    /// <summary>
    /// Marks one polyomino cell on a room prefab. Position children at cell centers in local space.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomCellMarker : MonoBehaviour
    {
        [Tooltip("When enabled, LocalCell is derived from this transform's local XZ / CellSize on RoomTemplateAuthoring.")]
        public bool SnapLocalCellFromTransform = true;

        public int2 ManualLocalCell;

        public int2 ResolveLocalCell(float cellSize)
        {
            if (!SnapLocalCellFromTransform || cellSize <= 0f)
                return ManualLocalCell;

            Vector3 local = transform.localPosition;
            return new int2(
                Mathf.RoundToInt(local.x / cellSize),
                Mathf.RoundToInt(local.z / cellSize));
        }
    }
}

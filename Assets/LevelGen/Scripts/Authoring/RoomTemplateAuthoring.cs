using UnityEngine;

namespace THPerfection.LevelGen.Authoring
{
    /// <summary>
    /// Root authoring component on a room prefab. Cell and door markers are gathered from children.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomTemplateAuthoring : MonoBehaviour
    {
        [Header("Identity")]
        public string TemplateId;

        public FloorMask AllowedFloors = FloorMask.Main;

        [Min(0f)]
        public float BaseWeight = 1f;

        [Tooltip("World units per grid cell (beta default 150×150 room footprint).")]
        public float CellSize = BuildingGenConfig.DefaultCellSize;

        [Header("Evaluation")]
        [Tooltip("Placement scorer for this room. Subclass DefaultRoomEvaluator for custom weights. Catalog Evaluator Override wins if set.")]
        public RoomTemplateBase Evaluator;

        [Header("Markers")]
        [Tooltip("Optional root for cell/door markers. Defaults to this transform.")]
        public Transform MarkersRoot;

        public Transform ResolvedMarkersRoot => MarkersRoot != null ? MarkersRoot : transform;

        public RoomCellMarker[] GetCellMarkers() =>
            ResolvedMarkersRoot.GetComponentsInChildren<RoomCellMarker>(true);

        public DoorSocketMarker[] GetDoorMarkers() =>
            ResolvedMarkersRoot.GetComponentsInChildren<DoorSocketMarker>(true);

        public bool TryBake(out RoomTemplateDefinition definition, out string error) =>
            RoomTemplateBaker.TryBakeFromAuthoring(this, out definition, out error);
    }
}

using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen.Authoring
{
    /// <summary>
    /// Marks a door on the room perimeter. Open/closed visuals toggle at spawn from CellDoorState.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorSocketMarker : MonoBehaviour
    {
        public int2 LocalCell;
        public DoorSide Side = DoorSide.North;
        public bool IsMainDoor;

        [Header("Runtime door visuals")]
        [Tooltip("Must stay active on the prefab so ECS baking includes the mesh. Runtime hides via DisableRendering.")]
        public GameObject OpenVisual;
        [Tooltip("Must stay active on the prefab so ECS baking includes the mesh. Runtime hides via DisableRendering.")]
        public GameObject ClosedVisual;

        public void ApplyFromInstance(RoomInstance instance, RoomDoorVisualPhase phase = RoomDoorVisualPhase.Gameplay)
        {
            int2 worldCell = instance.Origin
                + GridTransforms.RotateCell(LocalCell, instance.Rotation);
            DoorSide worldSide = GridTransforms.RotateSide(Side, instance.Rotation);
            var key = new DoorEdgeKey(instance.Floor, worldCell, worldSide);

            if (!instance.TryGetDoorState(key, out CellDoorState state))
            {
                SetVisuals(showOpen: false);
                return;
            }

            SetVisuals(RoomDoorVisualRules.ShouldShowOpen(state, phase));
        }

        public void SetVisuals(bool showOpen)
        {
            if (OpenVisual != null)
                OpenVisual.SetActive(showOpen);

            if (ClosedVisual != null)
                ClosedVisual.SetActive(!showOpen);
        }
    }
}

using System;
using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// Optional prefab-side door visuals. Maps template-local sockets to open/closed meshes.
    /// </summary>
    public sealed class LevelGenRoomVisualAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct DoorSocketVisual
        {
            public int2 LocalCell;
            public DoorSide Side;
            public GameObject OpenVisual;
            public GameObject ClosedVisual;
        }

        [SerializeField] DoorSocketVisual[] _doorVisuals = Array.Empty<DoorSocketVisual>();

        public void ApplyFromInstance(RoomInstance instance)
        {
            foreach (DoorSocketVisual entry in _doorVisuals)
            {
                int2 worldCell = instance.Origin
                    + GridTransforms.RotateCell(entry.LocalCell, instance.Rotation);
                DoorSide worldSide = GridTransforms.RotateSide(entry.Side, instance.Rotation);
                var key = new DoorEdgeKey(instance.Floor, worldCell, worldSide);

                if (!instance.TryGetDoorState(key, out CellDoorState state))
                {
                    SetDoorVisual(entry, showOpen: false);
                    continue;
                }

                bool showOpen = state is CellDoorState.Open or CellDoorState.Connected;
                SetDoorVisual(entry, showOpen);
            }
        }

        static void SetDoorVisual(in DoorSocketVisual entry, bool showOpen)
        {
            if (entry.OpenVisual != null)
                entry.OpenVisual.SetActive(showOpen);

            if (entry.ClosedVisual != null)
                entry.ClosedVisual.SetActive(!showOpen);
        }
    }
}

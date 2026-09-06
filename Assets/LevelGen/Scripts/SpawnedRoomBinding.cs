using THPerfection.LevelGen.Authoring;
using UnityEngine;

namespace THPerfection.LevelGen
{
    /// <summary>
    /// Links a spawned room root to its <see cref="RoomInstance"/> for door visual refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnedRoomBinding : MonoBehaviour
    {
        public int RoomInstanceId;

        public void ApplyDoorVisuals(RoomInstance instance, RoomDoorVisualPhase phase)
        {
            foreach (DoorSocketMarker marker in GetComponentsInChildren<DoorSocketMarker>(true))
                marker.ApplyFromInstance(instance, phase);

            LevelGenRoomVisualAuthoring visualAuthoring = GetComponentInChildren<LevelGenRoomVisualAuthoring>();
            if (visualAuthoring != null)
                visualAuthoring.ApplyFromInstance(instance, phase);

            foreach (ProceduralDoorVisual door in GetComponentsInChildren<ProceduralDoorVisual>(true))
                door.ApplyFromInstance(instance, phase);
        }
    }
}

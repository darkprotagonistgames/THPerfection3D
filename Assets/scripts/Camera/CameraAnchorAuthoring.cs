using THPerfection.LevelGen;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Place on a child GameObject inside a room prefab to bake a room-scoped camera anchor.
/// Keep this GameObject active so it joins <c>LinkedEntityGroup</c>.
/// Position follows the room; rotation is treated as world-north at bake and stays world-aligned
/// when the room instance is rotated. Duplicate this object to add extra room cameras.
/// </summary>
[AddComponentMenu("TH Perfection/Camera/Camera Anchor")]
[DisallowMultipleComponent]
public class CameraAnchorAuthoring : MonoBehaviour
{
    [Tooltip("Gizmo frustum length for placement preview (look distance to ground).")]
    public float GizmoFrustumLength = 40f;

    class Baker : Baker<CameraAnchorAuthoring>
    {
        public override void Bake(CameraAnchorAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Author with the prefab at R0: local rotation is the desired world rotation.
            Quaternion localRot = authoring.transform.localRotation;
            AddComponent<CameraAnchor>(entity);
            AddComponent(entity, new RoomCameraAnchor
            {
                RoomInstanceId = 0,
                PreferredWorldRotation = new quaternion(
                    localRot.x, localRot.y, localRot.z, localRot.w),
            });
            SetComponentEnabled<CameraAnchor>(entity, false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform t = transform;
        Vector3 origin = t.position;
        Quaternion rotation = t.rotation;
        float length = Mathf.Max(1f, GizmoFrustumLength);
        // Center-square gameplay viewport: usable FOV is vertical FOV on both axes.
        float half = length * Mathf.Tan(
            0.5f * RoomCameraAnchorPose.VerticalFieldOfViewDegrees * Mathf.Deg2Rad);

        Vector3 forward = rotation * Vector3.forward * length;
        Vector3 right = rotation * Vector3.right * half;
        Vector3 up = rotation * Vector3.up * half;
        Vector3 tip = origin + forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, tip + right + up);
        Gizmos.DrawLine(origin, tip + right - up);
        Gizmos.DrawLine(origin, tip - right + up);
        Gizmos.DrawLine(origin, tip - right - up);
        Gizmos.DrawLine(tip + right + up, tip + right - up);
        Gizmos.DrawLine(tip + right - up, tip - right - up);
        Gizmos.DrawLine(tip - right - up, tip - right + up);
        Gizmos.DrawLine(tip - right + up, tip + right + up);
        Gizmos.DrawWireSphere(origin, 2f);

        Vector3 lookDir = rotation * Vector3.forward;
        if (Mathf.Abs(lookDir.y) > 0.01f)
        {
            float tHit = -origin.y / lookDir.y;
            if (tHit > 0f)
                Gizmos.DrawWireSphere(origin + lookDir * tHit, 4f);
        }
    }
}

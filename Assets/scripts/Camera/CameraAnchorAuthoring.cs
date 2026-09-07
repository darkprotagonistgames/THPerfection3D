using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Place on a child GameObject inside a room prefab to bake a room-scoped camera anchor.
/// Keep this GameObject active so it joins <c>LinkedEntityGroup</c>.
/// Position follows the room; rotation is treated as world-north at bake and stays world-aligned
/// when the room instance is rotated.
/// </summary>
[DisallowMultipleComponent]
public class CameraAnchorAuthoring : MonoBehaviour
{
    [Tooltip("Gizmo frustum length for placement preview.")]
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
        // Preview as world-north intent (ignore parent room yaw in the editor when possible).
        Quaternion rotation = t.rotation;
        float length = Mathf.Max(1f, GizmoFrustumLength);
        float halfH = length * 0.35f;
        float halfV = length * 0.25f;

        Vector3 forward = rotation * Vector3.forward * length;
        Vector3 right = rotation * Vector3.right * halfH;
        Vector3 up = rotation * Vector3.up * halfV;
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
    }
}

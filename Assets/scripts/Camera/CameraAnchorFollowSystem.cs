using Rukhanka;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Smoothly moves the Unity Camera toward the nearest <b>enabled</b> room camera anchor
/// using world space (<see cref="LocalToWorld"/>), so child anchors inherit the room root pose.
/// Runs after transforms so hierarchy world matrices are current.
/// </summary>
[UpdateAfter(typeof(RukhankaAnimationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
public partial class CameraAnchorFollowSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<CameraAnchor>();
        RequireForUpdate<PlayerMovementData>();
    }

    protected override void OnUpdate()
    {
        Camera unityCamera = MainGameObjectCamera.Resolve();
        if (unityCamera == null)
            return;

        Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerMovementData>();
        float2 playerXZ = TopDownPlane.FromPosition(
            SystemAPI.GetComponent<LocalToWorld>(playerEntity).Position);

        float3 bestPosition = default;
        quaternion bestRotation = quaternion.identity;
        float bestDistanceSq = float.MaxValue;
        var foundAnchor = false;

        // LocalToWorld: room anchors are children; LocalTransform alone is room-local (as if at 0,0).
        foreach (var anchorLtw in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<CameraAnchor>())
        {
            float3 worldPos = anchorLtw.ValueRO.Position;
            float2 anchorXZ = TopDownPlane.FromPosition(worldPos);
            float distanceSq = math.lengthsq(anchorXZ - playerXZ);
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestPosition = worldPos;
            bestRotation = anchorLtw.ValueRO.Rotation;
            foundAnchor = true;
        }

        if (!foundAnchor)
            return;

        float deltaTime = SystemAPI.Time.DeltaTime;
        float positionSmoothTime = MainGameObjectCamera.Instance != null
            ? MainGameObjectCamera.Instance.PositionSmoothTime
            : 0.2f;
        float rotationSmoothTime = MainGameObjectCamera.Instance != null
            ? MainGameObjectCamera.Instance.RotationSmoothTime
            : 0.2f;

        Transform cameraTransform = unityCamera.transform;
        float3 currentPosition = cameraTransform.position;
        quaternion currentRotation = cameraTransform.rotation;

        float positionT = 1f - math.exp(-deltaTime / math.max(0.01f, positionSmoothTime));
        float rotationT = 1f - math.exp(-deltaTime / math.max(0.01f, rotationSmoothTime));

        float3 smoothedPosition = math.lerp(currentPosition, bestPosition, positionT);
        quaternion smoothedRotation = math.slerp(currentRotation, bestRotation, rotationT);

        cameraTransform.SetPositionAndRotation(smoothedPosition, smoothedRotation);

        if (SystemAPI.TryGetSingletonEntity<MainEntityCamera>(out Entity cameraEntity))
        {
            SystemAPI.SetComponent(cameraEntity, LocalTransform.FromPositionRotation(
                smoothedPosition,
                smoothedRotation));
        }
    }
}

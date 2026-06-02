using Rukhanka;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Smoothly moves the Unity Camera toward the nearest anchor after animation has updated entity transforms.
/// Must not read <see cref="LocalTransform"/> from MonoBehaviour LateUpdate while Rukhanka jobs are running.
/// </summary>
[UpdateAfter(typeof(RukhankaAnimationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
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

        float2 playerXZ = TopDownPlane.FromPosition(
            SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerMovementData>()).Position);

        LocalTransform bestTransform = default;
        float bestDistanceSq = float.MaxValue;
        var foundAnchor = false;

        foreach (var anchorTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<CameraAnchor>())
        {
            float2 anchorXZ = TopDownPlane.FromPosition(anchorTransform.ValueRO.Position);
            float distanceSq = math.lengthsq(anchorXZ - playerXZ);
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestTransform = anchorTransform.ValueRO;
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

        float3 smoothedPosition = math.lerp(currentPosition, bestTransform.Position, positionT);
        quaternion smoothedRotation = math.slerp(currentRotation, bestTransform.Rotation, rotationT);

        cameraTransform.SetPositionAndRotation(smoothedPosition, smoothedRotation);

        if (SystemAPI.TryGetSingletonEntity<MainEntityCamera>(out Entity cameraEntity))
        {
            SystemAPI.SetComponent(cameraEntity, LocalTransform.FromPositionRotation(
                smoothedPosition,
                smoothedRotation));
        }
    }
}

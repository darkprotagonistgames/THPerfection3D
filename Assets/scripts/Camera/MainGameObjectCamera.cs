using UnityEngine;

/// <summary>
/// Registers the main scene Unity Camera for <see cref="CameraAnchorFollowSystem"/>.
/// </summary>
[DisallowMultipleComponent]
public class MainGameObjectCamera : MonoBehaviour
{
    public static MainGameObjectCamera Instance { get; private set; }

    [Tooltip("Seconds to ease toward the target anchor position. Lower = faster.")]
    [Min(0.01f)]
    public float PositionSmoothTime = 0.2f;

    [Tooltip("Seconds to ease toward the target anchor rotation. Lower = faster.")]
    [Min(0.01f)]
    public float RotationSmoothTime = 0.2f;

    Camera _unityCamera;

    void Awake()
    {
        Instance = this;
        _unityCamera = GetComponent<Camera>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static Camera Resolve()
    {
        if (Instance != null && Instance._unityCamera != null)
            return Instance._unityCamera;

        return Camera.main;
    }
}

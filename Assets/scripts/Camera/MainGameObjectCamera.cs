using UnityEngine;

/// <summary>
/// Registers the main scene Unity Camera for <see cref="CameraAnchorFollowSystem"/>.
/// </summary>
[DisallowMultipleComponent]
public class MainGameObjectCamera : MonoBehaviour
{
    public static Camera Instance { get; private set; }

    void Awake()
    {
        Instance = GetComponent<Camera>();
    }

    void OnDestroy()
    {
        if (Instance == GetComponent<Camera>())
            Instance = null;
    }

    public static Camera Resolve()
    {
        if (Instance != null)
            return Instance;

        return Camera.main;
    }
}

using UnityEngine;

/// <summary>
/// Classic MonoBehaviour singleton that exposes the player's world Transform
/// to non-ECS systems (camera follow, UI, etc.).
/// The GameObject this sits on is kept in sync with the ECS player entity
/// by PlayerTransformSync.
/// </summary>
public class PlayerSingleton : MonoBehaviour
{
    public static PlayerSingleton Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

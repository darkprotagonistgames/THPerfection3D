#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot builder for gameplay prefabs used by the 3D ECS port.
/// </summary>
public static class PortPrefabBuilder
{
    private const string ZombiVisualPath =
        "Assets/Blueprints/Charecters/enimies/lu/Zombi/zombi_visual.prefab";

    [MenuItem("THPerfection/Build Port Prefabs")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/prefab");
        EnsureFolder("Assets/prefab/characters");
        EnsureFolder("Assets/prefab/characters/player");
        EnsureFolder("Assets/prefab/characters/enemies");

        BuildPlayerPrefab();
        BuildZombiPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Port prefabs built: Player and Zombi.");
    }

    private static void BuildPlayerPrefab()
    {
        const string path = "Assets/prefab/characters/player/Player.prefab";
        var root = new GameObject("Player");

        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.height = 2f;
        capsule.radius = 0.4f;
        capsule.center = new Vector3(0f, 1f, 0f);

        root.AddComponent<PlayerMovementAuthoring>();
        root.AddComponent<MecanimLocomotionAuthoring>();

        AddVisual(root);
        AddSpawnProtection(root, 8f);

        SavePrefab(root, path);
        Object.DestroyImmediate(root);
    }

    private static void BuildZombiPrefab()
    {
        const string path = "Assets/prefab/characters/enemies/Zombi.prefab";
        var root = new GameObject("Zombi");

        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.height = 2f;
        capsule.radius = 0.4f;
        capsule.center = new Vector3(0f, 1f, 0f);

        var moveTo = root.AddComponent<MoveToAuthoring>();
        moveTo.FollowPlayer = true;
        moveTo.MaxMoveSpeed = 4f;
        moveTo.Acceleration = 20f;

        root.AddComponent<HeatSignatureAuthoring>();
        root.AddComponent<MecanimLocomotionAuthoring>();

        AddVisual(root);
        AddSpawnProtection(root, 0.15f);
        AddSpawnFootprint(root, 0.3f);

        SavePrefab(root, path);
        Object.DestroyImmediate(root);
    }

    private static void AddVisual(GameObject root)
    {
        var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiVisualPath);
        if (visualPrefab == null)
        {
            Debug.LogError($"Missing visual prefab at {ZombiVisualPath}");
            return;
        }

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
        visual.name = "zombi_visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var mecanim = root.GetComponent<MecanimLocomotionAuthoring>();
        if (mecanim != null)
            mecanim.TargetAnimator = visual.GetComponentInChildren<Animator>();
    }

    private static void AddSpawnProtection(GameObject root, float radius)
    {
        var go = new GameObject("spawnProtection");
        go.transform.SetParent(root.transform, false);
        go.layer = LayerMask.NameToLayer("spawnProtection");

        var sphere = go.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;
    }

    private static void AddSpawnFootprint(GameObject root, float radius)
    {
        var go = new GameObject("SpawnFootprint");
        go.transform.SetParent(root.transform, false);
        go.layer = LayerMask.NameToLayer("spawnFootprint");

        var sphere = go.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            PrefabUtility.SaveAsPrefabAsset(root, path);
        else
            PrefabUtility.SaveAsPrefabAsset(root, path);
    }

    internal static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var leaf = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif

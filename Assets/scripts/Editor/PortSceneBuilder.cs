#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Unity.Scenes;

/// <summary>
/// Builds SampleScene + sampleSub for the 3D ECS port.
/// </summary>
public static class PortSceneBuilder
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string SubScenePath = "Assets/Scenes/SampleScene/sampleSub.unity";
    private const string PlayerPrefabPath = "Assets/prefab/characters/player/Player.prefab";
    private const string ZombiPrefabPath = "Assets/prefab/characters/enemies/Zombi.prefab";

    [MenuItem("THPerfection/Build Port Scenes")]
    public static void BuildAll()
    {
        PortPrefabBuilder.BuildAll();
        BuildSubScene();
        BuildMainScene();
        AssetDatabase.SaveAssets();
        Debug.Log("Port scenes built.");
    }

    private static void BuildSubScene()
    {
        PortPrefabBuilder.EnsureFolder("Assets/Scenes/SampleScene");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var playerSpawner = new GameObject("PlayerSpawner");
        playerSpawner.transform.position = Vector3.zero;
        var prefabSpawner = playerSpawner.AddComponent<PrefabSpawnerAuthoring>();
        prefabSpawner.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

        var zombiSpawner = new GameObject("ZombiSpawner");
        zombiSpawner.transform.position = Vector3.zero;
        var safeSpawner = zombiSpawner.AddComponent<SafeRandomSpawner>();
        safeSpawner.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiPrefabPath);
        safeSpawner.BoundsMin = new Vector2(-50f, -50f);
        safeSpawner.BoundsMax = new Vector2(50f, 50f);
        safeSpawner.SpawnCount = 10;

        var zombiSpawnerNear = new GameObject("NearZombiSpawner");
        zombiSpawnerNear.transform.position = Vector3.zero;
        var nearSpawner = zombiSpawnerNear.AddComponent<SafeRandomSpawner>();
        nearSpawner.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiPrefabPath);
        nearSpawner.BoundsMin = new Vector2(-10f, -10f);
        nearSpawner.BoundsMax = new Vector2(10f, 10f);
        nearSpawner.SpawnCount = 5;

        var radar = new GameObject("RadarConfig");
        radar.AddComponent<RadarConfigAuthoring>();

        EditorSceneManager.SaveScene(scene, SubScenePath);
    }

    private static void BuildMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(0f, 20f, -10f);
            camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            camera.orthographic = false;
        }

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10f, 1f, 10f);

        var proxy = new GameObject("PlayerProxy");
        proxy.AddComponent<PlayerSingleton>();
        proxy.AddComponent<PlayerTransformSync>();

        BuildRadarCanvas();

        var subSceneGo = new GameObject("sampleSub");
        var subScene = subSceneGo.AddComponent<SubScene>();
        subScene.AutoLoadScene = true;
        subScene.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SubScenePath);

        EditorSceneManager.SaveScene(scene, SampleScenePath);
    }

    private static void BuildRadarCanvas()
    {
        var canvasGo = new GameObject("RadarCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var displayGo = new GameObject("RadarDisplay");
        displayGo.transform.SetParent(canvasGo.transform, false);
        var rect = displayGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(150f, 150f);
        rect.anchoredPosition = new Vector2(-20f, -20f);

        displayGo.AddComponent<RawImage>();
        displayGo.AddComponent<RadarUIBridge>();
    }
}
#endif

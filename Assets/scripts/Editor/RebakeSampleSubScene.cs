using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Scenes;

/// <summary>
/// Saves and rebakes the sample ECS subscene so room prefab + door socket data is current.
/// </summary>
static class RebakeSampleSubScene
{
    const string SubScenePath = "Assets/Scenes/SampleScene/sampleSub.unity";
    const string MainScenePath = "Assets/SampleScene.unity";

    [MenuItem("THPerfection/Level Gen/Rebake sampleSub")]
    static void Rebake()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SubScenePath);
        if (sceneAsset == null)
        {
            Debug.LogError($"[RebakeSampleSubScene] Missing subscene at {SubScenePath}");
            return;
        }

        if (EditorSceneManager.GetActiveScene().path != MainScenePath)
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        var opened = EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(opened);

        foreach (GameObject root in opened.GetRootGameObjects())
        {
            SubScene subScene = root.GetComponent<SubScene>();
            if (subScene != null)
                continue;

            if (root.GetComponent<RoomCatalogEcsAuthoring>() != null)
                Debug.Log("[RebakeSampleSubScene] Found RoomCatalogEcsAuthoring — saving will refresh room prefab entity bakes.");
        }

        EditorSceneManager.SaveScene(opened);
        EditorSceneManager.CloseScene(opened, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[RebakeSampleSubScene] sampleSub saved. "
            + "If door visuals still fail, double-click the sampleSub object in the hierarchy to open the subscene, wait for bake, then save.");
    }
}

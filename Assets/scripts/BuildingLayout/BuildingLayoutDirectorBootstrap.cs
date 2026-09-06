using THPerfection.LevelGen;
using UnityEngine;

/// <summary>
/// Ensures every <see cref="BuildingRunDirector"/> in the scene has a layout ECS bridge.
/// </summary>
static class BuildingLayoutDirectorBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AttachMissingBridges()
    {
        var directors = Object.FindObjectsByType<BuildingRunDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < directors.Length; i++)
        {
            BuildingRunDirector director = directors[i];
            if (director.GetComponent<BuildingLayoutEcsBridge>() == null)
                director.gameObject.AddComponent<BuildingLayoutEcsBridge>();
        }
    }
}

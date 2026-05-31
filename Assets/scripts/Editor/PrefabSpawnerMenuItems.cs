using UnityEditor;
using UnityEngine;

public static class PrefabSpawnerMenuItems
{
    private const string PrefabSpawnerMenuPath = "GameObject/ECS/Prefab Spawner";
    private const int MenuPriority = 10;

    [MenuItem(PrefabSpawnerMenuPath, false, MenuPriority)]
    private static void CreatePrefabSpawner(MenuCommand command)
    {
        GameObject go = new GameObject("Prefab Spawner");
        go.AddComponent<PrefabSpawnerAuthoring>();

        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);

        Undo.RegisterCreatedObjectUndo(go, "Create Prefab Spawner");
        Selection.activeGameObject = go;
    }

    [MenuItem(PrefabSpawnerMenuPath, true)]
    private static bool ValidateCreatePrefabSpawner() => true;
}

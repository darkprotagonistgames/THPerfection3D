using System.Collections;
using THPerfection.LevelGen;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Starts a building run when play mode begins. Lives on the main-scene orchestrator —
/// not inside an ECS subscene (subscene authoring objects do not run MonoBehaviour logic).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BuildingRunDirector))]
[RequireComponent(typeof(BuildingLayoutEcsBridge))]
public sealed class BuildingRunAutoStart : MonoBehaviour
{
    public bool AutoStartOnPlay = true;

    [Tooltip("Max frames to wait for the ECS world and subscene catalog registry.")]
    public int MaxWaitFrames = 600;

    BuildingRunDirector _director;

    void Awake()
    {
        _director = GetComponent<BuildingRunDirector>();
    }

    void Start()
    {
        if (AutoStartOnPlay)
            StartCoroutine(BeginRunWhenReady());
    }

    IEnumerator BeginRunWhenReady()
    {
        int frames = 0;
        while (frames < MaxWaitFrames && !IsWorldReady())
        {
            frames++;
            yield return null;
        }

        if (!IsWorldReady())
        {
            Debug.LogError(
                "[BuildingRunAutoStart] ECS world not ready — aborting auto start. "
                + "Ensure sampleSub is present and Auto Load Scene is enabled.");
            yield break;
        }

        frames = 0;
        while (frames < MaxWaitFrames && !IsCatalogRegistryReady())
        {
            frames++;
            yield return null;
        }

        if (!IsCatalogRegistryReady())
        {
            Debug.LogWarning(
                "[BuildingRunAutoStart] RoomVisualPrefabRegistry not found — rebake sampleSub "
                + "with RoomCatalogEcsAuthoring. Starting run anyway (rooms may be missing visuals).");
        }

        _director.StartRun();
    }

    static bool IsWorldReady()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated;
    }

    static bool IsCatalogRegistryReady()
    {
        if (!IsWorldReady())
            return false;

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RoomVisualPrefabRegistryTag>(),
            ComponentType.ReadOnly<RoomVisualPrefabEntry>());

        return !query.IsEmpty;
    }
}

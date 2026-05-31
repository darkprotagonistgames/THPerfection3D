using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that owns the SpawnRequest singleton buffer entity.
/// Processes all queued requests each Update frame using safe placement logic,
/// and caches per-prefab footprints to avoid repeated hierarchy traversal.
/// ECS systems push SpawnRequest elements into the buffer; this bridge instantiates
/// the corresponding managed prefabs using Physics.CheckSphere overlap checks.
/// </summary>
public class EcsSpawnBridge : MonoBehaviour
{
    [Tooltip("Prefabs ECS systems can request by index via SpawnRequest.PrefabIndex.")]
    public List<GameObject> RegisteredPrefabs;

    [Min(1)]
    public int MaxAttempts = 30;

    public static EcsSpawnBridge Instance { get; private set; }

    private EntityManager _entityManager;
    private Entity        _singletonEntity;
    private Dictionary<int, SpawnPlacementUtils.FootprintSphere[]> _footprintCache;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _entityManager   = World.DefaultGameObjectInjectionWorld.EntityManager;
        _singletonEntity = _entityManager.CreateSingletonBuffer<SpawnRequest>();
        _footprintCache  = new Dictionary<int, SpawnPlacementUtils.FootprintSphere[]>();
    }

    private void Update()
    {
        DynamicBuffer<SpawnRequest> buffer = _entityManager.GetBuffer<SpawnRequest>(_singletonEntity);

        if (buffer.Length == 0)
            return;

        for (int i = 0; i < buffer.Length; i++)
        {
            SpawnRequest req = buffer[i];

            if (req.PrefabIndex < 0 || req.PrefabIndex >= RegisteredPrefabs.Count)
            {
                Debug.LogError($"[EcsSpawnBridge] PrefabIndex {req.PrefabIndex} is out of range (count: {RegisteredPrefabs.Count}). Skipping request.");
                continue;
            }

            SpawnPlacementUtils.FootprintSphere[] footprints = GetOrBuildFootprints(req.PrefabIndex);

            if (SpawnPlacementUtils.TryFindPositionAround(footprints, (Vector3)req.OriginPosition, req.SearchRadius, MaxAttempts, out Vector3 pos))
            {
                GameObject instance = Instantiate(RegisteredPrefabs[req.PrefabIndex], pos, Quaternion.identity);
                SpawnPlacementUtils.RemoveSpawnFootprints(instance);
            }
            else
            {
                Debug.LogError($"[EcsSpawnBridge] Failed to find a safe position for prefab index {req.PrefabIndex} after {MaxAttempts} attempts.");
                // TODO: Required spawns must never be silently dropped — add escalation or fallback here.
            }
        }

        buffer.Clear();
    }

    /// <summary>
    /// Pushes a SpawnRequest into the singleton buffer from managed code.
    /// </summary>
    public void RequestSpawn(int prefabIndex, Vector3 origin, float searchRadius)
    {
        _entityManager.GetBuffer<SpawnRequest>(_singletonEntity).Add(new SpawnRequest
        {
            OriginPosition = origin,
            SearchRadius   = searchRadius,
            PrefabIndex    = prefabIndex,
        });
    }

    private SpawnPlacementUtils.FootprintSphere[] GetOrBuildFootprints(int prefabIndex)
    {
        if (!_footprintCache.TryGetValue(prefabIndex, out var footprints))
        {
            footprints = SpawnPlacementUtils.CollectFootprints(RegisteredPrefabs[prefabIndex]);
            _footprintCache[prefabIndex] = footprints;
        }
        return footprints;
    }
}

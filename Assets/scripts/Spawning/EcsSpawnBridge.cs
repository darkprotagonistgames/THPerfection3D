using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that owns the SpawnRequest singleton buffer entity.
/// Processes all queued requests each Update frame using safe placement logic,
/// and caches per-prefab protection colliders to avoid repeated hierarchy traversal.
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
    private Dictionary<int, SpawnPlacementUtils.ProtectionCollection> _protectionCache;

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
        _protectionCache = new Dictionary<int, SpawnPlacementUtils.ProtectionCollection>();
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

            SpawnPlacementUtils.ProtectionCollection protection = GetOrBuildProtection(req.PrefabIndex);

            if (SpawnPlacementUtils.TryFindPositionAround(protection, (Vector3)req.OriginPosition, req.SearchRadius, MaxAttempts, out Vector3 pos))
            {
                GameObject instance = Instantiate(RegisteredPrefabs[req.PrefabIndex], pos, Quaternion.identity);
                SpawnPlacementUtils.RemoveSpawnProtection(instance);
            }
            else
            {
                Debug.LogError($"[EcsSpawnBridge] Failed to find a safe position for prefab index {req.PrefabIndex} after {MaxAttempts} attempts.");
            }
        }

        buffer.Clear();
    }

    public void RequestSpawn(int prefabIndex, Vector3 origin, float searchRadius)
    {
        _entityManager.GetBuffer<SpawnRequest>(_singletonEntity).Add(new SpawnRequest
        {
            OriginPosition = origin,
            SearchRadius   = searchRadius,
            PrefabIndex    = prefabIndex,
        });
    }

    private SpawnPlacementUtils.ProtectionCollection GetOrBuildProtection(int prefabIndex)
    {
        if (!_protectionCache.TryGetValue(prefabIndex, out var protection))
        {
            protection = SpawnPlacementUtils.CollectProtection(RegisteredPrefabs[prefabIndex]);
            _protectionCache[prefabIndex] = protection;
        }

        return protection;
    }
}

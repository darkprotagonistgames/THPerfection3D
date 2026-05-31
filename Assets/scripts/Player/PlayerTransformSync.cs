using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Queries the ECS world for the player entity (identified by PlayerMovementData)
/// and mirrors its LocalTransform position onto this GameObject's Transform every frame.
/// Attach this to the same GameObject as PlayerSingleton.
/// </summary>
public class PlayerTransformSync : MonoBehaviour
{
    private EntityManager _entityManager;
    private EntityQuery _playerQuery;
    private bool _queryReady;

    private void Start()
    {
        World defaultWorld = World.DefaultGameObjectInjectionWorld;
        if (defaultWorld == null)
        {
            Debug.LogError("[PlayerTransformSync] No default ECS world found.");
            return;
        }

        _entityManager = defaultWorld.EntityManager;
        _playerQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerMovementData>(),
            ComponentType.ReadOnly<LocalTransform>()
        );

        _queryReady = true;
    }

    private void Update()
    {
        if (!_queryReady || _playerQuery.IsEmpty)
            return;

        // Read the first (and only) player entity's LocalTransform.
        LocalTransform playerTransform = _playerQuery.GetSingleton<LocalTransform>();
        float3 position = playerTransform.Position;
        quaternion rotation = playerTransform.Rotation;
        transform.SetPositionAndRotation(
            new Vector3(position.x, position.y, position.z),
            new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w));
    }

    private void OnDestroy()
    {
        if (_queryReady)
            _playerQuery.Dispose();
    }
}

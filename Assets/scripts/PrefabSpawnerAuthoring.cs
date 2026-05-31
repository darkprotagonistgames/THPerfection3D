using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Place this on an empty GameObject in your subscene to spawn any entity prefab at a specific position.
/// Assign any saved prefab and position the GameObject to set the spawn point.
/// </summary>
public class PrefabSpawnerAuthoring : MonoBehaviour
{
    [Tooltip("The entity prefab to spawn.")]
    public GameObject Prefab;

    public class Baker : Baker<PrefabSpawnerAuthoring>
    {
        public override void Bake(PrefabSpawnerAuthoring authoring)
        {
            Entity spawnerEntity = GetEntity(TransformUsageFlags.None);

            AddComponent(spawnerEntity, new PrefabSpawnerData
            {
                Prefab        = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                SpawnPosition = authoring.transform.position,
            });
        }
    }
}

public struct PrefabSpawnerData : IComponentData
{
    public Entity Prefab;
    public float3 SpawnPosition;
}

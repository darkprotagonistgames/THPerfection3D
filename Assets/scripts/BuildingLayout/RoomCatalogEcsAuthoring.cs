using THPerfection.LevelGen;
using THPerfection.LevelGen.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Bakes entity prefab references from a room catalog for runtime room instantiation.
/// Place inside the ECS subscene (bake-only; runtime orchestration stays in the main scene).
/// </summary>
[DisallowMultipleComponent]
public sealed class RoomCatalogEcsAuthoring : MonoBehaviour
{
    public RoomCatalogAsset CatalogAsset;

    public class Baker : Baker<RoomCatalogEcsAuthoring>
    {
        public override void Bake(RoomCatalogEcsAuthoring authoring)
        {
            RoomCatalogAsset catalogAsset = authoring.CatalogAsset;
            if (catalogAsset == null)
                return;

            Entity registryEntity = GetEntity(TransformUsageFlags.None);
            DynamicBuffer<RoomVisualPrefabEntry> entries = AddBuffer<RoomVisualPrefabEntry>(registryEntity);
            AddComponent<RoomVisualPrefabRegistryTag>(registryEntity);

            for (int i = 0; i < catalogAsset.Entries.Count; i++)
            {
                RoomCatalogSourceEntry source = catalogAsset.Entries[i];
                if (source.RoomPrefab == null)
                    continue;

                var templateAuthoring = source.RoomPrefab.GetComponent<RoomTemplateAuthoring>();
                if (templateAuthoring == null || string.IsNullOrWhiteSpace(templateAuthoring.TemplateId))
                    continue;

                DependsOn(source.RoomPrefab);
                Entity prefabEntity = GetEntity(source.RoomPrefab, TransformUsageFlags.Dynamic);

                entries.Add(new RoomVisualPrefabEntry
                {
                    TemplateId = templateAuthoring.TemplateId,
                    Prefab       = prefabEntity,
                });
            }
        }
    }
}

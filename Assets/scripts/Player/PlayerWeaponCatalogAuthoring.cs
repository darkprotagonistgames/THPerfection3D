using System;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Bakes weapon-system prefab entity references for <see cref="PlayerInventoryManager"/>.
/// Place on a GameObject inside your ECS subscene and assign prefabs per attack slot.
/// </summary>
[DisallowMultipleComponent]
public class PlayerWeaponCatalogAuthoring : MonoBehaviour
{
    public const int SlotCount = 4;

    [Serializable]
    public struct WeaponSlot
    {
        [Tooltip("ECS weapon-system prefab for this slot (e.g. ConeAttack, TargetedAttack).")]
        public GameObject WeaponSystemPrefab;
    }

    [Tooltip("Slot 0 = Attack1, slot 1 = Attack2, slot 2 = Attack3, slot 3 = Attack4.")]
    public WeaponSlot[] Slots = new WeaponSlot[SlotCount];

    public class Baker : Baker<PlayerWeaponCatalogAuthoring>
    {
        public override void Bake(PlayerWeaponCatalogAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<PlayerWeaponCatalogTag>(entity);

            DynamicBuffer<PlayerWeaponPrefabEntry> buffer = AddBuffer<PlayerWeaponPrefabEntry>(entity);
            WeaponSlot[] slots = authoring.Slots ?? Array.Empty<WeaponSlot>();

            for (int i = 0; i < slots.Length; i++)
            {
                GameObject prefab = slots[i].WeaponSystemPrefab;
                if (prefab == null)
                    continue;

                DependsOn(prefab);
                buffer.Add(new PlayerWeaponPrefabEntry
                {
                    SlotIndex = i,
                    PrefabEntity = GetEntity(prefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}

/// <summary>Baked singleton marker for <see cref="PlayerWeaponCatalogAuthoring"/>.</summary>
public struct PlayerWeaponCatalogTag : IComponentData { }

/// <summary>Baked weapon-system prefab reference for one inventory slot.</summary>
public struct PlayerWeaponPrefabEntry : IBufferElementData
{
    public int SlotIndex;
    public Entity PrefabEntity;
}

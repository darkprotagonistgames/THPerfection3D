using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main-scene input bridge: reads Attack1–Attack4 and spawns ECS weapon systems from the
/// baked <see cref="PlayerWeaponCatalogAuthoring"/> catalog in your subscene.
/// </summary>
[DisallowMultipleComponent]
public class PlayerInventoryManager : MonoBehaviour
{
    const int SlotCount = 4;

    static readonly string[] AttackActionNames =
    {
        "Attack1",
        "Attack2",
        "Attack3",
        "Attack4",
    };

    InputSystem_Actions _actions;
    InputAction[] _attackActions = new InputAction[SlotCount];
    EntityManager _entityManager;
    EntityQuery _playerQuery;
    EntityQuery _catalogQuery;
    EntityQuery _activeWeaponQuery;
    bool _ready;
    bool _catalogMissingLogged;
    int _activeSlotIndex = -1;

    const float CatalogWaitTimeoutSeconds = 10f;
    float _catalogWaitStartTime;

    void OnEnable()
    {
        _actions ??= new InputSystem_Actions();
        _actions.Player.Enable();

        InputActionMap playerMap = _actions.Player.Get();
        for (int i = 0; i < AttackActionNames.Length; i++)
            _attackActions[i] = playerMap.FindAction(AttackActionNames[i], throwIfNotFound: true);
    }

    void OnDisable()
    {
        if (_actions == null)
            return;

        _actions.Player.Disable();
        _actions.Dispose();
        _actions = null;
    }

    void Start()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("[PlayerInventoryManager] No default ECS world found.");
            return;
        }

        _entityManager = world.EntityManager;
        _playerQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerMovementData>(),
            ComponentType.ReadOnly<LocalTransform>());
        _catalogQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerWeaponCatalogTag>(),
            ComponentType.ReadOnly<PlayerWeaponPrefabEntry>());
        _activeWeaponQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<ActivePlayerWeaponTag>());
        _catalogWaitStartTime = Time.realtimeSinceStartup;
    }

    void Update()
    {
        if (!_ready)
        {
            if (_catalogQuery.IsEmpty)
            {
                if (!_catalogMissingLogged
                    && Time.realtimeSinceStartup - _catalogWaitStartTime >= CatalogWaitTimeoutSeconds)
                {
                    _catalogMissingLogged = true;
                    Debug.LogError(
                        "[PlayerInventoryManager] No baked weapon catalog found after subscene load. " +
                        "Add PlayerWeaponCatalogAuthoring to a GameObject in your ECS subscene, assign weapon prefabs, " +
                        "then close the subscene and rebake (Entities > Baking > Bake Scene or save the subscene).");
                }

                return;
            }

            _ready = true;
        }

        if (_actions == null)
            return;

        for (int i = 0; i < _attackActions.Length; i++)
        {
            if (_attackActions[i].WasPerformedThisFrame())
                EquipWeaponSlot(i);
        }
    }

    void EquipWeaponSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
            return;

        if (!TryGetPrefabEntity(slotIndex, out Entity prefabEntity))
            return;

        if (slotIndex == _activeSlotIndex && HasActiveWeapon())
            return;

        DestroyActiveWeapon();

        if (_playerQuery.IsEmpty)
        {
            Debug.LogWarning("[PlayerInventoryManager] No player entity found.");
            return;
        }

        Entity playerEntity = _playerQuery.GetSingletonEntity();
        LocalTransform playerTransform = _entityManager.GetComponentData<LocalTransform>(playerEntity);

        Entity weaponEntity = _entityManager.Instantiate(prefabEntity);
        _entityManager.SetComponentData(weaponEntity, playerTransform);

        if (!_entityManager.HasComponent<SnapToPlayerTag>(weaponEntity))
            _entityManager.AddComponent<SnapToPlayerTag>(weaponEntity);

        _entityManager.AddComponent<ActivePlayerWeaponTag>(weaponEntity);
        _activeSlotIndex = slotIndex;
    }

    bool TryGetPrefabEntity(int slotIndex, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;

        Entity catalogEntity = _catalogQuery.GetSingletonEntity();
        DynamicBuffer<PlayerWeaponPrefabEntry> catalog =
            _entityManager.GetBuffer<PlayerWeaponPrefabEntry>(catalogEntity);

        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i].SlotIndex != slotIndex)
                continue;

            prefabEntity = catalog[i].PrefabEntity;
            if (prefabEntity == Entity.Null)
                Debug.LogWarning($"[PlayerInventoryManager] Slot {slotIndex} has no prefab assigned.");

            return prefabEntity != Entity.Null;
        }

        Debug.LogWarning($"[PlayerInventoryManager] Slot {slotIndex} is not in the baked catalog.");
        return false;
    }

    void DestroyActiveWeapon()
    {
        using var entities = _activeWeaponQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (_entityManager.Exists(entities[i]))
                _entityManager.DestroyEntity(entities[i]);
        }

        _activeSlotIndex = -1;
    }

    bool HasActiveWeapon()
    {
        return !_activeWeaponQuery.IsEmpty;
    }
}

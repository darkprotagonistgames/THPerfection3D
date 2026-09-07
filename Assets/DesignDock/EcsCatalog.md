# Custom ECS catalog

Inventory of **project-owned** Unity Entities types under `Assets/` (not Rukhanka, Unity Physics, or other packages). Generated frame events live in `Assets/EcsEventSystem/Generated/EcsEvents.generated.cs`.

`Assets/LevelGen/` is the GameObject director + pure grid library (not ECS). The committed layout snapshot and room entities live in `Assets/scripts/BuildingLayout/`.

When you add, rename, or remove a custom `IComponentData`, buffer, or system, update this file in the same change.

---

## Components

### Characters and player

| Type | Kind | What it does |
|------|------|----------------|
| `CharacterTag` | tag | Marks a character entity (player, enemy, NPC). |
| `FaceVelocityRotation` | data | Min XZ speed squared; when present, rotation follows velocity. |
| `Health` | data | Current hit points. |
| `PlayerMovementData` | data | Player acceleration and max speed. |
| `PlayerInputData` | data | Normalized XZ move and jump this frame. |
| `SnapToPlayerTag` | tag | Copy the player `LocalTransform` onto this entity each frame. |
| `FollowPlayerTag` | tag | Keep `MoveToTarget` pointed at the player. |
| `MoveToStatsReference` | data | Blob ref to shared accel / max speed for seek movement. |
| `MoveToTarget` | data | XZ seek target and whether seeking is active. |
| `ActivePlayerWeaponTag` | tag | Currently equipped runtime weapon-system entity (one at a time). |
| `PlayerWeaponCatalogTag` | tag | Baked singleton that owns the weapon prefab catalog. |
| `PlayerWeaponPrefabEntry` | buffer | Slot index → weapon-system prefab entity. |

### Combat

| Type | Kind | What it does |
|------|------|----------------|
| `HitboxData` | data | Attack volume damage and weapon type. |
| `HitboxOwner` | data | Character root that owns the hitbox (self-hit checks). |
| `HurtboxData` | data | Receiver category and per-attacker invuln duration. |
| `HurtboxOwner` | data | Character root that owns the hurtbox (damage sender). |
| `HurtboxInvulnerabilityLink` | buffer | Points at TTL invuln record entities for this hurtbox. |
| `HurtboxInvulnerabilityRecord` | data | TTL entity storing the attacker root this window applies to. |
| `SpawnInvulnerabilityTag` | tag | Blocks hits for the first physics frame after spawn. |
| `CombatColliderLocalOffset` | data | Baked local pose so kinematic hit/hurt colliders follow the root. |
| `ConeAttackData` | data | Prefab, range, cone half-angle, cooldown, target layer mask. |
| `TargetedAttackData` | data | Prefab, range, cooldown, target layer mask (closest hurtbox). |
| `AttackSpawnContext` | data | Origin and target entities on a spawned attack instance. |
| `DirectDamageData` | data | One-shot damage + weapon type applied via `AttackSpawnContext`. |
| `KeepAfterDeathTag` | tag | Child to detach in world space instead of destroying with the dead root. |
| `SpawnGroupAnchorTag` | tag | Runtime child that parents spawned attacks and owns their `LinkedEntityGroup`. |

### Camera, world, radar

| Type | Kind | What it does |
|------|------|----------------|
| `CameraAnchor` | tag | Fixed camera pose for a world region. |
| `CameraAnchorGridConfig` | singleton | Grid spawn settings (FOV, aspect, world size, overlap). |
| `MainEntityCamera` | tag | Baked on the camera-rig/config entity; follow system mirrors the smoothed pose onto it when present. |
| `WorldSurfaceBounds` | singleton | Playable XZ plane width/depth from origin. |
| `HeatSignatureData` | data | Radar heat type, amount, and scatter size. |
| `RadarRendererConfig` | singleton | Radar texture size, filter, color, world mapping. |

### Building layout

| Type | Kind | What it does |
|------|------|----------------|
| `BuildingSpatialConfig` | data | Cell size and main-floor Y for world mapping. |
| `BuildingLayoutBlobRef` | data | Blob snapshot of cells/rooms plus layout version. |
| `BuildingLayoutChanged` | enableable | Enabled one frame after a layout commit; then disabled. |
| `BuildingLayoutSingletonTag` | tag | Marks the layout singleton entity. |
| `BuildingRoomTag` | tag | Marks a spawned room instance entity. |
| `BuildingRoomInstance` | data | Room id, floor, origin, rotation, template id. |
| `BuildingRoomDoorState` | buffer | Per-edge door state on a room (cell, side, `CellDoorState`). |
| `BuildingRoomVisualPhaseState` | data | Current door visual phase for the room. |
| `BuildingRoomEntityEntry` | buffer | Room instance id → room entity (on the singleton). |
| `RoomVisualPrefabRegistryTag` | tag | Baked catalog of room visual prefabs. |
| `RoomVisualPrefabEntry` | buffer | Template id → room prefab entity. |
| `RoomDoorSocketBaked` | data | Baked door socket: local cell, side, open/closed visual entities. |
| `RoomDoorOpenVisualEntity` | buffer | Extra open-door visual entities on a socket. |
| `RoomDoorClosedVisualEntity` | buffer | Extra closed-door visual entities on a socket. |

### Spatial occupancy

| Type | Kind | What it does |
|------|------|----------------|
| `TracksSpatialOccupancy` | tag | Opt-in: entity participates in cell/room tracking. |
| `GridCellLocation` | data | Cached `FloorId` + grid `int2` from transform. |
| `RoomLocation` | data | Cached `RoomInstanceId` (`0` = outside stamped cells). |
| `GridCellChanged` | enableable | Enabled one frame when cell changes (previous/current). |
| `RoomChanged` | enableable | Enabled one frame when room changes (previous/current). |

### Spawning and lifetime

| Type | Kind | What it does |
|------|------|----------------|
| `PrefabSpawnerData` | data | Prefab + world position; consumed once at init. |
| `SafeRandomSpawnerData` | data | Prefab + blob config for protected random XZ spawns. |
| `SpawnRequest` | buffer | Request `EcsSpawnBridge` to place a registered prefab near a point. |
| `TtlData` | data | Seconds remaining; host entity is destroyed at zero. |

### Generated frame events (`THPerfection.GeneratedEvents`)

Do not edit the generated file by hand; change the ECS Event System config and regenerate.

| Type | Kind | What it does |
|------|------|----------------|
| `jumpEvent` | frame event | Jump from a sender (`high`). |
| `damageEvent` | frame event | Damage to a victim (amount, weapon, targetable type). |
| `deathEvent` | frame event | Death of `Sender`. |
| `roomChangedEvent` | frame event | Any tracked entity changed rooms (`previousRoomId`, `currentRoomId`). |
| `playerRoomChangedEvent` | frame event | Player (`PlayerMovementData`) changed rooms. |
| `weponbatTag` | tag | Added on damage events with `wepon.bat`. |
| `targetablewallTag` / `targetablezombiTag` / `targetableplayerTag` | tags | Added on damage events from `targetable`. |

`IEcsFrameEvent` is the marker interface for those event structs. Events start disabled; `EnableAllEcsEventsSystem` enables them the same frame, then `CleanupAllEcsEventsSystem` destroys enabled event entities.

---

## Systems

### Input, movement, facing

| System | Group / notes | What it does |
|--------|---------------|----------------|
| `PlayerInputSystem` | `SystemBase` (managed Input Actions) | Writes move/jump into `PlayerInputData`. |
| `PlayerMovementSystem` | simulation | Accelerates the player from input; emits `jumpEvent`. |
| `MoveToSystem` | simulation | Accelerates seekers toward `MoveToTarget` on XZ. |
| `FollowPlayerSystem` | before `MoveToSystem` | Sets follower targets to the player XZ. |
| `FaceVelocityRotationSystem` | after player + move-to | Faces model forward along XZ velocity (`-Z` convention). |
| `SnapToPlayerSystem` | before transforms | Copies player transform onto tagged entities. |

### Combat

| System | Group / notes | What it does |
|--------|---------------|----------------|
| `ConeAttackSystem` | after transforms | Spawns cone-attack prefab when a matching hurtbox is in arc. |
| `TargetedAttackSystem` | after transforms | Spawns attack prefab at closest matching hurtbox in range. |
| `DirectDamageSystem` | after targeted attack, before event enable | Emits `damageEvent` from `AttackSpawnContext`, then destroys self. |
| `CombatColliderFollowOwnerSystem` | fixed-step, first | Re-applies baked local offset on kinematic hit/hurt bodies. |
| `HitboxTriggerSystem` | physics simulation | Overlap hitbox→hurtbox → `damageEvent` + invuln TTL records. |
| `DamageEventLoggerSystem` | after event enable | Debug-logs enabled damage events. |
| `DamageHealthSystem` | after event enable | Subtracts damage from victim `Health`. |
| `DeathFromHealthSystem` | after health | Emits `deathEvent` when victim health ≤ 0. |
| `DeathSystem` | after event enable | Detaches `KeepAfterDeathTag` children, destroys dead hierarchy. |
| `HurtboxInvulnerabilityCleanupSystem` | after TTL | Drops stale invuln buffer links. |
| `SpawnInvulnerabilityCleanupSystem` | simulation, last | Removes spawn-invuln tags after one frame. |

### Building layout

| System | Group / notes | What it does |
|--------|---------------|----------------|
| `RoomDoorVisualBakingSystem` | baking (`PostBakingSystemGroup`) | Strips `Disabled` from baked door visuals so runtime can show them. |
| `BuildingLayoutChangedCleanupSystem` | simulation, last | Disables `BuildingLayoutChanged` after consumers have seen it. |

### Spatial occupancy

| System | Group / notes | What it does |
|--------|---------------|----------------|
| `SpatialOccupancyUpdateSystem` | after transforms, before event enable | Resolves cell/room from layout blob; enables change tags; emits room events. |
| `SpatialOccupancyCleanupSystem` | simulation, last | Disables `GridCellChanged` / `RoomChanged` after consumers. |

### Camera, spawn, radar, TTL, events

| System | Group / notes | What it does |
|--------|---------------|----------------|
| `PrefabSpawnerSystem` | initialization | Instantiates authored prefabs at positions, then disables itself. |
| `SafeRandomSpawnerSystem` | simulation, first | Random XZ spawns avoiding spawn-protection spheres. |
| `CameraAnchorSpawnSystem` | simulation, first | Builds the camera-anchor grid from config (once). |
| `CameraAnchorFollowSystem` | after Rukhanka, before transforms | Moves the GameObject camera toward the nearest anchor. |
| `RadarRendererSystem` | `SystemBase` | Paints a heatmap texture from `HeatSignatureData`. |
| `TtlSystem` | simulation | Counts down `TtlData` and destroys expired entities. |
| `EnableAllEcsEventsSystem` | generated | Sets `Enabled` on new frame-event entities. |
| `CleanupAllEcsEventsSystem` | generated, after enable | Destroys enabled frame-event entities. |
| `JumpEventLoggerSystem` | between enable and cleanup | Debug-logs enabled jump events. |

---

## Related (not components / systems)

- **Bakers / authoring** live next to many of the types above; they only exist at bake time.
- **`EcsSpawnBridge`** (MonoBehaviour) consumes `SpawnRequest` and instantiates registered prefabs.
- **`PlayerInventoryManager`** (MonoBehaviour) reads the weapon catalog and spawns the active weapon entity.
- **`BuildingLayoutEcsBridge`** (MonoBehaviour on `BuildingRunDirector`) commits `BuildingRunState` into the layout singleton and room entities.
- **`RoomIdDebugOverlay`** (MonoBehaviour) optionally draws room instance ids at each layout cell center in the Game view (`drawRoomIds`).
- **`MoveToStats` / `SpawnConfigBlob` / `BuildingLayoutBlob`** are blob payloads referenced by components, not `IComponentData`.

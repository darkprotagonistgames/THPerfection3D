# Game design — TH Perfection

Authoritative high-level design for gameplay, runs, and world simulation. Read this before adding systems that touch rounds, omens, events, spawning, or the persistent map.

## Genre and moment-to-moment play

- **Top-down bullet hell** during active gameplay sections.
- Combat and hazards are read from above; movement and patterns should assume a top-down plane (see project top-down / plane conventions in code where relevant).

## “Round-based” means two things

1. **Story / run structure** — The overall experience advances in **rounds** (macro beats of a run or campaign arc), not as one uninterrupted continuous level.
2. **Roguelike-style rounds** — Each run is built from repeated **round cycles** that layer outcomes onto a world that **accumulates** rather than resetting cleanly between rounds.

Design features, UI, and ECS systems should respect both: narrative pacing *and* procedural run layering.

## Active world during gameplay

**Everything on the map should be active** while the player is in a gameplay section.

- Enemies, hazards, props, and other map entities are expected to **simulate and interact**, not sit idle until triggered.
- Avoid designs that freeze the world except for a small “arena” bubble unless explicitly scoped as an exception.

### Why ECS (Unity Entities)

Persistent, many-entity simulation is a core requirement:

- Large numbers of entities can be updated every frame without a heavy `MonoBehaviour` per object graph.
- Systems can run over **all relevant entities** on the map during gameplay.
- New event-driven spawns and location additions must **plug into the same simulation**, not a separate non-ECS island.

When adding gameplay code, prefer **ECS systems and components** that match existing project patterns unless there is a documented exception.

## Run world generation (interacting layers)

World content for a **run** is not produced by a single generator pass. Several **interacting elements** combine:

- Baseline layout / geography for the run.
- **Omen** choices (see below) that bias what can happen next.
- **Events** rolled and applied each round (spawns, new locations, modifiers, etc.).
- **Leftover state** from prior rounds (entities and structures still in the world).

No single subsystem “owns” the whole map; designs must specify **how** a new piece cooperates with omens, event tables, and existing entities.

## Round cycle — omens

At the **start of each round** (between gameplay sections or as defined by mode flow):

1. The player is shown **three omens**.
2. The player **discards one** omen.
3. The **remaining pair** defines (or strongly biases) which **events** are possible or **more probable** for that round.

Implications for implementation:

- Omen definitions should encode **compatibility** or **weighting** toward event categories, not only flavor text.
- Discarded vs kept omens should be **logged** on the run state so later systems (events, UI, debug) can read the active pair.
- UI must make the discard choice explicit (three presented → one removed → two active).

## Round cycle — events

After omens resolve, **events are rolled** and **applied to the world**.

Examples of event outcomes (non-exhaustive):

- **Spawn enemies** (or waves) into the live map.
- **Add locations** (rooms, landmarks, interactables, faction pockets).
- Apply buffs, hazards, faction pressure, or narrative triggers.

Events should target the **persistent run world**, not a disposable sub-scene that is thrown away when the round ends.

## Persistence between rounds

Critical rule: **nothing is removed between rounds** solely because a round ended.

- Structures, enemies, pickups, and other entities **remain** unless the player or a specific in-world system destroys them.
- Entities still on the map **continue to cause changes** (AI, spawning children, territorial effects, etc.) into later rounds.

Do **not** default to:

- Full map clears between rounds.
- Despawning all enemies when exiting a gameplay section.
- Regenerating the entire terrain from scratch each round without an explicit, documented event.

When adding “end of round” logic, prefer **phase transitions** (omen UI → event application → next gameplay section) over **world teardown**.

## Design checklist for new features

| Question | Expected answer |
|----------|-----------------|
| Does this run during active gameplay on the map? | Yes, or justify as menu/meta-only |
| Does it use ECS for entities that must stay active? | Yes for map entities |
| Does it respect the two kept omens for this round? | If event-related, yes |
| Does it mutate the persistent run world? | Events and spawns should |
| Does it despawn or wipe the map on round end? | No, unless a named exception |

## Related docs in this folder

- [README.md](README.md) — Design Dock index
- [FactionPaletteRules.md](FactionPaletteRules.md) — faction colors for UI, cards, characters
- [palette-reference.json](palette-reference.json) — hex values and palette paths

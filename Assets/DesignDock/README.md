# Design Dock — TH Perfection

Central design reference for this Unity project. Agents and contributors should start here for **game loop**, **simulation constraints**, and **visual palette** rules.

## Game design

| Doc | Contents |
|-----|----------|
| [GameDesign.md](GameDesign.md) | Round-based top-down bullet hell; roguelike rounds; omens (pick 3, discard 1); event rolls; persistent active map; ECS; run world layers |
| [LevelGenPlan.md](LevelGenPlan.md) | Procedural office building (3 floors), grid rooms, doorway expansion, prefab authoring, door lifecycle |

**Short summary:** Gameplay is active top-down bullet hell on a map where all entities simulate. Runs advance in **rounds**. Each round the player keeps **two of three omens**, which shape **event** probability; events add spawns, locations, etc. **Nothing is wiped between rounds**—the world accumulates.

## Color and art

Palettes imported from `C:\Users\gmcal\.cursor\projects\THperffectionArt\color-pallets`.

| Doc | Contents |
|-----|----------|
| [FactionPaletteRules.md](FactionPaletteRules.md) | Neutral / Lu / HP / J factions, blood red, card vs enemy usage |
| [palette-reference.json](palette-reference.json) | Hex values and `.gpl` paths |
| [ColorPalettes/](ColorPalettes/) | Aseprite-ready `.gpl` files (UI, card art, enemies) |

### Quick palette hex

| Role | Neutral | Lu | HP | J |
|------|---------|----|----|---|
| BG 1 | `#1E1F1B` | `#241718` | `#141C23` | `#1B1D28` |
| BG 2 | `#35372F` | `#3B2322` | `#21313B` | `#2D3143` |
| BG 3 | `#575548` | `#5A3430` | `#31484D` | `#484B5B` |
| FG 1 | `#8A816B` | `#D46A3A` | `#83C7B5` | `#E4D8B8` |
| FG 2 | `#B4AA8B` | `#F0C06A` | `#C7E6D8` | `#F6F1DE` |
| FG 3 | — | `#F4E3B2` | `#E7FFF4` | `#FFF8EE` |
| Blood | `#C8262D` (all factions, injury only) |

### Aseprite

Palette panel → **Load Palette…** → file under `ColorPalettes/`.

### MagicaVoxel

See `Assets/voxFIles/` — `THPerfection-palette-8x32.png`, `palette-slot-map.md`.

## Syncing palettes from art repo

Copy `THperffectionArt/color-pallets/*.gpl` into `DesignDock/ColorPalettes/` and regenerate the MagicaVoxel PNG if colors change.

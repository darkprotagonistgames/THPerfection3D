# MagicaVoxel palette slot map

Layout: **8 columns × 32 rows**. Index = `row × 8 + column` (0-based). MagicaVoxel maps PNG cell order to palette indices when you drag `THPerfection-palette-8x32.png` onto the palette grid.

## Row 0 (indices 0–7)

| Idx | Name | Hex |
|-----|------|-----|
| 0 | Empty | transparent |
| 1 | Neutral_BG_1 | `#1E1F1B` |
| 2 | Neutral_BG_2 | `#35372F` |
| 3 | Neutral_BG_3 | `#575548` |
| 4 | Neutral_FG_1 | `#8A816B` |
| 5 | Neutral_FG_2 | `#B4AA8B` |
| 6 | Blood_C8262D | `#C8262D` |
| 7 | (reserved) | `#1E1F1B` |

## Row 1 — Lu (indices 8–15)

| Idx | Name | Hex |
|-----|------|-----|
| 8 | Lu_BG_1 | `#241718` |
| 9 | Lu_BG_2 | `#3B2322` |
| 10 | Lu_BG_3 | `#5A3430` |
| 11 | Lu_FG_1 | `#D46A3A` |
| 12 | Lu_FG_2 | `#F0C06A` |
| 13 | Lu_FG_3 | `#F4E3B2` |
| 14–15 | reserved | `#1E1F1B` |

## Row 2 — HP (indices 16–23)

| Idx | Name | Hex |
|-----|------|-----|
| 16 | HP_BG_1 | `#141C23` |
| 17 | HP_BG_2 | `#21313B` |
| 18 | HP_BG_3 | `#31484D` |
| 19 | HP_FG_1 | `#83C7B5` |
| 20 | HP_FG_2 | `#C7E6D8` |
| 21 | HP_FG_3 | `#E7FFF4` |
| 22–23 | reserved | `#1E1F1B` |

## Row 3 — J (indices 24–31)

| Idx | Name | Hex |
|-----|------|-----|
| 24 | J_BG_1 | `#1B1D28` |
| 25 | J_BG_2 | `#2D3143` |
| 26 | J_BG_3 | `#484B5B` |
| 27 | J_FG_1 | `#E4D8B8` |
| 28 | J_FG_2 | `#F6F1DE` |
| 29 | J_FG_3 | `#FFF8EE` |
| 30–31 | reserved | `#1E1F1B` |

## Indices 32–255

Filled with `#1E1F1B` (Neutral_BG_1) so unused MagicaVoxel slots stay on-palette if accidentally picked.

# MagicaVoxel palettes — TH Perfection

Voxel models in this folder should use the project palette so imports match in-game faction colors.

## Files

| File | Purpose |
|------|---------|
| `THPerfection-palette-8x32.png` | 8×32 grid for MagicaVoxel (256 slots) |
| `THPerfection-magica-palette.gpl` | Same colors for Aseprite / GIMP |
| `palette-slot-map.md` | Slot index → color name |

Full rules and UI/card palettes: `Assets/DesignDock/`.

## Load palette in MagicaVoxel

1. Open or create a `.vox` model.
2. Open the **Palette** panel (bottom of editor).
3. **Right-drag** `THPerfection-palette-8x32.png` onto the palette grid  
   (or use palette import if your build exposes it).
4. Confirm slots **1–6** (Neutral + blood) and faction rows match `palette-slot-map.md`.
5. Paint voxels using those indices only for character art.

## Workflow tips

- Use **Lu / HP / J** FG slots for faction characters; Neutral FG for shared metal/skin accents.
- **Blood** is slot **6** — injury/voxel gore only.
- Slot **0** is empty / transparent; do not use for visible voxels unless you intend erase.
- After editing, save `.vox` here; Unity’s Voxel Toolkit importer reads embedded palette colors from the file.

## Updating

If `DesignDock/ColorPalettes/` changes, regenerate `THPerfection-palette-8x32.png` from the master color list (see Design Dock README).

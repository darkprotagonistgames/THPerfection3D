# Faction palette and style

Imported from THperffectionArt `.cursor/rules/faction-palette-and-style.mdc`.

## Palettes

Maintain four palettes: **Neutral**, **Lu**, **HP**, **J**.

Shared blood red (hits / injury only): **`#C8262D`**

### Neutral

- BG: `#1E1F1B`, `#35372F`, `#575548`
- FG: `#8A816B`, `#B4AA8B`

### Lu

- BG: `#241718`, `#3B2322`, `#5A3430`
- FG: `#D46A3A`, `#F0C06A`, `#F4E3B2`

### HP

- BG: `#141C23`, `#21313B`, `#31484D`
- FG: `#83C7B5`, `#C7E6D8`, `#E7FFF4`

### J

- BG: `#1B1D28`, `#2D3143`, `#484B5B`
- FG: `#E4D8B8`, `#F6F1DE`, `#FFF8EE`

## Characters and enemies

- Sprite-sheet **backgrounds** use only that character’s faction BG colors.
- Character / enemy pixels use only that faction’s FG colors.
- May also use **Neutral** FG (`#8A816B`, `#B4AA8B`) as support accents.
- Do **not** use another non-neutral faction’s FG on a character or enemy.

## Card art

- May always use Neutral FG and Neutral BG support colors.
- Backgrounds must be **fully opaque** and completely filled (no transparent pixels).
- Faction card art (`Lu`, `HP`, `J`): map hues to the strict faction palette before neutral support; never use another faction’s non-neutral colors.
- Closest-match guidance:
  - **Lu:** warm oranges / golds → Lu FG swatches (not HP teal or J ivory).
  - **HP:** cool teals / mints → HP FG swatches (not Lu orange or J ivory).
  - **J:** parchment / ivory → J FG swatches (not Lu orange or HP teal).

## Generation prompts

For any generated or edited asset (UI, card, in-game):

> Adhere strictly to the approved palette; do not introduce off-palette colors.

## Readability

Any faction foreground must remain legible on any other palette background.

- [x] A1: Replace the two-level navigation with one shell-owned cache-type row, including Models.
- [x] A2: Remove obsolete browser navigation code; verify route/query/detail selection and canceled transitions.
- [x] A3: Apply the existing Material search/result structure to Models and verify selection/reload.
- [x] A4: Complete automated and desktop/phone browser checks, preserving legacy sprite removal.


## Final validation

Admin suite: 234 tests passed across 20 files. Production build passes without warnings; Biome error gate, Fallow audit with zero introduced findings, strict OpenSpec validation and diff whitespace checks pass. Existing polyfills test warning remains.

The shared type row now contains Items, NPCs, Objects, Animations, Maps, Sprites, Quests, Graphics, Varp bits, Client map definitions, Config definitions, CS2 scripts, CS2 integers and Models. The previous Definitions/Models/Sprites row is removed. Sprite manager files/route remain deleted; the current browser and Sprite Studio remain.

Measured changed-page coverage: 100% lines, 93.58% statements, 89.83% branches and 93.87% functions. Existing fallback/guard and model reload-without-preview branches remain partially uncovered; no exclusions or thresholds were changed.

Browser checks use isolated fixture responses. Confirmed one cache navbar, Models navigation and shared Material lookup layout, current sprite browser -> Sprite Studio selection, and no page overflow at 390px width. Native Material arrows paginate the long type row. No editor console errors observed. Live authentication, cache writes and actual model rendering remain unverified.

Evidence: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a19264-ebde-7101-9f0c-c53b7278cfed/single-cache-tabs-models.png` and `single-cache-tabs-phone.png`.

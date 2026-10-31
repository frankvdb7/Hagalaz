- [x] A1: Remove legacy route/page/tests and orphaned metadata/search state and service methods.
- [x] A2: Route Sprites to the current browser and derive mutually exclusive resource selection from router state.
- [x] A3: Retain current editor/guard tests and update navigation and store regression coverage.
- [x] A4: Run validation and desktop/phone browser checks; record remaining limitations.


## Final validation

Admin suite: 234 tests passed across 20 files. Production build passes without warnings; Biome error gate, Fallow audit with zero introduced findings, strict OpenSpec validation and diff whitespace checks pass. Existing polyfills test warning remains.

The shared type row now contains Items, NPCs, Objects, Animations, Maps, Sprites, Quests, Graphics, Varp bits, Client map definitions, Config definitions, CS2 scripts, CS2 integers and Models. The previous Definitions/Models/Sprites row is removed. Sprite manager files/route remain deleted; the current browser and Sprite Studio remain.

Measured changed-page coverage: 100% lines, 93.58% statements, 89.83% branches and 93.87% functions. Existing fallback/guard and model reload-without-preview branches remain partially uncovered; no exclusions or thresholds were changed.

Browser checks use isolated fixture responses. Confirmed one cache navbar, Models navigation and shared Material lookup layout, current sprite browser -> Sprite Studio selection, and no page overflow at 390px width. Native Material arrows paginate the long type row. No editor console errors observed. Live authentication, cache writes and actual model rendering remain unverified.

Evidence: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a19264-ebde-7101-9f0c-c53b7278cfed/single-cache-tabs-models.png` and `single-cache-tabs-phone.png`.

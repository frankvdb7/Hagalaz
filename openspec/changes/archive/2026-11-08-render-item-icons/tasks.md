# Tasks

## 1. Shared inventory-model assembly

- [x] 1.1 Extract the decoding, scaling, recolour, retexture and texture-coordinate logic of `AssembleModel` into a method returning assembled model data; map it to `GetModelDto` in the existing endpoints. Verify: all existing `Hagalaz.Services.Cache.Tests` model and item model tests pass unchanged (acceptance 6).

## 2. Icon renderer

- [x] 2.1 Port client HSL palette and model lighting to .NET with the icon light (ambient `64 + ambient`, contrast `768 + contrast`, sun (-50, -10, -50), intensity 1.0). Verify: unit tests compare palette entries and per-vertex shades with values computed from the client routines.
- [x] 2.2 Implement the client icon camera and transform order (zan2d, rotation2, zoom/offset translation, rotation1; zoom ×1.5 and ×1.04 cases) and projection for 36×32 and scaled sizes. Verify: unit tests pin projected vertex positions for known settings and for a scaled size.
- [x] 2.3 Implement triangle rasterisation: depth-tested opaque faces, back-to-front translucent faces, flat and Gouraud colour, hidden faces, and perspective-correct textured faces with alpha modes. Verify: golden RGBA tests for plain, translucent and textured fixture models.
- [x] 2.4 Implement the outline (levels 0–2) and shadow passes and transparent-pixel normalisation from the client. Verify: unit tests on small pixel buffers reproduce the client passes exactly.

## 3. Item image endpoint

- [x] 3.1 Replace the static GIF lookup with render-on-request PNG output: resolve the item and template items, assemble models, decode textures, render, encode PNG, set `ETag` (SHA-256 of bytes) and `Cache-Control: private, no-cache`, and answer matching `If-None-Match` with 304. Verify: endpoint tests for 200 PNG 36×32, identical bytes and ETag on repeat, and 304 (acceptance 1, 4).
- [x] 3.2 Composite noted, lent and bought items with their templates in client order. Verify: endpoint golden test for a noted item and a lent or bought item (acceptance 1).
- [x] 3.3 Add `width`, `height` (8–512), `outline` (0–2) and `shadow` (`none` or `RRGGBB`) parameters with validation. Verify: tests for custom size dimensions, outline and shadow effects, distinct ETags, and 400 for each invalid value (acceptance 2).
- [x] 3.4 Map items without an inventory model and missing models to 404, and undecodable models to 422 problem responses. Verify: endpoint tests for each case (acceptance 3).
- [x] 3.5 Show that cache changes reach the icon: updating the item definition changes the bytes and ETag, and a duplicated item renders immediately. Verify: endpoint tests using the existing definition update and duplicate endpoints (acceptance 4, 5).

## 4. Remove static sprites and verify

- [x] 4.1 Delete `Hagalaz.Services.Cache/wwwroot/images/items` and any web-root configuration that only served them; update tests that seeded GIFs. Verify: `dotnet test Hagalaz.Services.Cache.Tests` passes and no code references `images/items` (acceptance 7).
- [x] 4.2 Confirm the default shadow colour and outline level against the client's inventory interface component and adjust the defaults if they differ. Verify: the decision and source are recorded in this file.
- [x] 4.3 Run the cache library, cache service and admin test suites and the admin build; record results and any new warnings here. Verify: commands pass (acceptance 6, 7).
- [x] 4.4 Render the golden item set to PNG files outside the repository and compare them visually with client screenshots or the previous GIFs; record the comparison notes here. Verify: notes recorded before archive (acceptance 1).

## Implementation notes

- 4.2 Confirmed from the pinned 742 cache: backpack interface 679 component 0 runs script 1613 on open and on inventory change. Script 1613 sets shadow `0x302020`, places items from inventory 93 with quantity mode 2 and no appearance recolouring, and sets outline 1 (outline 2 only for the slot selected for "Use", via varc 1031 and script 1615). The generic item grid (scripts 150 -> 153 -> 154) uses the same outline 1 and shadow `0x302020`. The defaults match the unselected backpack slot.
- Back faces are culled like the client: cache winding is clockwise on the Y-down screen. Confirmed on the real cache by comparing both windings against the reference set (party hat interior no longer visible).
- Rotation directions (Z, Y, X) and the translation order were confirmed on the real cache; flipping pitch or yaw makes renders diverge or leave the frame.
- 4.3 Verification, 2026-11-07: `Hagalaz.Cache.Tests` 463 passed, `Hagalaz.Services.Cache.Tests` 195 passed, admin tests 321 passed, admin production build succeeded. No new warnings in the changed files.
- 4.4 Renders of 15 real items (plain, textured, translucent, rotated/offset, noted) were written outside the repository and compared with the previous GIFs at 6× zoom. Framing, orientation, outline and shadow match closely: silhouette IoU against the 32×32 GIFs is 0.81–0.96 for most items. Two blades (1305, 4587) differ in angle with no `zan2d`, which points to a different model or definition in the GIF source rather than the camera. Noted items now show the original item on the note, where the GIFs showed blank paper. No in-game client screenshots were compared.

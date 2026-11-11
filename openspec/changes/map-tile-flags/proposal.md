# Proposal

## Why

The map editor can change terrain heights and floors but cannot see or change the per-tile flags that decide where players can walk, which columns are bridges and which tiles sit under a roof. An administrator who reshapes terrain therefore cannot make the new ground walkable or blocked, and cannot check what the game server will treat as blocked.

## What Changes

One goal: let an administrator see and edit terrain tile flags in the admin map editor, with the editor's idea of "blocked" matching the game server's.

In scope:

- A pure web helper that turns a tile's flag value into named facts, using the bit meanings verified against the client (see design.md, "Phase 0 findings").
- A walkability overlay view option, off by default, that marks blocked, bridge, roof and minimap flags per tile and plane, applies the client's bridge plane shift, and includes object collision.
- Brush tools that set or clear one flag bit, with the same stroke preview, undo and save path as the existing terrain brushes.
- An optional flag value on a terrain tile change, written by the terrain patcher for edited tiles only.
- Validation that rejects a flag value the format cannot store before anything is written.
- A pinned real-region check that the editor's and the server's terrain blocking agree.
- The save confirmation's "tile flags are not changed" note is replaced, because flags can now change.

Non-goals:

- Pushing changes to a running game world.
- Editing object collision, or changing game server collision.
- Conflict detection between administrators.
- A general terrain encoder; underwater terrain (`um`), atmosphere and light records.

Acceptance criteria:

- An edited flag round trips through save and reload; an unchanged tile stays byte-identical; a save with no effective change reproduces the archive byte for byte; data after the terrain survives.
- A flag value above 32 is rejected with a validation error and nothing is written.
- Toggling the overlay does not rebuild terrain geometry, and the overlay follows unsaved edits.
- For pinned region 12850 the editor helper and the server's `MapProvider` produce the same blocked tiles per collision plane.
- `openspec validate map-tile-flags --strict` passes and the frontend and focused backend checks listed in tasks.md pass.

Stop conditions:

- If the editor and server disagree on terrain blocking, stop, identify the wrong side and report it; server collision is not changed here.
- If object collision cannot be shown with the server's footprint and rotation rules within this change, ship terrain flags only, label the overlay "terrain only" and record the limitation in design.md.

Reused mechanisms: `MapTerrainPatcher` and `MapTerrainWriter` for encoding and verification, the existing `POST /maps/{id}/terrain` endpoint, `map-terrain-edit.ts` for edits, strokes and saves, the brush panel, `map-scene-geometry.ts` for geometry, `ModelScene` for GPU resources, and the existing world map refresh after a save. No new endpoint, store, worker or dependency.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-map-terrain-mutations`: a tile change may carry a flag value; flags are preserved only for tiles whose change omits them; unstorable flag values are rejected; the editor gains flag brushes and the save confirmation changes.
- `map-editor-live-painting`: a flag brush shows its effect during the drag.
- `web-3d-map-rendering`: adds the walkability overlay and its agreement with server terrain blocking.

## Impact

- `Hagalaz.Cache`: `MapTerrainTileChange`, `MapTerrainPatcher`, `MapTerrainWriter`.
- `Hagalaz.Services.Cache`: terrain save validation in `MapDefinitionsEndpoints`; object appearance DTO gains the collision fields the overlay needs.
- `Hagalaz.Web.App` admin app: `map-render.models.ts`, new `map-tile-flags.ts`, `map-terrain-edit.ts`, `map-scene-geometry.ts`, `map-terrain-panel.component.ts`, `map-viewport.component.ts`, `maps-detail.page.*`; `shared/model-viewer/model-scene.ts` for the overlay mesh.
- Tests in `Hagalaz.Cache.Tests`, `Hagalaz.Services.Cache.Tests` and the admin and shared web specs.
- The API change is additive: an omitted flag value keeps today's behaviour.

# Proposal

## Why

The map editor's walkability overlay (change `map-tile-flags`) shows terrain flags and an approximation of object collision. It does not show what the game server's pathfinding actually reads: it misses objects that have no model, sizes footprints from the appearance instead of the placed definition, draws a wall only on its own tile and not the neighbour it also blocks, and ignores projectile (line-of-sight) blocking entirely. An administrator therefore cannot trust the overlay to answer "can a player walk or shoot here?".

## What Changes

One goal: make the existing walkability overlay show the game server's full static collision for each tile, for both movement and projectiles.

In scope:

- The walkability overlay derives its object and terrain markers from one per-tile collision value that matches the game server's static collision bits for the same terrain and placements (floor block, floor decoration, object tile, solid object, walls in eight directions, projectile-blocking walls).
- Object collision uses the placed definition's size, clip type, solid and gateway values, so objects without a model are included and the footprint uses the server's sizes.
- Walls and corners mark both tiles they affect, including tiles in a neighbouring loaded region.
- Movement and projectile clipping appear together, with distinct colours and a shared legend.
- Hovering a tile while the overlay is on shows its collision plane and the names of its collision bits.
- A pinned real-region check that the editor's collision values equal the game server's `MapRegion` collision for region 12850.
- A small batch read of object collision fields by definition ID, because the appearance endpoint does not answer for objects without a model.

Non-goals:

- A new overlay, view option or scene mesh; the existing walkability option, overlay builder and `ModelScene.setTileOverlay` are reused.
- Editing collision or object definitions; changing game server collision.
- Collision from database object spawns, scripts, NPCs or anything added at run time; the overlay shows cache data only and says so.
- Clipping across regions that are not loaded in the editor.

Acceptance criteria:

- For pinned region 12850, the editor's collision value per tile and plane equals the game server's `MapRegion` collision after loading the same terrain and landscape, in the normal test run.
- An object with no model but a blocking definition is marked; a rotated 2×1 object uses the definition's sizes.
- A wall marks its own tile edge and the facing edge of its neighbour.
- Solid objects and walls use distinct colours to show that they also block shots.
- Unsaved terrain flag and placement edits update the overlay; toggling it does not rebuild terrain or object geometry.
- `openspec validate map-collision-view --strict` and the checks in tasks.md pass.

Stop conditions:

- If the editor and server disagree on region 12850, stop and identify which side is wrong; do not change server collision in this change.
- If the server's collision depends on data the editor cannot obtain from the cache service, record it as a stated limitation instead of adding a new data path.

Reused mechanisms: `map-walkability.ts`, the walkability view option, `ModelScene.setTileOverlay`, `map-tile-flags.ts` (`collisionPlane`), the edited placements already passed to the overlay, the pinned fixtures in `TestSupport/Fixtures/maps`, and `ObjectDefinitionsEndpoints`. No new store, worker or dependency.

Prerequisite: `map-tile-flags` introduces the walkability overlay requirements this change adds to; it should be archived (or its specs synced) before this change is archived.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-3d-map-rendering`: adds requirements for full server collision in the walkability overlay, its movement and projectile markers, tile inspection, server agreement, and the batch read of object collision fields it depends on.

## Impact

- `Hagalaz.Web.App` admin app: new pure `map-collision.ts` (collision values), `map-walkability.ts` (drawn from collision values), `map-render.models.ts`, the cache types service, `maps-detail.page.*`, `map-viewport.component.ts` (hover readout).
- `Hagalaz.Services.Cache`: `ObjectDefinitionsEndpoints` gains `POST /types/objects/collision`.
- Test fixtures: `collision-12850.json` and `object-collision-12850.json` in `TestSupport/Fixtures/maps`, produced from the game server's `MapRegion`.
- Tests in `Hagalaz.Services.GameWorld.Tests` (or the existing map tests project), `Hagalaz.Services.Cache.Tests` and admin web specs.

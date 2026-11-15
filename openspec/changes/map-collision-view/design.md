# Design

## Context

See proposal.md for motivation. Current state:

- `map-walkability.ts` `buildWalkabilityOverlay` draws terrain flag markers and object markers into one `StrokePreviewGeometry`, which `ModelScene.setTileOverlay` owns. Object markers come from `ObjectRenderAppearance` (`clipType`, `solid`, `gateway`, appearance sizes) and draw a wall only on its own tile. Limitations are recorded in `map-tile-flags/design.md` and the option label "Walkability (flags, drawn objects)".
- The game server writes static collision in `MapRegion` (`Hagalaz.Services.GameWorld/Model/Maps/Regions/CollisionMethods.cs`): `FloorBlock` from terrain (`MapProvider.EnumerateBlockedTiles`, bridge rule), `FloorDecorationBlock` for shape 22 with clip type 1, `ObjectTile`/`ObjectBlock`/`ObjectAllowRange` over a rotated footprint for standard shapes 9–21, and three wall layers (`Wall*`, `Blocked*` when solid, `WallAllowRange*` when not a gateway) on the origin tile and its neighbour for shapes 0–3. Writers are skipped when clip type is 0 and the object is not a gateway. Objects are lowered a plane on bridge columns by `MapCodec.DecodeObjectData`. The `map-collision-writer` spec pins these directions.
- Movement step validation (`Movement` → `CheckStep`) reads the `Check*` combinations (floor, floor decoration, object tile, `Wall*`); this is what actually stops a step. Route planning in `SmartPathfinder`/`DumbPathFinder` reads the `Traversable*` combinations (floor, floor decoration, `WallAllowRange*`, `ObjectAllowRange`), which omit gateway objects and gateway walls, so a planned route can pass a gateway that step validation then refuses. `ProjectilePathfinder` reads `ObjectBlock` and `Blocked*` only.
- Movement markers show step validation (`Check*`). The route-planning disagreement on gateways is a server question outside this change; the tile readout lists every bit, so a gateway tile is identifiable by `ObjectTile` without `ObjectAllowRange`.
- The server also adds database object spawns and run-time collision; the cache editor has neither.

## Goals / Non-Goals

**Goals:** one editor-side owner of the collision rules; the existing overlay draws from it; pinned agreement with the server's actual `MapRegion` output.

**Non-Goals:** sharing code between C# and TypeScript; a server endpoint that computes collision; per-flag filter toggles.

## Decisions

### 1. Collision values are computed in the editor, pinned to the server by a fixture

New pure module `map-collision.ts`: `buildCollisionMap(regions, placements, definitions) → Map<regionId, Int32Array[4]>` (4096 values per plane, server bit layout, bit constants named after `CollisionFlag`). It reuses `collisionPlane`/`blockedPlane` from `map-tile-flags.ts` and ports the writer table from `CollisionMethods.cs` as data (shape × rotation → origin and neighbour bits per layer), not as branching code.

Why: the overlay must follow unsaved edits in the browser; the previous change established the same pattern (pure helper + pinned fixture) for terrain blocking.

Rejected: a cache-service endpoint that runs the server's writer. It cannot see unsaved edits without posting the whole edited region on every stroke, and the cache service does not reference `Hagalaz.Services.GameWorld`; moving `MapRegion`'s writers into a shared library is a larger refactor than this goal.

### 2. Agreement fixture produced from the real server writer

A `Hagalaz.Services.GameWorld.Tests` test loads region 12850 through `MapRegionLoader` (existing test setup in `MapRegionLoaderTests`/`CollisionMethodsTests`) from `terrain-12850.dat` and `landscape-12850.dat` plus `object-collision-12850.json`, and asserts the region's collision equals `collision-12850.json`. A web spec feeds the same three fixtures to `buildCollisionMap` and asserts the same `collision-12850.json`. `object-collision-12850.json` holds `{id, sizeX, sizeY, clipType, solid, gateway}` for each distinct placed ID, captured once from the cache; the README records its provenance. Neighbour writes that fall outside region 12850 are excluded on both sides (the server test has no neighbours loaded).

Rejected: a cross-runtime harness; needs a live cache in the normal run (same reasoning as `map-tile-flags`).

### 3. Object collision fields come from a batch definition read

`POST /types/objects/collision` in `ObjectDefinitionsEndpoints` takes up to 256 IDs and returns per-ID `{id, sizeX, sizeY, clipType, solid, gateway}` or not-found, mapped from the same `IObjectType` the server uses (base definition, no transform). The admin cache service caches results per ID for the page's lifetime alongside appearances.

Why not reuse `render-appearances`: it is keyed by placement and shape, answers not-found when there is no model, and reports transform-variant sizes. Why not `GET /types/objects/{id}` per ID: a region has hundreds of distinct IDs. Why not `objects/range`: placed IDs are sparse.

### 4. One overlay draws movement and projectile collision

`buildWalkabilityOverlay(regions, visiblePlanes, collision)` draws terrain flags, green walkable floor insets, orange movement-only blockers, pink objects that also block shots, and cyan edges or corners that also block shots. Collision values contain both sides of a wall. The legend shares the overlay palette and stays inside the viewport. Toggling Walkability changes only the overlay mesh.

### 5. Hover readout uses the existing brush hover point

`map-viewport.component.ts` already emits `brushHover` tile points. When the overlay is on the page derives `{x, y, plane, collisionPlane, names}` from the collision map and the viewport shows it as a small status line. No new picking.

## Ownership

| Responsibility | Owner |
| --- | --- |
| Server collision rules | `MapRegion` (unchanged) |
| Editor collision values | `map-collision.ts` |
| Agreement between them | the two fixture tests |
| Object collision fields | `ObjectDefinitionsEndpoints` → admin cache service |
| Overlay geometry and GPU lifetime | `map-walkability.ts`, `ModelScene.setTileOverlay` (unchanged) |

## Risks / Trade-offs

- [Rules drift when `CollisionMethods.cs` changes] → the GameWorld fixture test fails first; the writer table in TS mirrors the C# structure so the diff is easy to follow.
- [Database spawns and run-time collision are absent] → the option label says "cache objects only"; tile readout does the same.
- [Large multi-region views] → collision is rebuilt per edited region only; unchanged regions keep their arrays.
- [Prerequisite change unarchived] → this change only adds requirements; archive `map-tile-flags` first.

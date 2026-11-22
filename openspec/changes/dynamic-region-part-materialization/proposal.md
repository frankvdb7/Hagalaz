# Proposal

## Why

Dynamic region parts (8x8 chunks copied, rotated or deleted into an instance region) do not produce server state that matches what the client draws (GitHub issue #401). Creation copies parts from absolute part coordinates `0..7` instead of the source region, rotate/delete compute the destination Y with the X conversion, copied objects keep their source orientation and are read from the destination's tile range and plane, raw source collision overwrites object collision without rotation, chunk replacement leaks disabled-static entries, and `MakeStandard()` only resets descriptors. The revision-742 dynamic map packet (#400) needs this part state to be authoritative before it can serialize it.

## What Changes

Goal: every dynamic region part's server objects and collision are a deterministic materialization of its source template, its source part and plane, and its rotation.

In scope:

- One owner prepares a region's **map template**: cache object placements, blocked floor tiles and configured database object spawns, with definitions resolved. `MapRegionLoader` uses it for standard loads; dynamic parts use it instead of reading a live source region.
- Dynamic creation maps each destination local part to the source region's part at the same local index (absolute source part coordinates), and rotate/delete use the Y conversion for Y.
- Materializing a part reads the source 8x8 from the part's source part X/Y and source plane, places objects at rotated positions with rotation `(source rotation + part rotation) & 3`, and flags collision from the rotated floor blocks and the placed objects through the existing collision routines. Raw source collision is never copied.
- Replacing, rotating or deleting a part discards the old part content without leaving disabled-static entries or queuing runtime removal updates. Runtime add/remove behaviour outside part rebuilds is unchanged.
- `MakeStandard()` restores self-mapped, present descriptors and rebuilds every part from the region's own template. Dynamic creation no longer calls it on the live source region.
- Part presence stays explicit (`HasDrawSource`); descriptor value `0` remains a valid present mapping.
- **BREAKING (internal API)**: `IMapRegion` exposes local-index part operations that take the source template; the ambiguous absolute-coordinate wrappers and the per-part source dimension (`DrawRegionDimension`, `WriteBlock(..., drawPartDimension)`) are removed because templates do not depend on a dimension.

Non-goals:

- The opcode 153 / `DrawDynamicMapMessage` wire format (#400) and resending maps to clients after a rebuild.
- Cloning characters, NPCs, ground items, projectiles or any other runtime entity into dynamic parts; NPC and ground-item spawns stay with the standard loader.
- Template caching, a second map decoder, or a new instance/map subsystem.
- Removing the `Dimension` model or changing region residency, readiness or teardown.

Acceptance criteria: the checklist in issue #401, verified by the scenarios in the delta spec.

Stop conditions: pause and propose a follow-up if the work requires protocol changes, a template cache, region residency changes, or changes to runtime object add/remove semantics outside part rebuilds.

## Capabilities

### New Capabilities

- `dynamic-region-parts`: how dynamic region parts map to source parts and materialize server objects and collision.

### Modified Capabilities

- `map-region-load-readiness`: the "Dynamic regions preserve source and destination dimensions" requirement changes from reading a live source region in the source dimension to materializing from the dimension-independent source template.

## Impact

- `Hagalaz.Game.Abstractions`: `IMapRegion`, `IMapRegionPart`, new map template type.
- `Hagalaz.Cache` / `Hagalaz.Cache.Abstractions`: `MapCodec`, `MapProvider` and `DecodedMapRegion` keep raw object planes and terrain flags next to the bridge-adjusted views (`MapTerrain` holds the bridge rule).
- `Hagalaz.Services.GameWorld`: `MapRegion` (editing, objects, collision), `MapRegionPart`, `MapRegionService.CreateDynamicRegion`, `MapRegionLoader` (template extraction).
- Tests in `Hagalaz.Services.GameWorld.Tests` (`DynamicRegionDimensionTests`, `MapRegionServiceTests`, `MapRegionLoaderTests`) and new dynamic part tests.
- Existing mechanisms reused: `IMapProvider.DecodeRegion`, `IGameObjectSpawnRepository`, `IGameObjectService` definition lookup, `IGameObjectBuilder`, `MapRegion` collision flagging, `MapRegionLoadScheduler` source readiness.

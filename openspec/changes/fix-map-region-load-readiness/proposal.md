## Why

Map regions currently become observable as loaded before their NPCs, ground items, static map objects, and collision flags have finished loading. Movement and pathfinding can therefore read an empty collision grid, while the scheduler suppresses subsequent load requests; a load failure after the early state change can leave the region permanently marked loaded.

## What Changes

- Publish a region as loaded only after the complete region-load pipeline succeeds, including static map collision population.
- Keep a region eligible for a later load attempt when loading is canceled or fails before completion.
- Preserve the existing single-reader `MapRegionLoadScheduler` and `IMapRegionLoader` ownership; do not add a second queue, worker, or retry mechanism.
- Add deterministic regression coverage for movement queries during loading, successful readiness publication, and retry after failure.

### Acceptance Criteria

- A pathfinding or movement collision query cannot observe a region as loaded before static collision population completes.
- A successful region load becomes loaded exactly once after all population steps complete.
- A failed or canceled load does not permanently suppress a later request for that region.
- Existing static-object coordinate handling, custom-object collision behavior, and intentional non-colliding floor-decoration behavior remain unchanged.

### Stop Conditions

- Stop if satisfying readiness requires changing pathfinding algorithms, collision flag meanings, cache decoding, or client protocol behavior; record that work as a follow-up.
- Stop if reliable retry semantics require a new persistence, queue, or background-worker mechanism beyond the existing scheduler.

## Capabilities

### New Capabilities

- `map-region-load-readiness`: Region loading exposes collision data only after complete successful population and permits recovery from failed loads.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Model/Maps/Regions/MapRegion.cs`
- `Hagalaz.Services.GameWorld/Data/MapRegionLoader.cs`
- `Hagalaz.Services.GameWorld/Services/MapRegionLoadScheduler.cs`
- `Hagalaz.Services.GameWorld/Services/MapUpdateService.cs` only if request/readiness coordination requires a focused integration adjustment.
- `Hagalaz.Services.GameWorld.Tests` region-loader/scheduler regression tests.
- No new dependencies, persisted-data changes, cache-format changes, or client changes.

## Why

Map regions are created in the active region service before loading starts. If
the loader mutates that region while a later database or cache operation can
still fail, the instance can contain incomplete collision or entities and can
become difficult to recover safely. Broad cache exception handling also turns
corrupt map data into apparently empty map data.

## What Changes

- Prepare database-backed spawn data and decoded static map data in local
  collections before mutating the region.
- Publish static collision and objects only after the complete static decode has
  succeeded, then apply configured objects/items and register NPCs near the end.
- Treat only the cache API's documented missing-file result (`-1`) as absent;
  propagate container, decryption, decompression, and decode failures.
- Treat arbitrary NPC construction failures as fatal, alongside source,
  cache, map-apply, and registration failures; preserve cancellation.
- Replace the independent loaded flag with the three-state `MapRegionState`
  lifecycle. On fatal failure, mark the exact region instance discarded,
  unregister NPCs successfully registered by this attempt, exact-remove it,
  and let a later request get a fresh region instance.
- Reject discarded or non-canonical initializing instances at the scheduler
  boundary, rebind stale viewport references to the canonical region, and
  restrict normal GameWorld ticks and collision reads to ready regions.
- Remove the obsolete in-place unpublished-region rollback machinery while
  retaining the existing scheduler coalescing and readiness gate.

## Acceptance Criteria

- A source or cache preparation failure cannot leave collision, objects, items,
  or NPCs applied to the failed region.
- Static decode callbacks stage local data and do not mutate the region until
  the entire decode succeeds.
- Unexpected cache read/decode failures remain fatal; only a genuinely absent
  named archive follows the existing empty-data semantics.
- NPC registration occurs after fatal map preparation and apply work. Any NPC
  construction or registration failure aborts the load, and cancellation is
  never swallowed.
- Every NPC successfully registered by a failed attempt is unregistered before
  the attempt completes, with cleanup failures logged without replacing the
  primary load failure.
- The failed region is removed only when the service still holds that exact
  instance; a stale failure cannot remove a replacement region.
- A later request creates a fresh region instance, and concurrent requests for
  one active instance still share one scheduler load attempt.
- New regions begin `Initializing`, successful loading publishes `Ready` last,
  and failed loading publishes terminal `Discarded` state without resetting
  the instance.
- Only ready regions participate in normal creature, map-update, GameWorker,
  or collision processing; dynamic map packet selection is independent of
  readiness, and stale references cannot schedule a replacement.
- Live command and network script entrypoints hand world mutations to the
  existing serialized GameWorker execution boundary; `MapRegionService` keeps
  residency ownership while arbitrary callbacks remain outside its gate.

## Stop Conditions

- Do not add a transaction framework, rollback coordinator, retry worker,
  persistence mechanism, or generalized region-loading abstraction.
- Do not change pathfinding algorithms, collision meanings, cache formats,
  client protocol behavior, or unrelated reconnect/session architecture.
- Do not broaden isolated-content exception handling to cache, database,
  collision-apply, or scheduler infrastructure failures.

## Capabilities

### New Capabilities

- `map-region-load-readiness`: A region becomes ready only after prepared data
  has been applied successfully, and failed instances are discarded.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Data/MapRegionLoader.cs`
- `Hagalaz.Services.GameWorld/Services/MapRegionService.cs`
- `Hagalaz.Game.Abstractions/Services/IMapRegionService.cs`
- `Hagalaz.Cache/Types/Providers/MapProvider.cs`
- `Hagalaz.Services.GameWorld/Model/Maps/Regions/MapRegion.cs`
- `Hagalaz.Services.GameWorld/Model/Maps/Regions/MapRegionPart.cs`
- `Hagalaz.Game.Abstractions/Model/Maps/MapRegionState.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Viewport.cs`
- `Hagalaz.Services.GameWorld/Services/MapRegionLoadScheduler.cs`
- `Hagalaz.Services.GameWorld/Services/MapUpdateService.cs`
- `Hagalaz.Services.GameWorld/Services/GameWorkerService.cs`
- `Hagalaz.Services.GameWorld/Services/MapRegionBackgroundService.cs`
- focused GameWorld and cache map tests
- the directly requested NPC indexed lookup and flat aggregation cleanup

No new dependencies, persisted-data changes, cache-format changes, or client
changes are required.

## Context

`MapRegionService` publishes a newly created region in its active dictionary.
`MapRegionLoadScheduler` owns asynchronous loading and coalesces requests by
region instance. `MapRegionLoader` must therefore avoid exposing partial state
and must remove a failed published instance before completing its failed load.

## Goals and non-goals

Goals are complete readiness publication, prepare-before-apply, accurate cache
failure semantics, isolated NPC content failure, cleanup of external NPC
ownership, and fresh-instance retry through the existing service/scheduler.

Non-goals are a generic transaction abstraction, a second queue or worker,
automatic retries, pathfinding changes, cache-format changes, and unrelated
GameWorld lifecycle changes.

## Decisions

### 1. Use local staging in `MapRegionLoader`

The loader queries all configured spawn sources first. Static decode callbacks
append prepared object instances and collision coordinates to local lists. The
loader builds configured items and objects before applying any region state.
These lists are private implementation details; no transaction or rollback
abstraction is introduced.

### 2. Apply prepared map state before NPC registration

After preparation succeeds, collision, static objects, configured objects, and
ground items are applied. NPCs are then built and registered. This keeps global
NPC ownership out of failures discovered during source preparation and map
construction. `MapRegion.Load()` remains the final commit/readiness signal.

### 3. Treat cache absence differently from cache failure

`ICacheAPI.GetFileId` returns `-1` when the named archive is absent. The map
provider returns `null` only for that result, preserving existing empty-map
behavior. It does not catch `ReadContainer` or decoder exceptions, so corrupt,
encrypted-with-wrong-keys, truncated, or structurally invalid data aborts the
load.

### 4. Keep isolated NPC handling at the registration boundary

`INpcService.RegisterAsync` owns cleanup for its own failed registration. The
loader catches non-cancellation exceptions from one NPC build or registration,
logs the region and NPC identity, and continues. Database, cache, map apply,
and cancellation failures remain fatal.

### 5. Clean external ownership, then discard the region

The loader keeps a local list of NPCs whose registration completed successfully.
If the attempt later fails, it tries to unregister every item in that list and
preserves cleanup failures. It then calls the map service's compare-and-remove
operation. The operation removes only the active dictionary entry whose value
is the expected instance, so a late failure cannot remove a replacement.
The failed region's partially applied local state is not reset or reused.

### 6. Preserve scheduler ownership and coalescing

The existing scheduler remains the only load worker and its in-flight map still
coalesces duplicate requests for one active region instance. Failure completion
occurs only after loader cleanup and exact removal have run. A normal later map
request resolves through `MapRegionService` and receives a new instance.

### 7. Keep NPC store lookup indexed

`NpcStore.FindByIndexAsync` uses `CreatureCollection`'s indexer under a short
`AsyncReaderWriterLock` reader lock. The unused predicate API is removed. Sync
writer/readers continue to use the lock's synchronous methods where applicable.
Registration/cleanup aggregate exceptions are flattened at the public throw
boundary.

## Failure flow

```text
create/publish R1
  -> query and decode into local data
  -> apply prepared map data
  -> register valid NPCs
  -> R1.Load() publishes readiness

fatal failure
  -> unregister only NPCs registered by this attempt
  -> exact-remove R1 if still current
  -> propagate failure; R1 is discarded

later request
  -> MapRegionService creates R2
```

## Verification

Use deterministic task gates for blocked decode/registration tests. Verify
staged data is not applied after a decode failure, cache absence versus cache
failure, valid NPC continuation, cancellation cleanup, exact replacement, and
existing scheduler coalescing. Run focused tests, strict OpenSpec validation,
build/diff checks, and broader validation when resources permit.

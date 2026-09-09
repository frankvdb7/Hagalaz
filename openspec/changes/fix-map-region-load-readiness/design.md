## Context

`MapRegionService` publishes a newly created region in its active dictionary.
`MapRegionLoadScheduler` owns asynchronous loading and coalesces requests by
region instance. `MapRegionLoader` must therefore avoid exposing partial state
and must remove a failed published instance before completing its failed load.

## Goals and non-goals

Goals are complete readiness publication, prepare-before-apply, accurate cache
failure semantics, isolated NPC construction failure, fatal NPC registration
failure, cleanup of external NPC ownership, explicit region lifecycle, stale
reference rejection, and fresh-instance retry through the existing
service/scheduler.

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

### 2. Use one explicit region lifecycle source of truth

`IMapRegion.State` is the only stored initial-load lifecycle state and uses
exactly `Initializing`, `Ready`, and `Discarded`. A new region starts
`Initializing`; only the loader can publish `Ready` after all required
population; a fatal failure or cancellation marks that instance `Discarded`.
`Ready` and `Discarded` are terminal for this lifecycle. A discarded instance
is never reset or retried.

### 3. Apply prepared map state before NPC registration

After preparation succeeds, collision, static objects, configured objects, and
ground items are applied. NPCs are then built and registered. This keeps global
NPC ownership out of failures discovered during source preparation and map
construction. `MapRegion.MarkReady()` remains the final commit/readiness signal.

### 4. Treat cache absence differently from cache failure

`ICacheAPI.GetFileId` returns `-1` when the named archive is absent. The map
provider returns `null` only for that result, preserving existing empty-map
behavior. It does not catch `ReadContainer` or decoder exceptions, so corrupt,
encrypted-with-wrong-keys, truncated, or structurally invalid data aborts the
load.

### 5. Keep isolated NPC handling at the construction boundary

`INpcService.RegisterAsync` owns cleanup for its own failed registration. The
loader catches non-cancellation exceptions from `BuildNpc`, logs the region and
NPC identity, and continues. It does not catch failures from
`INpcService.RegisterAsync`; the service remains responsible for cleaning up
its own failed registration and the region load fails. Database, cache, map
apply, and cancellation failures remain fatal.

### 6. Discard the instance and clean external ownership

The loader keeps a local list of NPCs whose registration completed successfully.
If the attempt later fails, it marks the instance discarded before cleanup so
stale references become inert immediately. It then tries to unregister every
item in that list and preserves cleanup failures before calling the map
service's compare-and-remove operation. The operation removes only the active
dictionary entry whose value is the expected instance, so a late failure
cannot remove a replacement. The failed region's partially applied local
state is not reset or reused.

### 7. Preserve scheduler ownership, coalescing, and canonical identity

The existing scheduler remains the only load worker and its in-flight map still
coalesces duplicate requests for one active region instance. It schedules only
an `Initializing` region that is still the exact current instance in
`MapRegionService`; `Ready` is a no-op and `Discarded` is rejected. Failure
completion occurs only after loader cleanup and exact removal have run. A
normal later map request resolves through `MapRegionService` and receives a
new instance.

### 8. Keep stale consumers fail-closed

`Viewport` rebinds retained region references through `MapRegionService` when
they are exposed or processed. Only ready regions contribute creatures,
dynamic-map state, full region updates, or world ticks. Initializing canonical
regions may still be submitted for loading; discarded or stale instances are
not. The background lifecycle service also leaves initializing regions active
until they publish readiness. The GameWorker filters its snapshot to ready
regions before any major tick phase, and collision returns `FloorBlock` for
every non-ready state.

### 9. Keep NPC store lookup indexed

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
  -> R1.MarkReady() publishes readiness

fatal failure
  -> mark R1 Discarded
  -> unregister only NPCs registered by this attempt
  -> exact-remove R1 if still current
  -> propagate failure; R1 is discarded

later request
  -> MapRegionService creates R2
```

## Verification

Use deterministic task gates for blocked decode/registration tests. Verify
staged data is not applied after a decode failure, cache absence versus cache
failure, valid NPC construction continuation, fatal registration failure,
cancellation cleanup, exact replacement, discarded scheduling rejection,
canonical identity, stale viewport rebinding, ready-only worker ticks,
fail-closed collision, and existing scheduler coalescing. Run focused tests,
strict OpenSpec validation, build/diff checks, and broader validation when
resources permit.

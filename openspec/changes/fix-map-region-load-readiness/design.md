## Context

`MapUpdateService` submits visible regions to the existing asynchronous `MapRegionLoadScheduler`. The scheduler deduplicates requests using `IMapRegion.IsLoaded` plus its in-flight set, while `MapRegionLoader` currently calls `region.Load()` before the asynchronous and synchronous population steps finish. `MapRegionService.GetClippingFlag` then reads the region collision array without considering that readiness boundary.

See `proposal.md` for the observed regression and `specs/map-region-load-readiness/spec.md` for the behavior contract.

## Goals / Non-Goals

**Goals:**

- Make the region's existing loaded state the commit point for complete population.
- Fail closed for movement collision queries against regions that are not ready.
- Preserve the existing scheduler's one-reader, in-flight deduplication, and failure logging.
- Allow a later request to retry a region whose load did not commit successfully.
- Add deterministic tests for the loader/readiness boundary and scheduler retry behavior.

**Non-Goals:**

- Do not alter pathfinding algorithms, movement-step geometry, collision flag definitions, cache decoding, or client packets.
- Do not add a second queue, retry worker, persistence mechanism, or region-state store.
- Do not change the intended passability of floor decorations or the collision behavior of custom objects.
- Do not redesign dynamic-region loading or object placement.

## Decisions

### 1. Keep `MapRegion` as the readiness owner

`MapRegion.IsLoaded` remains the single authoritative readiness state. The loader will invoke the existing `Load()` transition only after all population operations have completed successfully. No other component will publish readiness, and the scheduler will continue to use this state to decide whether a completed region needs another load.

An alternative is to add a separate readiness registry or a second loading state machine. That would create another owner for a state already represented by `IMapRegion.IsLoaded`, so it is rejected.

### 2. Fail closed at the collision-query boundary

`MapRegionService.GetClippingFlag` will check the resolved region's readiness before returning its collision array. A region that is not ready will return the existing blocking terrain flag, preventing both pathfinding and per-tick movement validation from entering or traversing a partially populated region. Ready regions will continue returning their stored flags unchanged.

Only the query boundary is gated. Population continues to use the existing direct region collision writers, so loading can construct the collision grid without routing its writes through a new mechanism.

Returning a walkable value during loading is rejected because it recreates the reported failure. Blocking only the affected not-ready region is preferred over changing the pathfinder or making the synchronous game tick wait for asynchronous loading.

### 3. Reuse scheduler admission for retry

`MapRegionLoadScheduler` remains the sole asynchronous loading owner. Its existing `finally` block will continue to release the in-flight marker after success, failure, or cancellation. With readiness published only at the end, failed and canceled loads leave `IsLoaded == false`; a later `RequestLoad` is therefore admitted by the existing scheduler without an additional retry loop.

Duplicate requests during the active operation remain suppressed by `_scheduled`, and requests for a committed region remain suppressed by `IsLoaded`.

### 4. Test the semantic gates, not timing

Regression tests will coordinate on explicit load-start, population-progress, completion, and failure gates. They will not use sleeps or read-count assumptions. Coverage will verify that readiness is false during population, collision queries fail closed, success publishes readiness after the final population step, and a failed attempt can be requested again.

## Risks / Trade-offs

- [Risk] Movement requests targeting a region still loading will fail closed rather than wait for it. → This preserves the synchronous movement contract and prevents traversal through unknown geometry; the client can issue a later movement request after the region is ready.
- [Risk] A load failure may leave population work performed before the failure in the in-memory region. → Readiness remains false and blocks movement; implementation must preserve the current region ownership and verify that a later admitted attempt is not suppressed. Any broader rollback or transactional population mechanism is outside this change unless tests demonstrate it is required for retry correctness.
- [Risk] The blocking flag can affect callers that use `GetClippingFlag` for non-movement queries. → Limit the change to the existing world collision service boundary and cover the public collision-query contract; pathfinding and movement remain unchanged.

## Migration Plan

1. Apply the loader ordering and collision-query readiness gate.
2. Run focused GameWorld readiness/scheduler tests, existing pathfinder tests, strict OpenSpec validation, and a build.
3. Rebuild and restart the affected GameWorld service, then manually verify a static wall/solid object and a custom object in the client.
4. Roll back by reverting the focused readiness change if the runtime test shows an unrelated caller depends on walkable flags from an unready region; do not revert the completed static-coordinate fix as part of this change.

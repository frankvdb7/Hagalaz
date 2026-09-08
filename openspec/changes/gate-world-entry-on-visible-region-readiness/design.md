## Context

`Character.OnRegistered()` currently calls the synchronous `UpdateMap` startup path. `MapUpdateService` rebuilds the viewport, sends the map packet, queues visible regions through `IMapRegionLoadScheduler`, and immediately sends region-part updates. The existing readiness change makes `IMapRegion.IsLoaded` the commit point after full region population, but it does not provide a way for world entry to wait for that commit.

## Design

### 1. Add an awaitable operation to the existing scheduler implementation

Keep `IMapRegionLoadScheduler.RequestLoad` unchanged. Add an internal operation on the concrete `MapRegionLoadScheduler` for the world-entry consumer to submit a finite set of regions and await their existing in-flight operations. The scheduler remains the only reader and loader owner.

Each scheduled region receives a completion signal owned by the scheduler. The worker completes it successfully only when `region.IsLoaded` is true after `IMapRegionLoader.LoadAsync` returns. Loader exceptions and a completed load that did not publish readiness complete the signal as failure. The entry caller observes the first failure and does not submit another request. Fire-and-forget callers do not receive faulted unobserved tasks.

### 2. Gate the existing world-sign-in consumer

Before `WorldSignInCommandConsumer` calls `character.OnRegistered()`, it will rebuild `character.Viewport` and await the scheduler for `character.Viewport.VisibleRegions`. This uses the same visible-region calculation as the startup map update, so the set being awaited matches the set subsequently rendered. `OnRegistered()` then sends the existing startup packets only after the barrier succeeds.

The consumer will use the existing `IGameSessionConnectionTerminator` for terminal failure. Its current cleanup remains authoritative for character removal, session removal, and world sign-out; the connection abort occurs after cleanup in a `finally` path so a failed entry cannot leave an active client connection behind.

### 3. Preserve public script-facing contracts

No new member is added to `IMapRegionLoadScheduler`, `IMapUpdateService`, `ICharacter`, or the script-facing map APIs. Later map updates continue to call the existing synchronous `UpdateMap` and enqueue background loads as before.

## Invariants

- A successful world-entry barrier returns only when every initial visible region has `IsLoaded == true`.
- No startup registration or map packet occurs before the barrier returns.
- A failed barrier performs one cleanup path and one connection abort; it never retries the failed region.
- The existing scheduler has one worker and one in-flight admission per region.
- No script-facing public API gains an asynchronous counterpart.

## Failure Handling

- If the loader throws, the scheduler logs the failure and reports it to the waiting entry operation.
- If the loader returns while `IsLoaded` is still false, the scheduler reports a readiness failure rather than treating the region as complete.
- If the entry cancellation token is canceled, the wait stops and the consumer follows its existing failure cleanup and disconnect path.
- Normal background requests remain eligible for their existing later request semantics; the world-entry barrier itself performs no retry.

## Testing Strategy

- Gate scheduler tests on explicit load-start and release signals, proving the awaitable covers all regions and does not complete early.
- Add failure coverage for a loader that returns without readiness, proving no second attempt is made by the entry path.
- Add world-sign-in consumer tests proving registration and successful world/contact publication occur only after readiness, while failure cleans up and aborts the connection.
- Retain existing scheduler deduplication and normal `MapUpdateService` synchronous-contract tests.

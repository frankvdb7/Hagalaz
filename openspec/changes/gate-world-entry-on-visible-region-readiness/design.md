## Context

`Character.OnRegistered()` currently calls the synchronous `UpdateMap` startup path. `MapUpdateService` rebuilds the viewport, sends the map packet, queues visible regions through `IMapRegionLoadScheduler`, and immediately sends region-part updates. The existing readiness change makes `IMapRegion.State == Ready` the commit point after full region population, but it does not provide a way for world entry to wait for that commit.

## Design

### 1. Add an awaitable operation to the existing scheduler implementation

Keep `IMapRegionLoadScheduler.RequestLoad` unchanged. Add an internal operation on the concrete `MapRegionLoadScheduler` for the world-entry consumer to submit a finite set of regions and await their existing in-flight operations. The scheduler remains the only reader and loader owner.

The scheduler keeps one completion signal per in-flight region in a normal
dictionary protected by its existing lock. The worker completes it only when
`region.State == Ready` after `IMapRegionLoader.LoadAsync` returns. Loader
exceptions fault the shared task and cancellation cancels it; a completed load
that did not publish readiness faults it as well. `EnsureLoadedAsync` awaits
the distinct region tasks together, while its caller cancellation only stops
that caller's wait. Shutdown cancels unresolved waiters before the worker
exits, so no entry waiter can remain pending indefinitely. Fire-and-forget
callers do not receive or observe these internal completion tasks.

### 2. Gate the existing world-sign-in consumer

Before `WorldSignInCommandConsumer` calls `character.OnRegistered()`, it will
rebuild `character.Viewport`, explicitly refresh retained region references,
and await the scheduler for `character.Viewport.VisibleRegions`. This is
required to know the initial canonical visible set before registration.
`MapUpdateService` reuses that prepared viewport when `Character.OnRegistered()`
sends the startup map and refreshes it once at the map-update boundary, so the
set being awaited matches the set subsequently rendered without a second
rebuild.

The consumer will use the existing `IGameSessionConnectionTerminator` for
terminal failure. It logs the initialization failure and aborts the session;
`ConnectionHub` and `AuthenticationService.SignOutAsync` remain the sole
owners of character removal, persistence, session release, detachment, and
world sign-out. The consumer does not duplicate that cleanup.

### 3. Preserve public script-facing contracts

No asynchronous member is added to `IMapRegionLoadScheduler`,
`IMapUpdateService`, `ICharacter`, or the script-facing map APIs. The viewport
has one synchronous canonical-refresh operation; later map updates continue
to call the existing synchronous `UpdateMap` and enqueue background loads as
before.

## Invariants

- A successful world-entry barrier returns only when every initial visible region has `State == Ready`.
- No startup registration or map packet occurs before the barrier returns.
- A failed barrier performs one connection abort and delegates cleanup to the
  normal disconnect/sign-out owner; it never retries the failed region.
- The existing scheduler has one worker and one in-flight admission per region.
- No script-facing public API gains an asynchronous counterpart.

## Failure Handling

- If the loader throws, the scheduler logs the failure and reports it to the waiting entry operation.
- If the loader returns while `State` is still `Initializing`, the scheduler reports a readiness failure rather than treating the region as complete.
- If the entry cancellation token is canceled, the wait stops and the consumer follows its existing failure cleanup and disconnect path.
- Normal background requests remain eligible for their existing later request semantics; the world-entry barrier itself performs no retry.

## Testing Strategy

- Gate scheduler tests on explicit load-start and release signals, proving the awaitable covers all regions and does not complete early.
- Add failure coverage for a loader that returns without readiness, proving no second attempt is made by the entry path.
- Add world-sign-in consumer tests proving registration and successful world/contact publication occur only after readiness, while failure cleans up and aborts the connection.
- Retain existing scheduler deduplication and normal `MapUpdateService` synchronous-contract tests.
- Verify that startup map delivery reuses the prebuilt entry viewport without a
  second rebuild.

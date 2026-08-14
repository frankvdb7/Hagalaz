## Context

The GameWorld worker owns the scheduler and the four region phases. The tick budget is a performance signal, not permission to abandon work. Region and creature phases perform in-process state mutation and packet creation; they do not need asynchronous return types.

The one real asynchronous prerequisite for client rendering is the global character-store read lock. Character rendering previously acquired that lock and enumerated every character once per character, producing O(N²) work and repeated dictionary allocations.

Viewport map loading is asynchronous, but the synchronous render boundary only needs to submit a request. `MapRegionLoadScheduler.RequestLoad` deduplicates loaded and pending/in-flight regions and writes to its own request channel with `TryWrite`, so the game tick never waits for asynchronous queue admission. A single hosted scheduler consumer owns the asynchronous loader call and creates a fresh service scope for each region. It releases the in-flight marker when the load operation completes; the region's `IsLoaded` state remains authoritative for whether a later request is admitted. `MapUpdateService` can therefore request loading and send `SendFullPartUpdates` immediately without creating a detached task or blocking the character's synchronous render phase.

## Decisions

1. **Keep `BackgroundService` as the lifecycle owner.** `ExecuteAsync` retains the worker task. `StopAsync` passes the host token to the base implementation. When the host token expires before a synchronous owned tick completes, the service checks `ExecuteTask` and throws a timeout instead of claiming successful shutdown. A normal stop still awaits the worker task.

2. **Make the game tick synchronous after its prerequisites.** `RunMajorTickAsync` snapshots active regions, obtains one character view with the worker token, and then invokes the four region phases directly. No stopping-token checks occur inside the started synchronous pipeline; cancellation is observed before the next tick or while waiting for the outer snapshot. This gives the full tick one ownership boundary and prevents half-applied phase sequences.

3. **Use synchronous region and creature contracts.** `IMapRegion` exposes `void` methods for the four phases. `ICreature` exposes synchronous prepare/update/reset methods, and concrete NPC/character update methods no longer return completed tasks. The character-specific client update receives the already-captured character view.

4. **Capture global character state once.** `ICharacterStore.GetSnapshotAsync` acquires the existing reader lock once and returns a read-only dictionary keyed by character index. `GameWorkerService` passes that same view to every region, and `CharacterRenderInformation.Update` only formats and sends messages synchronously.

5. **Keep viewport state separate from map-update orchestration.** `Viewport` owns visible-region state, bounds, and visibility calculations. The stateless `MapUpdateService` performs the rebuild, map packet construction/session send, `RequestLoad` calls, and immediate full region-part updates. `ICharacter.UpdateMap` remains a synchronous facade to that service; `IViewport` does not expose map-update orchestration. Startup and world-map callers therefore do not create an async state machine or block on a load task, and no detached map-load waiter is created inside the worker-owned synchronous region phase.

6. **Give region loading one asynchronous owner.** `MapRegionLoadScheduler` owns a deduplicated unbounded request channel and consumes it serially. It accepts requests synchronously with `TryWrite`, removes the request marker after each load, creates an async service scope per load, and logs loader failures without making the game tick own the asynchronous operation. It marks itself stopping before completing the channel, so requests racing with intentional shutdown are ignored, while a failed `TryWrite` during normal operation still propagates as an unexpected scheduling failure. Startup registers the scheduler before `GameWorkerService`, so hosted-service LIFO shutdown lets the worker finish its already-started tick before the scheduler closes its writer. The obsolete `IBackgroundTaskQueue`, `DefaultBackgroundTaskQueue`, and `QueuedHostedService` bridge are removed because repository search found no other producers.

7. **Preserve fault classification.** Delay cancellation and the worker token are handled separately from tick execution. An `OperationCanceledException` is treated as routine shutdown only when it carries the worker token; unrelated cancellation exceptions are logged as tick failures. The pipeline remains directly awaited at its only asynchronous boundary.

## Alternatives Rejected

- Passing cancellation tokens through every region and creature callback: this preserves artificial asynchronous contracts and cannot interrupt non-cancellable synchronous model work safely.
- Keeping `StopAsync(CancellationToken.None)`: it defeats the host shutdown bound. The host token is used, and an expired token is surfaced as an unsuccessful stop when owned synchronous work remains.
- Building a persistent immutable character snapshot store: it could remove the one per-tick read, but would expand the change into character registration/removal publication semantics. One snapshot per tick removes the O(N²) rendering cost without that larger lifecycle change.
- Putting map-update orchestration in `Viewport`: visible-region ownership does not require protocol I/O or background queue ownership. A focused stateless map-update service keeps that boundary explicit without coupling it to a generic coordinator.
- Blocking the synchronous game tick on bounded generic queue admission: it couples the model boundary to an unrelated async producer contract. The dedicated scheduler accepts at most one pending or in-flight request per region synchronously and owns the asynchronous load lifecycle.

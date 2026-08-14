## Context

The GameWorld worker owns the scheduler and the four region phases. The tick budget is a performance signal, not permission to abandon work. Region and creature phases perform in-process state mutation and packet creation; they do not need asynchronous return types.

The one real asynchronous prerequisite for client rendering is the global character-store read lock. Character rendering previously acquired that lock and enumerated every character once per character, producing O(N²) work and repeated dictionary allocations.

Viewport map loading is also asynchronous, but `MapRegionService.LoadRegionAsync` only submits a work item to the background queue; it does not wait for the region data load. The viewport component can therefore initiate that submission and send `SendFullPartUpdates` immediately without putting an async task into the character's synchronous render phase.

## Decisions

1. **Keep `BackgroundService` as the lifecycle owner.** `ExecuteAsync` retains the worker task. `StopAsync` passes the host token to the base implementation. When the host token expires before a synchronous owned tick completes, the service checks `ExecuteTask` and throws a timeout instead of claiming successful shutdown. A normal stop still awaits the worker task.

2. **Make the game tick synchronous after its prerequisites.** `RunMajorTickAsync` snapshots active regions, obtains one character view with the worker token, and then invokes the four region phases directly. No stopping-token checks occur inside the started synchronous pipeline; cancellation is observed before the next tick or while waiting for the outer snapshot. This gives the full tick one ownership boundary and prevents half-applied phase sequences.

3. **Use synchronous region and creature contracts.** `IMapRegion` exposes `void` methods for the four phases. `ICreature` exposes synchronous prepare/update/reset methods, and concrete NPC/character update methods no longer return completed tasks. The character-specific client update receives the already-captured character view.

4. **Capture global character state once.** `ICharacterStore.GetSnapshotAsync` acquires the existing reader lock once and returns a read-only dictionary keyed by character index. `GameWorkerService` passes that same view to every region, and `CharacterRenderInformation.Update` only formats and sends messages synchronously.

5. **Let the viewport component own map-update orchestration.** `Viewport.UpdateMap` performs the rebuild and map packet send, initiates each `LoadRegionAsync` submission with fire-and-forget observation, and immediately sends the corresponding full region-part updates. `Viewport.UpdateMapAsync` remains for initial/external callers that need the existing asynchronous queue-submission boundary. `Character` delegates both paths and no longer schedules viewport work through its creature task queue. No map queue await occurs inside the worker-owned synchronous region phase.

6. **Preserve fault classification.** Delay cancellation and the worker token are handled separately from tick execution. An `OperationCanceledException` is treated as routine shutdown only when it carries the worker token; unrelated cancellation exceptions are logged as tick failures. The pipeline remains directly awaited at its only asynchronous boundary.

## Alternatives Rejected

- Passing cancellation tokens through every region and creature callback: this preserves artificial asynchronous contracts and cannot interrupt non-cancellable synchronous model work safely.
- Keeping `StopAsync(CancellationToken.None)`: it defeats the host shutdown bound. The host token is used, and an expired token is surfaced as an unsuccessful stop when owned synchronous work remains.
- Building a persistent immutable character snapshot store: it could remove the one per-tick read, but would expand the change into character registration/removal publication semantics. One snapshot per tick removes the O(N²) rendering cost without that larger lifecycle change.
- Adding a separate map-loading coordinator: the existing `Viewport` component already owns visible-region state and can provide the synchronous orchestration boundary without routing render work through the creature task scheduler.

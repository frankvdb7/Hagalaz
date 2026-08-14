## 1. Worker lifecycle and serialized execution

- [x] 1.1 Convert `GameWorkerService` to the existing `BackgroundService` lifecycle and remove the discarded `Task.Run`/local cancellation source.
- [x] 1.2 Await the complete four-phase region pipeline directly and preserve phase ordering and region snapshot semantics.
- [x] 1.3 Measure completed tick duration, log budget overruns, and distinguish stopping cancellation from unexpected tick exceptions.
- [x] 1.4 Use the host shutdown token and surface an expired token as a timeout when the owned synchronous tick is still alive.

## 2. Synchronous game tick core

- [x] 2.1 Remove artificial `Task` and cancellation-token plumbing from `IMapRegion` and creature phase contracts.
- [x] 2.2 Make NPC and character client rendering synchronous after the worker obtains its prerequisites.
- [x] 2.3 Add one cancellable character snapshot boundary per tick and pass the same snapshot to every region client update.
- [x] 2.4 Move viewport region loading out of the synchronous character render update and onto the existing creature task scheduler.
- [x] 2.5 Move viewport/map-update orchestration into `MapUpdateService` so full region-part updates occur in the current synchronous render phase without a character-owned `RsAsyncTask`, while keeping `Viewport` state-only.
- [x] 2.6 Remove `UpdateMapAsync` from the character and viewport model contracts and migrate startup/world-map callers to synchronous `UpdateMap`.
- [x] 2.7 Replace `LoadRegionAsync`/`.Forget()` with deduplicated synchronous region-load scheduling that preserves bounded queue backpressure and propagates submission failures.

## 3. Deterministic regression coverage

- [x] 3.1 Add a blocked-major-update regression proving an over-budget tick cannot overlap the next tick.
- [x] 3.2 Add phase-order and adjacent-tick serialization coverage for prepare, update, and reset phases.
- [x] 3.3 Add overrun logging, genuine exception, host cancellation, unrelated cancellation, and shutdown ownership coverage.
- [x] 3.4 Add coverage proving one character snapshot is shared across regions and synchronous render information sends both client messages.

## 4. Validation

- [x] 4.1 Run the focused GameWorld worker, rendering, and ground-item tests to a clean exit.
- [x] 4.2 Run the GameWorld test project, solution build, strict OpenSpec validation, and final diff/status review.

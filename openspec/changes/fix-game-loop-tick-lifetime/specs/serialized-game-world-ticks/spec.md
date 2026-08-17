## ADDED Requirements

### Requirement: Major world ticks are serialized

The GameWorld worker MUST own and complete the major update, client-prepare, client-update, and client-reset phases before it can start another major tick.

#### Scenario: A tick exceeds its budget

- **WHEN** a synchronous region update remains incomplete beyond the configured tick duration
- **THEN** no subsequent tick begins until the original four-phase pipeline completes

#### Scenario: Adjacent ticks complete normally

- **WHEN** one major tick completes and the worker starts the next iteration
- **THEN** the next tick begins only after every phase of the previous tick has completed

### Requirement: Region phase ordering is preserved

The worker MUST execute all region major updates, then all client-prepare updates, then all client updates, then all client-reset updates, without overlapping phases.

#### Scenario: Multiple active regions are present

- **WHEN** a major tick snapshots multiple active regions
- **THEN** each phase runs serially over that same region snapshot in the existing phase order

### Requirement: Client rendering uses one character view per tick

The worker MUST capture the active character view once for a tick and MUST pass that same view to each region's client-update phase.

#### Scenario: Multiple regions render clients

- **WHEN** the client-update phase runs for multiple regions
- **THEN** the regions receive the same character snapshot and no individual character re-enumerates the global character store

### Requirement: Worker shutdown owns in-flight work

The hosted GameWorld worker MUST retain ownership of its execution task and MUST NOT report successful shutdown while a worker-owned synchronous tick is still running.

#### Scenario: Shutdown begins during a region phase

- **WHEN** `StopAsync` is called while a major region phase is blocked
- **THEN** normal shutdown remains incomplete until that phase and the worker task finish

#### Scenario: Host cancellation is requested between ticks

- **WHEN** the worker stopping token is cancelled while the worker is delaying or before a new tick starts
- **THEN** the worker exits cleanly without reporting routine host cancellation as an unexpected major tick failure

#### Scenario: Host shutdown timeout expires during a synchronous phase

- **WHEN** the `StopAsync` host token is cancelled while a worker-owned synchronous phase remains blocked
- **THEN** `StopAsync` fails with a timeout and does not report successful completion while `ExecuteTask` remains alive

#### Scenario: Shutdown is requested after a tick starts

- **WHEN** the worker stopping token is cancelled during the synchronous four-phase pipeline
- **THEN** the worker completes that pipeline and exits before starting another tick

#### Scenario: A map request races with scheduler shutdown

- **WHEN** an already-started synchronous tick requests a region after region-load scheduler shutdown has begun
- **THEN** the request returns without throwing so the owned tick can complete

### Requirement: Viewport map updates stay synchronous at the render boundary

Character map rebuilding and packet construction MUST be synchronous within the render phase. A map-update service MUST orchestrate the viewport rebuild, map packet send, synchronous non-blocking region-load requests, and full region-part updates during that same synchronous operation; the actual region data load MUST be owned by a dedicated asynchronous scheduler. `Viewport` MUST remain responsible only for visible-region state, bounds, and visibility calculations.

The `ICharacter` model contract MUST expose only a synchronous map-update operation. `IViewport` MUST NOT expose map-update orchestration, and callers MUST NOT use sync-over-async waits for map updates. The region-load scheduler MUST accept requests synchronously without blocking the game tick or creating detached tasks, skip loaded or already scheduled regions, deduplicate pending and in-flight requests, and own the asynchronous loader operation. `IMapRegion.IsLoaded` remains the authoritative indication that a region should not be admitted again; this requirement does not define recovery for a loader that marks a region loaded before a later population step fails.

#### Scenario: A character crosses a viewport rebuild boundary

- **WHEN** a character's render update detects that its viewport must be rebuilt
- **THEN** the map-update service asks the viewport to rebuild, sends the map update, synchronously requests each visible region load through the dedicated scheduler, and sends full region-part updates before the current render operation returns, without awaiting region loading, blocking for queue capacity, or scheduling an `RsAsyncTask`

#### Scenario: A region is already loaded or scheduled

- **WHEN** a map update requests a region whose data is loaded or whose load is already admitted/in flight
- **THEN** the loading layer does not enqueue another work item for that region

#### Scenario: A completed load marks a region loaded

- **WHEN** a region load work item completes after the loader has marked the region loaded
- **THEN** a later map update does not enqueue another work item for that region

#### Scenario: Region loading is busy

- **WHEN** a map update requests a region while another region load is running or pending
- **THEN** the request returns without blocking the game tick, and the scheduler retains at most one pending or in-flight request for each region

#### Scenario: The scheduler consumes a region request

- **WHEN** the asynchronous scheduler receives a new region request
- **THEN** it creates the scoped loader operation, awaits the region load on its own consumer, and releases the region's scheduled marker when the operation completes

#### Scenario: The request channel fails during normal operation

- **WHEN** a region request cannot be written while scheduler shutdown has not begun
- **THEN** the synchronous request boundary propagates the scheduling failure instead of silently treating it as accepted

#### Scenario: Startup or world-map code requests a map update

- **WHEN** startup or a world-map script requests a map update
- **THEN** it calls synchronous `UpdateMap` directly without creating an async map-update task or blocking on `.Wait()`

### Requirement: Tick failures and overruns remain observable

The worker MUST log genuine major tick exceptions and MUST report a completed major tick that exceeds its configured duration, without treating an overrun as permission to overlap work.

#### Scenario: A major tick overruns

- **WHEN** the completed major region pipeline takes longer than `TickTimeSpan`
- **THEN** the worker emits an overrun warning and still waits for the pipeline before starting another tick

#### Scenario: A major tick faults

- **WHEN** a region phase throws an unexpected exception
- **THEN** the worker logs the exception as a major tick failure and continues with a later tick only after the faulted pipeline has ended

#### Scenario: Tick work throws an unrelated cancellation exception during shutdown

- **WHEN** a phase throws an `OperationCanceledException` carrying a token other than the worker stopping token while shutdown is requested
- **THEN** the worker logs it as a major tick failure instead of silently classifying it as routine shutdown

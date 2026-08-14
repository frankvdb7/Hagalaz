## Why

`GameWorkerService` must not abandon a running world tick when its budget is exceeded or when host shutdown begins. The original timeout allowed overlapping ticks, while passing asynchronous wrappers and cancellation tokens through naturally synchronous creature work made the ownership boundary harder to reason about.

## What Changes

- Keep the complete major game pipeline owned by `BackgroundService` and await it before another iteration can begin.
- Keep the configured tick duration as an observability budget only; it never abandons world work.
- Make region and creature simulation/render phases synchronous, removing artificial `Task` and cancellation-token plumbing from the game model.
- Capture one read-only character snapshot per tick and pass it through synchronous character rendering.
- Move viewport/map-update orchestration into the viewport component. A synchronous rebuild must send the map packet, initiate asynchronous region loading, and send full region-part updates before the render phase returns; the actual region loading remains owned by the background loader.
- Use the host shutdown token with the normal `BackgroundService` wait. If that token expires while a synchronous tick is still owned by the worker, report a timeout rather than silently reporting a successful stop.
- Preserve explicit handling for worker cancellation, genuine tick failures, and budget overruns.

## Capabilities

### New Capabilities

- `serialized-game-world-ticks`: Serialized major world updates with an owned hosted-service lifetime.

### Modified Capabilities

None.

## Impact

The change is limited to the existing worker, the `IMapRegion` and creature tick contracts, character snapshot/rendering state, viewport map-loading scheduling, focused GameWorld tests, and the associated OpenSpec record. It adds no dependencies, persistence changes, or second worker/coordinator.

## Acceptance Criteria

- At most one complete major world tick executes at a time, including when it exceeds its configured budget.
- The four phases remain ordered and a started tick runs its synchronous pipeline to completion before shutdown or the next iteration is observed.
- Each tick obtains one character snapshot and all region client-update calls receive that same snapshot.
- Character rendering does not enumerate the global character store once per character.
- Viewport rebuild work is not deferred to a later creature-task tick: the viewport component performs the rebuild, map packet send, region-load scheduling, and full-part updates during the current synchronous render phase.
- `StopAsync` never reports successful completion while `ExecuteTask` is still alive; an expired host shutdown token is surfaced as a timeout when a synchronous tick cannot yet reach its boundary.
- Host cancellation is not logged as an unexpected tick failure, while genuine faults and unrelated cancellation exceptions remain observable.
- Tick overruns remain observable through logging.

## Stop Conditions

Do not add parallel region execution, a generic game-loop coordinator, a second queue/lock, cancellation parameters to unrelated creature APIs, or unrelated lifecycle changes.

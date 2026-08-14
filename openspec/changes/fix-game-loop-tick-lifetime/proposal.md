## Why

`GameWorkerService` times out its wait for a major world tick without cancelling or awaiting the underlying region work. A slow tick can therefore overlap the next tick, and shutdown can report completion while the discarded worker task is still mutating world state.

## What Changes

- Run the game loop through the existing `BackgroundService` lifecycle so the worker task is retained and awaited during shutdown.
- Await the complete major region pipeline before beginning another iteration; the configured tick duration remains an observability budget, not a concurrency timeout.
- Preserve the existing region and client phase order, and distinguish host cancellation, tick faults, and budget overruns in logging.
- Add deterministic GameWorld worker tests covering serial ticks, phase ordering, overrun reporting, exceptions, cancellation, and shutdown ownership.

## Capabilities

### New Capabilities

- `serialized-game-world-ticks`: Serialized major world updates with an owned hosted-service lifetime.

### Modified Capabilities

None.

## Impact

The change is limited to `Hagalaz.Services.GameWorld/Services/GameWorkerService.cs` and focused tests in `Hagalaz.Services.GameWorld.Tests`. It removes the service-local cancellation/task plumbing and adds no dependencies, persistence changes, or new worker framework.

## Acceptance Criteria

- At most one major world tick executes at a time, including when it exceeds its configured budget.
- The four existing phases remain ordered and serialized across adjacent ticks.
- `StopAsync` does not complete while worker-owned region work is still running.
- Host cancellation is not logged as an unexpected tick failure; genuine faults remain observable.
- Tick overruns remain observable through logging.

## Stop Conditions

Do not add parallel region execution, a generic game-loop coordinator, a second queue/lock, cooperative cancellation parameters to all region APIs, or unrelated lifecycle changes.

## Why

`Hagalaz.Services.GameWorld.Tests` starts the real `GameWorkerService` with a zero-duration tick delay for ordinary behavior tests. That makes the tests depend on scheduling and can leave a hot background loop, retained substitute calls, and test-owned log entries alive when a test exits unexpectedly. The change will make tick behavior deterministic and make hosted-service ownership explicit so a failed test cannot strand work.

## What Changes

- Extract an internal, single-iteration game-worker tick seam for ordinary unit tests.
- Move worker behavior tests that only need one or two ticks off the unbounded zero-delay hosted loop.
- Keep a small set of hosted-lifecycle tests, use a long real delay for the no-tick cancellation case, and guarantee stop-and-await cleanup on every path.
- Bound test logging to the entries needed by assertions; do not use a bounded collection to hide a still-running worker.
- Measure the pathfinder fixtures independently and replace high-frequency NSubstitute clipping mocks with a focused fake only if call retention is shown to be material.
- Add regression coverage for cancellation, exception termination, tick completion, and worker shutdown ownership.

## Capabilities

### New Capabilities

- `gameworld-test-lifecycle`: GameWorld worker tests execute controlled ticks and deterministically release all owned background work and test resources.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Services/GameWorkerService.cs` gains an internal testable tick boundary while preserving the hosted-service loop and production timing.
- `Hagalaz.Services.GameWorld.Tests/GameWorkerServiceTests.cs` is simplified around deterministic tick execution and exception-safe lifecycle tests.
- Pathfinder test fixtures may receive a local collision fake if runtime evidence confirms NSubstitute call history is a retained-memory contributor.
- No public API, production parallelism, CI memory limit, retry, or garbage-collection behavior changes.

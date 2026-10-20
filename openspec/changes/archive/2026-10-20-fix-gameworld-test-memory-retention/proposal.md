## Why

GameWorld validation also evaluates project-local generated `artifacts` output
through the SDK's implicit globs. The generated output contains nested copies
of plugin and build trees, so each evaluation expands the input graph and
intermediate file list recursively until the build process consumes excessive
memory. The change will keep the existing deterministic worker-test ownership
and exclude that generated tree from implicit project inputs.

## What Changes

- Extract an internal, single-iteration game-worker tick seam for ordinary unit tests.
- Move worker behavior tests that only need one or two ticks off the unbounded zero-delay hosted loop.
- Keep a small set of hosted-lifecycle tests, use a long real delay for the no-tick cancellation case, and guarantee stop-and-await cleanup on every path.
- Bound test logging to the entries needed by assertions; do not use a bounded collection to hide a still-running worker.
- Measure the pathfinder fixtures independently and replace high-frequency NSubstitute clipping mocks with a focused fake only if call retention is shown to be material.
- Exclude project-local generated `artifacts` output from implicit MSBuild
  items so recursive build output cannot become project input.
- Add regression coverage for cancellation, exception termination, tick completion, and worker shutdown ownership.

## Capabilities

### New Capabilities

- `gameworld-test-lifecycle`: GameWorld worker tests execute controlled ticks and deterministically release all owned background work and test resources.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Services/GameWorkerService.cs` gains an internal testable tick boundary while preserving the hosted-service loop and production timing.
- `Hagalaz.Services.GameWorld.Tests/GameWorkerServiceTests.cs` is simplified around deterministic tick execution and exception-safe lifecycle tests.
- `Directory.Build.props` excludes project-local generated `artifacts` output
  from implicit MSBuild items.
- Pathfinder test fixtures may receive a local collision fake if runtime evidence confirms NSubstitute call history is a retained-memory contributor.
- No public API, production parallelism, CI memory limit, retry, or garbage-collection behavior changes.

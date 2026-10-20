## Why

`DumbPathFinder` returns its initially successful `Path` when the safety step cap is reached before the requested destination, allowing a partial path to be consumed as a successful route. `ProjectilePathFinder` has the same result risk because its ray tracing does not enforce the shared safety cap. Both finders must report whether the requested operation actually reached its target.

## What Changes

- Mark capped `DumbPathFinder` routes unsuccessful when the target has not been reached, while preserving whether traversal made progress.
- Apply the existing `PathFinderBase.QueueSize` cap to projectile ray tracing and return an unsuccessful result when it is exhausted before the target.
- Add test-first MSTest coverage for normal success, cap exhaustion, final permitted step, collision failure, returned points, step counts, and derived destination flags.

### Non-goals

- Do not change either pathfinding algorithm, collision masks, ray geometry, or the numerical cap.
- Do not change `Path`, `IPath`, movement execution, consumers, or introduce a new result type.
- Do not duplicate or address directional traversal defects tracked by other issues.

### Acceptance Criteria

- Neither `DumbPathFinder` nor `ProjectilePathFinder` reports `Successful == true` after its cap is exhausted without reaching the requested target.
- A target reached on the final permitted traversal remains successful and has the existing step/result semantics.
- Normal short paths and pre-cap collision failures retain their current behavior.
- Cap results have deterministic `MovedNear`, `Steps`, point-count, `ReachedDestination`, and `MovedNearDestination` behavior covered by focused tests.
- Existing consumers continue receiving the existing `IPath` contract without a new architecture.

### Stop Conditions

Pause and record follow-up work if satisfying these criteria requires changing `Path` or `IPath`, movement/consumer behavior, collision writers, or either finder’s traversal geometry.

## Capabilities

### New Capabilities

- `pathfinder-cap-result-semantics`: Report coherent unsuccessful results when simple or projectile traversal exhausts its safety cap before reaching the target.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Logic/Pathfinding/DumbPathFinder.cs`
- `Hagalaz.Services.GameWorld/Logic/Pathfinding/ProjectilePathfinder.cs`
- `Hagalaz.Services.GameWorld.Tests/DumbPathfinderTests.cs`
- `Hagalaz.Services.GameWorld.Tests/ProjectilePathfinderTests.cs`
- Reuses `PathFinderBase.QueueSize`, `Path`, `IMapRegionService`, and the existing MSTest/NSubstitute fixtures; no API, dependency, persistence, deployment, or migration changes.

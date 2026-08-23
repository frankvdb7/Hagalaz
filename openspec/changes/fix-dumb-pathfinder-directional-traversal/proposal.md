## Why

`DumbPathFinder` has localized directional traversal defects: its size-two southwest branch validates southwest geometry but advances northwest, and its size-one northeast branch applies the northwest diagonal collision mask. The current size-one southwest branch already advances to the correct coordinate, but lacks targeted coverage to preserve that behavior. These inconsistencies can create incorrect simple paths and are not detected by the broad empty-diagonal tests.

## What Changes

- Correct the size-two southwest coordinate update and the size-one northeast diagonal mask in the existing `DumbPathFinder`.
- Add test-first, direction-specific MSTest regressions for size-one traversal and focused size-two footprint checks, while retaining characterization coverage for the existing variable-size southwest behavior.
- Validate direction and mask semantics against the original GameClient routefinders.

### Non-goals

- Do not change collision flag definitions, `SmartPathFinder`, `PathFinderBase`, movement execution, route representation, or client protocol behavior.
- Do not introduce a pathfinding abstraction, replacement algorithm, dependency, or public API.
- Do not correct any directional branch beyond the issue's confirmed defects without recording a separate follow-up.

### Acceptance Criteria

- Size-one southwest resolves to `(x - 1, y - 1)` and remains protected by an explicit regression.
- Size-two southwest resolves to `(x - 1, y - 1)` after its southwest footprint checks pass.
- Size-one northeast blocks on `TraversableNorthEastBlocked`, while a northwest-only diagonal blocker does not block it.
- All eight size-one directions have explicit destination-coordinate, matching-mask, and opposite-mask regressions.
- Size-two directional movement is covered on empty collision and targeted newly occupied footprint blockers, including southwest.
- Existing size-three-or-larger southwest behavior remains characterized as `(x - 1, y - 1)`.

### Stop Conditions

Pause and record follow-up work if satisfying these criteria requires changing collision flag definitions, `SmartPathFinder`, route reconstruction, movement execution, or a pathfinding implementation other than `DumbPathFinder`.

## Capabilities

### New Capabilities

- `dumb-pathfinder-directional-traversal`: Correct and regression-test direction-specific simple-path traversal geometry and collision masks.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Logic/Pathfinding/DumbPathFinder.cs`
- `Hagalaz.Services.GameWorld.Tests/DumbPathfinderTests.cs`
- Reuses `DumbPathFinder`, `CollisionFlag`, `IMapRegionService`, and the existing MSTest/NSubstitute fixture; no API, dependency, persistence, deployment, or migration changes.

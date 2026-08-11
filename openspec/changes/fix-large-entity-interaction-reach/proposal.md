## Why

Large creatures can be reported as already able to interact with decorations and doors when only their X coordinate overlaps the target, because the shared reach guards do not actually bound the Y coordinate. Two large-entity collision lookups also pass an X value as the map Y coordinate, letting unrelated collision tiles affect interaction reach.

## What Changes

- Correct the existing large-entity overlap guards in `PathfinderBase.CanDecorationInteract` and `PathfinderBase.CanDoorInteract` to test both target coordinates against the mover footprint.
- Correct the confirmed X/Y collision lookup defects in the affected decoration and door rotation-0 branches.
- Add focused, test-first MSTest coverage for size-2 and size-3 interaction reach, including footprint boundaries, rotations, blocking versus unrelated collision, and size-1 characterization.

### Non-goals

- Do not change path traversal, path compression, combat-specific targeting, collision flag definitions, or size-1 interaction behavior.
- Do not add a new interaction/pathfinding framework, helper abstraction, dependency, or public API.
- Do not copy the malformed decompiled-client overlap guard; use direct two-dimensional footprint geometry while using coherent client branches only to validate concrete collision coordinates.

### Acceptance Criteria

- Both large-entity methods only treat a target as overlapping when it lies within the mover footprint on both axes.
- The affected door lookup reads `(toAbsX, currentAbsY)` and the affected decoration lookup reads the west-side tile using its actual Y coordinate.
- Sizes 2 and 3 are covered across the supported door and decoration shapes/rotations, with distinct X/Y values, actual-side blocking, and unrelated-collision cases.
- Size-1 behavior is characterized and remains unchanged.

### Stop Conditions

Pause and record follow-up work if the correction requires changing collision flag definitions, movement/pathfinding algorithms, combat consumers, persisted data, or any behavior beyond interaction reach.

## Capabilities

### New Capabilities

- `large-entity-interaction-reach`: Defines correct footprint and collision-side evaluation when a multi-tile entity checks object or door interaction reach.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Logic/Pathfinding/PathfinderBase.cs`
- A focused GameWorld pathfinder interaction MSTest suite
- Reuses the existing `PathfinderBase` reach methods and `IMapRegionService` clipping lookups; no API, dependency, or persistence changes.

## Why

`PathFinderBase.CheckStep` can accept blocked movement for size-2 and size-3+ creatures because of incorrect size-2 branches, skipped variable-size edge loops, and a fail-open fallback for unsupported offsets. `Movement.Tick` passes compressed waypoint deltas to this unit-step validator, so a collision introduced after path calculation can be skipped during actual movement.

## What Changes

- Correct all size-2 directional footprint checks, including southeast and northwest.
- Execute interior edge validation for size-3+ cardinal movement using the client routefinder bounds.
- Make unsupported and zero offsets fail closed, and make distance-based checks compose unit-step validation.
- Make `Movement.Tick` validate each one-tile step it applies toward a compressed waypoint, stopping before blocked movement and retaining the waypoint.
- Add focused MSTest regressions for size 1, size 2, variable-size geometry, unsupported offsets, distance checks, and long waypoint movement.

### Non-goals

- Do not replace or redesign the existing pathfinding algorithms.
- Do not add a new collision store, movement queue, worker, or abstraction framework.
- Do not change unrelated movement, teleport, region, or path compression behavior.

### Acceptance Criteria

- All eight size-2 directions validate the newly occupied footprint.
- Size-3 and size-4 cardinal checks inspect corner and interior edge tiles.
- Unsupported or zero offsets cannot return collision-free through fall-through behavior.
- Compressed waypoint movement revalidates every applied unit step.
- Existing size-1 directional behavior remains compatible.
- Focused GameWorld tests and the solution build pass.

### Stop Conditions

Pause and record follow-up work if the fix requires changing pathfinding architecture, collision flag definitions, or unrelated movement consumers.

## Capabilities

### New Capabilities

- `pathfinder-step-validation`: Shared unit-step collision validation for creature footprints and movement revalidation.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Logic/Pathfinding/PathfinderBase.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Movement.cs`
- `Hagalaz.Services.GameWorld.Tests`
- No new dependencies or public API signatures.

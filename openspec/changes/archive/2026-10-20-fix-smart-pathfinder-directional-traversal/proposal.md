## Why

`SmartPathFinder` has six verified direction-specific traversal defects that can apply an incorrect collision mask, inspect a tile outside an incoming footprint, enqueue a tile different from the one it validated, or let a large footprint step beyond the local route graph. These defects can produce illegal routes or prevent valid routes for player and NPC movement.

## What Changes

- Correct the six audited size-1, size-2, and variable-size directional traversal conditions in the existing `SmartPathFinder` BFS.
- Add direction-specific MSTest regressions that distinguish collision masks and coordinates, including large-footprint graph boundaries.
- Preserve the existing queue, path reconstruction, and pathfinding architecture.

### Non-goals

- Do not change collision flag definitions, `DumbPathFinder`, path compression, movement execution, or client protocol behavior.
- Do not introduce a new pathfinding algorithm, abstraction, dependency, or public API.
- Do not repair unrelated traversal branches discovered outside the six audited defects.

### Acceptance Criteria

- Size-1 southeast uses the southeast destination mask.
- Size-2 west validates exactly its two incoming west cells, and size-2 north uses distinct northwest and northeast top-edge masks.
- Variable-size southwest and southeast enqueue the collision-validated adjacent anchors.
- Variable-size positive expansion bounds prevent sizes 3 and 4 from extending beyond the 104x104 local graph.
- Direction-specific tests prove size-1 and size-2 collision behavior, and size-3/size-4 directional and boundary behavior.

### Stop Conditions

Pause and record follow-up work if satisfying these criteria requires a change to collision flag definitions, route reconstruction architecture, movement execution, or a pathfinding implementation other than `SmartPathFinder`.

## Capabilities

### New Capabilities

- `smart-pathfinder-directional-traversal`: Correct collision geometry, queue coordinates, and local-graph bounds for the existing smart pathfinder BFS.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Logic/Pathfinding/SmartPathfinder.cs`
- `Hagalaz.Services.GameWorld.Tests/SmartPathfinderTests.cs`
- Reuses the existing collision flags, `IMapRegionService` clipping lookups, and `SmartPathFinder` BFS with no API, dependency, persistence, or deployment change.

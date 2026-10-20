## Why

PR #380 corrected most of issue #365, but the shared `PathfinderBase.CheckStep` still has two north-east geometry errors. The size-2 branch checks an overlapping tile instead of the newly occupied east edge, and the size-3+ north-east branch applies the top and right edge masks in reverse. Existing tests do not catch the mask swap because they mostly use `FloorBlock`, which belongs to both directional composites.

## What Changes

- Correct the size-2 north-east third footprint coordinate to `(fromX + 2, fromY + 1)`.
- Correct the variable-size north-east top edge to use `CheckSouthVariable` and the right edge to use `CheckWestVariable`.
- Update the client-parity movement row to block the actual size-2 north-east incoming east-edge tile.
- Add directional-bit regressions that distinguish the two variable-size composites without relying on `FloorBlock`.

### Non-goals

- Do not revert or redesign PR #380's unit-step movement validation.
- Do not change size-1 behavior, other directions, collision flag definitions, path compression, or movement ownership.
- Do not add a new collision abstraction, pathfinding algorithm, or runtime workaround.

### Acceptance Criteria

- A size-2 north-east step is rejected when `(fromX + 2, fromY + 1)` is blocked, and does not depend on `(fromX + 1, fromY + 1)`.
- Size-2 client-parity coverage blocks `(fromX + 2, fromY + 1)` for north-east movement.
- A size-3 or size-4 north-east step rejects a top-edge `WallSouthEast` bit through `CheckSouthVariable`.
- A size-3 or size-4 north-east step rejects a right-edge `WallNorthEast` bit through `CheckWestVariable`.
- Existing focused GameWorld tests and the solution build remain passing.

### Stop Conditions

Pause and record follow-up work if the correction requires changing collision flag definitions, movement consumers, pathfinding algorithms, or unrelated directional branches.

## Impact

- `Hagalaz.Services.GameWorld/Logic/Pathfinding/PathfinderBase.cs`
- `Hagalaz.Services.GameWorld.Tests/PathfinderStepValidationTests.cs`
- `Hagalaz.Services.GameWorld.Tests/MovementTests.cs`
- No new dependencies, public APIs, or persisted data.

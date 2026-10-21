# Design

## Context

The flag and unflag methods both rotate an object's dimensions when its rotation is 1 or 3, then visit the resulting footprint. Their mutation operation differs, and flagging has an additional non-clipped/non-gateway guard.

## Goals / Non-Goals

**Goals:**

- Keep the collision operations and guards separate.
- Centralize the standard-object dimension rule in the existing `MapRegion` partial class.
- Reuse the direct `StandardObject_FlagThenUnflag_WritesRotatedFootprintAndRestoresBaseline` test, which exercises both public methods across rotations 0 through 3.

**Non-Goals:**

- Add a general footprint iterator or callback-based collision operation.
- Change collision storage, region services, or wall-object handling.

## Decisions

- Add one private instance method returning the oriented `(SizeX, SizeY)` values. Both methods use those values for their existing loops.
  - A shared mutation callback would obscure the important distinction between setting and clearing flags; only the repeated geometry rule needs one owner.
- Keep the `FlagStandardObject` early return and both existing `FlagCollision`/`UnFlagCollision` calls at their current control-flow points.
- Rely on the existing public API test data for rotations 0, 1, 2, and 3, and the existing non-clipped flagging test for its early return.

## Risks / Trade-offs

- The private method adds one local call but prevents the two footprint dimensions from drifting.

## Migration Plan

No persisted data or public API signatures change. Rollback consists of restoring the rotation branches in both methods.

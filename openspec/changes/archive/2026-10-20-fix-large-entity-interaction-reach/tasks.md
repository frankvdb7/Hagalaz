## 1. Test-first interaction reach regressions

- [x] 1.1 Add a focused `PathfinderBase` interaction-reach fixture using the existing substituted clipping-map service and deliberately distinct X/Y coordinates.
- [x] 1.2 Add size-2 and size-3 overlap-boundary regressions for `CanDecorationInteract` and `CanDoorInteract`, covering targets inside one axis only and inside both axes.
- [x] 1.3 Add table-driven door regressions for shapes 0, 2, and 9 across supported rotations, actual-side blocking, unrelated collision, and the rotation-0 `(toAbsX, currentAbsY)` lookup.
- [x] 1.4 Add table-driven decoration regressions for shapes 6, 7, and 8 across relevant rotations, actual-side blocking, unrelated collision, and the rotation-0 west-side Y lookup.
- [x] 1.5 Characterize currently valid size-1 door and decoration interaction approaches.

## 2. Shared reach corrections

- [x] 2.1 Correct both large-entity overlap guards to use inclusive X and Y bounds of the mover footprint.
- [x] 2.2 Correct only the two confirmed large-entity collision lookup coordinates while preserving all remaining branch behavior.

## 3. Verification

- [x] 3.1 Run the new focused regressions before the production correction and confirm they fail for the documented footprint and coordinate defects.
- [x] 3.2 Run the focused GameWorld tests after the correction and confirm the new regressions and size-1 characterization pass.
- [x] 3.3 Run the solution build, strict positional OpenSpec validation, and `git diff --check`.

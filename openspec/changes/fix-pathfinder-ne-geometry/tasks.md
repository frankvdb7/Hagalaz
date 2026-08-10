## 1. Regression Coverage

- [x] 1.1 Add a size-2 north-east validator regression that blocks `(fromX + 2, fromY + 1)`.
- [x] 1.2 Correct the movement client-parity north-east row to block `(fromX + 2, fromY + 1)`.
- [x] 1.3 Add size-3 and size-4 north-east top/right edge tests using `WallSouthEast` and `WallNorthEast` directional bits.

## 2. North-east Validation Fix

- [x] 2.1 Change the size-2 north-east third coordinate to `(fromX + 2, fromY + 1)`.
- [x] 2.2 Map the variable-size north-east top edge to `CheckSouthVariable` and right edge to `CheckWestVariable`.

## 3. Verification

- [x] 3.1 Run the focused GameWorld tests and confirm the new regressions fail before the production correction and pass afterward.
- [x] 3.2 Run the solution build, strict OpenSpec validation, and `git diff --check`.

## 1. Test-first directional regressions

- [x] 1.1 Add table-driven size-one `DumbPathFinder` cases for all eight clear compass targets, matching directional blockers, and opposite diagonal blockers using exclusive collision bits.
- [x] 1.2 Add size-two cases for all eight clear compass targets and targeted blockers on their newly occupied footprint, including the southwest anchor regression.
- [x] 1.3 Add a size-three-or-larger southwest characterization regression that preserves the current `(x - 1, y - 1)` anchor update.

## 2. Local traversal corrections

- [x] 2.1 Correct the size-one northeast destination check to use the northeast directional collision composite.
- [x] 2.2 Correct the size-two southwest anchor mutation to decrement both X and Y after validation.

## 3. Verification

- [x] 3.1 Confirm the focused regressions fail before the production corrections and pass afterward.
- [x] 3.2 Run the GameWorld test project, solution build, strict OpenSpec validation, and `git diff --check`.

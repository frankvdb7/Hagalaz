# Tasks

## 1. Characterize and consolidate the calculation

- [x] 1.1 Add tests for `BaseCombatLevel` and `FullCombatLevel`, including a summoning level that changes the full value; verify the tests pass against current behavior.
- [x] 1.2 Extract the common fractional calculation into a private method and keep each property's existing cast order; verify `CharacterStatisticsTests` passes.

## 2. Verify the scoped refactor

- [x] 2.1 Run the focused GameWorld tests and rerun `npx jscpd --mode strict --kind exact --reporters ai --summary Hagalaz.Services.GameWorld`; verify the duplicate getter body is gone and the remaining exact clones are unchanged.

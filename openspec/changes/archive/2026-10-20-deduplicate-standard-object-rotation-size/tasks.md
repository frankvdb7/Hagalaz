# Tasks

## 1. Characterize public collision behavior

- [x] 1.1 Run the existing direct flag/unflag footprint test for rotations 0 through 3 and preserve the non-clipped flagging test.

## 2. Share the orientation rule

- [x] 2.1 Extract the repeated rotated dimensions into one private `MapRegion` method and use it from both public methods.
- [x] 2.2 Rerun the focused public-method tests and verify the footprint and baseline behavior remain correct.

## 3. Verify the scoped refactor

- [x] 3.1 Rerun `npx jscpd --mode strict --kind exact --reporters ai Hagalaz.Services.GameWorld/Model/Maps/Regions/CollisionMethods.cs` and confirm this clone is gone.
- [x] 3.2 Run strict OpenSpec validation and `git diff --check`.

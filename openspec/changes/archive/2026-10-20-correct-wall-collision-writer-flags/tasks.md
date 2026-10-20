## 1. Collision Writer Regression Coverage

- [x] 1.1 Add a test-local `IMapRegionService` collision recorder and object/region fixtures using the existing MSTest and NSubstitute project infrastructure.
- [x] 1.2 Add floor-decoration and standard-object writer coverage for clip gates, rotated footprints, solid/range combinations, and valid flag/unflag restoration.
- [x] 1.3 Add parameterized wall coverage for every supported wall shape and rotation, including independent low/solid/range assertions, gateway/non-solid gates, and inverse preservation of unrelated flags.
- [x] 1.4 Add public collision-dispatch coverage for wall, standard-object, floor-decoration, and wall-decoration shapes.

## 2. Wall Collision Corrections

- [x] 2.1 Correct diagonal/corner solid `Blocked*` writes and removals to the client-parity direction pairs.
- [x] 2.2 Correct unfinished-wall rotation-0 origin range flags to the west/north pair used by its geometry and inverse.

## 3. Verification

- [x] 3.1 Confirm the new targeted regressions fail against the uncorrected writer and pass after the focused corrections.
- [x] 3.2 Run focused and complete GameWorld unit tests, solution-level discovery, a non-incremental solution build, strict OpenSpec validation, and `git diff --check`.

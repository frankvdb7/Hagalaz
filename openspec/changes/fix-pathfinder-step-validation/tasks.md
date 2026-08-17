## 1. Regression Coverage

- [x] 1.1 Add shared-validator tests for size-1 compatibility and all eight size-2 directions, including explicit southeast and distinct northwest exposed-cell cases.
- [x] 1.2 Add size-3 and size-4 cardinal corner/interior-edge tests plus representative diagonal footprint tests using targeted collision flags.
- [x] 1.3 Add unsupported/zero-offset and distance-based unit-validation tests, including distances greater than one.
- [x] 1.4 Add `Movement.Tick` coverage for existing valid walk/run/diagonal/warp waypoint behavior, blocked-warp stop/resume handling, unblock-and-resume handling, a long queued waypoint with a newly blocked intervening tile, multiple independently validated movement units, and size-2/size-3 client parity cases.

## 2. Shared Validation Fix

- [x] 2.1 Correct size-2 southeast and northwest footprint coordinates while preserving existing collision-flag conventions.
- [x] 2.2 Fix all size-3+ cardinal interior loop bounds to validate offsets from 1 through `size - 2`.
- [x] 2.3 Make unsupported scalar offsets fail closed and implement distance checks through unit-step validation.

## 3. Runtime Movement Fix

- [x] 3.1 Change `Movement.Tick` to compute, validate, and commit one-tile candidates within the existing movement budget, retaining blocked waypoints.

## 4. Verification

- [x] 4.1 Run focused GameWorld tests and resolve any regressions.
- [x] 4.2 Run the solution build, strict OpenSpec validation, and `git diff --check`.

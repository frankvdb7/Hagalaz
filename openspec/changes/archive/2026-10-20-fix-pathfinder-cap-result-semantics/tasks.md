## 1. Test-first cap regressions

- [x] 1.1 Add `DumbPathFinder` regressions for short success, pre-cap collision failure, exact final permitted step, cap exhaustion, traversed points, `MovedNear`, `Steps`, and derived destination flags.
- [x] 1.2 Add `ProjectilePathFinder` regressions for short success, pre-cap LOS collision failure, exact final permitted step, cap exhaustion, destination-point behavior, `MovedNear`, `Steps`, and derived destination flags.
- [x] 1.3 Run the focused pathfinder tests before production changes and confirm the new cap regressions fail against the current implementation.

## 2. Cap result correction

- [x] 2.1 Mark `DumbPathFinder` cap exhaustion unsuccessful and preserve its existing progress/point semantics.
- [x] 2.2 Enforce `PathFinderBase.QueueSize` in both projectile trace loops and return the existing unsuccessful failure shape when the target is not reached.

## 3. Verification

- [x] 3.1 Run the focused `DumbPathFinder` and `ProjectilePathFinder` tests after the corrections.
- [x] 3.2 Run the complete `Hagalaz.Services.GameWorld.Tests` project and a production build.
- [x] 3.3 Validate the OpenSpec change strictly and review `git diff --check` plus the final diff for scope and duplicate mechanisms.

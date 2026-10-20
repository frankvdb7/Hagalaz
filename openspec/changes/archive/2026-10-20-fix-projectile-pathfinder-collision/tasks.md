## 1. Regression Characterization

- [x] 1.1 Replace the routing-layer projectile regressions with writer-derived full-object, high-only object, cardinal gateway-wall, high-only wall, diagonal-wall, and movement-only collision cases.
- [x] 1.2 Add exact shallow and steep fixed-point traces in every quadrant, crossed-X/crossed-Y-boundary, old-staircase-only-obstacle, axis/45-degree, southwest-output, and different-plane regressions.
- [x] 1.3 Run the focused tests against the current implementation and confirm the new LOS-layer and asymmetric-ray regressions fail.

## 2. Projectile LOS Correction

- [x] 2.1 Replace sign-based directional traversal with the RuneLite-equivalent 16.16 fixed-point tile-boundary tracer.
- [x] 2.2 Use only `ObjectBlock` and the matching middle `Blocked*` flags for LOS, including diagonal flags on exact 45-degree rays, while preserving the existing `IProjectilePathFinder` runtime boundary.
- [x] 2.3 Verify the focused regressions pass after the production correction.

## 3. Verification

- [x] 3.1 Confirm `CreatureCombat.ReachTarget(range > 1)` still consumes `IProjectilePathFinder` without another collision evaluator.
- [x] 3.2 Run the GameWorld test project and production build, validate the OpenSpec change, and review the final diff for scope and duplicate collision logic.

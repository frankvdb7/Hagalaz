## 1. Regression Characterization

- [x] 1.1 Add focused projectile-pathfinder regressions using collision state emitted by the existing standard-object and wall writers, including empty, floor, floor-decoration, cardinal paired-wall, and diagonal cases.
- [x] 1.2 Add regression coverage for the writer-derived range-permissive `ObjectBlock | ObjectAllowRange` standard object, high-object-only blocker, floor/decorations combined with that range-permissive state, and the southwest waypoint coordinate.
- [x] 1.3 Run the focused projectile-pathfinder tests before the production correction and confirm the range-permissive regression fails for the mask-only behavior.

## 2. Projectile Traversal Correction

- [x] 2.1 Make `ProjectilePathFinder` honor its existing traversal mask while allowing only the writer-derived range-permissive `ObjectBlock | ObjectAllowRange` object state.
- [x] 2.2 Correct the southwest coordinate update and verify the focused regressions pass.

## 3. Verification

- [x] 3.1 Confirm `CreatureCombat` continues to use `IProjectilePathFinder` for ranged target reach without another collision evaluator.
- [x] 3.2 Run the focused GameWorld test project and build, validate the OpenSpec change, and review the final diff for scope and duplicate collision logic.

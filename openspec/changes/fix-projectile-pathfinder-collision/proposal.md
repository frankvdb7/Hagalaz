## Why

The previous implementation and its first correction treat the high `WallAllowRange*` and `ObjectAllowRange` routing layer as projectile line-of-sight (LOS). That is reversed. The client-era collision writer emits a distinct LOS layer: `ObjectBlock` is the full LOS blocker and the middle `Blocked*` flags are directional LOS blockers. In addition, `ProjectilePathFinder` follows the sign of the remaining delta, which stair-steps non-45-degree rays instead of checking the tiles that a projectile ray actually crosses.

## What Changes

- Replace routing-mask checks with the existing full and directional LOS collision layer, including Hagalaz's diagonal wall flags on matching 45-degree rays.
- Trace each projectile ray with the 16.16 fixed-point, axis-boundary algorithm used by RuneLite's `WorldArea` LOS implementation.
- Retain the existing `IProjectilePathFinder` runtime boundary and collision writers; only their already-emitted LOS state is consumed.
- Add focused writer-derived and off-axis-ray regressions before the production correction.

## Scope and Acceptance Criteria

- `ObjectBlock` blocks LOS whether or not `ObjectAllowRange` is also present.
- Gateway walls and standard objects block LOS through their `Blocked*` or `ObjectBlock` state even though they omit the high routing layer.
- `ObjectAllowRange`, `WallAllowRange*`, `FloorBlock`, and `FloorDecorationBlock` do not by themselves block the client-equivalent tile-to-tile LOS ray.
- A matching diagonal `Blocked*` flag blocks an exact 45-degree ray through its wall/corner geometry.
- An off-axis ray follows its fixed-point tile crossings; blockers on each crossed X or Y boundary stop it.
- A ray between different planes is unsuccessful.
- `CreatureCombat.ReachTarget(range > 1)` continues to consume the single `IProjectilePathFinder` result.

## Non-Goals

- Do not change collision writing, enum layout/names, movement routing, combat targeting, service registration, or dependencies.
- Do not add terrain-height/arc collision or a separate LOS service.
- Do not broaden this tile-to-tile correction into RuneLite's multi-tile `WorldArea` candidate-selection policy; the existing `ProjectilePathFinder` currently traces its supplied source and target tiles.

## Impact

- Runtime path: `ProjectilePathFinder` through `CreatureCombat.ReachTarget(range > 1)`.
- Affected tests: `Hagalaz.Services.GameWorld.Tests` projectile-pathfinder regressions.
- No API, schema, migration, or deployment change.

## Why

`ProjectilePathFinder` currently accepts failed directional and object collision checks whenever a tile lacks `FloorBlock`. This allows ranged line-of-sight to pass through collision written by the existing map collision system, while its southwest step reports the wrong destination. The correction must preserve the writer's intentional range-permissive standard-object combination rather than treating every high object bit as a blocker.

## What Changes

- Define projectile line-of-sight as a traversal of the existing collision masks written by `CollisionMethods`, with the range-permissive standard-object combination evaluated explicitly.
- Correct `ProjectilePathFinder` so a matching directional, incompatible object, floor-decoration, or floor blocker stops traversal instead of falling through on non-floor tiles.
- Correct southwest traversal to move to the southwest tile.
- Add focused MSTest regressions covering cardinal and diagonal projectile blockers, paired wall sides, standard object range behavior, and the existing `CreatureCombat` projectile-pathfinder boundary.

## Capabilities

### New Capabilities

- `projectile-line-of-sight`: Projectile traversal honors the collision layers emitted by map objects and reports the actual traversed coordinates.

### Modified Capabilities

- None.

## Scope and Acceptance Criteria

- Empty tiles, objects whose collision writer omits the projectile layer, and standard objects that emit `ObjectBlock | ObjectAllowRange` remain traversable.
- `FloorBlock`, `FloorDecorationBlock`, and matching cardinal or diagonal projectile-direction bits stop line-of-sight from every direction.
- A standard object with only `ObjectAllowRange` stops line-of-sight; the writer's `ObjectBlock | ObjectAllowRange` range-permissive combination does not.
- A southwest path reaches `(x - 1, y - 1)`.
- `CreatureCombat.ReachTarget(range > 1)` continues to consume `IProjectilePathFinder` without a second collision implementation.

## Non-Goals and Stop Conditions

- Do not change the collision writer, enum layout, combat range rules, or pathfinding architecture.
- Do not introduce a separate line-of-sight service or duplicate collision state.
- If client evidence requires changing collision flags themselves rather than the existing projectile pathfinder interpretation, stop and create a separate change.

## Impact

- Affected runtime path: `ProjectilePathFinder` through `CreatureCombat.ReachTarget(range > 1)`.
- Affected tests: `Hagalaz.Services.GameWorld.Tests` projectile-pathfinder regressions.
- No API, dependency, schema, migration, or service-registration change.

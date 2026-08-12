## Context

`ProjectilePathFinder` advances one tile at a time and already receives the exact directional mask for each cardinal or diagonal step. `CollisionMethods` writes the collision state, and `CreatureCombat.ReachTarget` already consumes the projectile pathfinder for attacks with range greater than one.

The client-side `CollisionData.flagStandartObject` writes `0x20000` for a solid object and `0x40000000` when the object is not a gateway. The server's standard-object writer mirrors that layout as `ObjectBlock` and `ObjectAllowRange`. The historical projectile regression explicitly treats their combined writer output as range-permissive. The existing server `Traversable*Blocked` composites still contain the high directional/object layer, floor-decoration layer, and floor layer, so that special combination must not become a fallback that ignores an accompanying wall or floor blocker.

## Goals / Non-Goals

**Goals:**

- Make the existing directional projectile masks the sole traversal decision.
- Characterize the collision writer's standard-object and wall output through the projectile pathfinder.
- Preserve the existing combat-to-projectile-pathfinder dependency and correct the southwest coordinate update.

**Non-Goals:**

- Rename or relayout collision flags, change map collision writing, or add a line-of-sight abstraction.
- Change melee movement/pathfinding or combat target selection.
- Add persistence, background work, or dependencies.

## Decisions

### Use the existing `Traversable*Blocked` mask with its range-permissive object exception

`CheckSingleTraversal` already selects the exact client-derived mask for every step. `IsTraversable` will allow a step when the selected mask is absent, or when the selected mask contains only `ObjectAllowRange` and the tile also carries `ObjectBlock`. This is the precise range-permissive combination emitted by a solid, non-gateway standard object. Any floor, floor-decoration, or directional wall bit remains a blocker because it makes the selected mask contain more than `ObjectAllowRange`.

Rejected alternative: build a separate projectile-mask table or line-of-sight service. It would duplicate the collision knowledge already represented by the masks and create a second owner for directional semantics. Also rejected: a broad `ObjectBlock && ObjectAllowRange` fallback, because it would allow a floor or wall blocker on the same tile.

### Derive object and wall test inputs from writer-equivalent state

The regressions will use the same standard-object and paired-wall flag combinations emitted by `CollisionMethods`: a solid non-gateway object emits the range-permissive `ObjectBlock | ObjectAllowRange` combination; a non-solid non-gateway object emits only `ObjectAllowRange` and blocks; a gateway object omits the high object layer and remains traversable. Cardinal and diagonal tests will put the paired high directional flag on each physical side of the wall.

Rejected alternative: test only arbitrary enum values. That would not prove the pathfinder agrees with the collision data actually generated for map objects.

### Correct southwest at the existing step owner

The southwest branch validates `(x - 1, y - 1)` and will update to those same coordinates before adding the waypoint.

Rejected alternative: compensate in consumers. The coordinate transition belongs exclusively to `ProjectilePathFinder`.

## Risks / Trade-offs

- [Misleading legacy enum names] → Assert actual flag combinations from `CollisionMethods`, preserve the existing range-permissive regression, and test that floor/decorations still win over that exception.
- [Directional regression affects ranged combat] → Cover all cardinal and diagonal directions and retain the direct `CreatureCombat` dependency without adding a second evaluator.
- [Focused fix silently broadens] → Stop if the writer itself requires modification; this change only consumes its existing output.

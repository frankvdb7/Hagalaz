## Context

`CollisionMethods` and the companion game client both write three independent layers: low movement walls/occupancy, middle LOS blockers, and high routing flags. The public RuneLite collision definitions identify `0x20000` as the full LOS blocker and `0x400`, `0x1000`, `0x4000`, and `0x10000` as the four cardinal LOS blockers. Hagalaz's collision writers additionally emit the middle diagonal `Blocked*` flags for diagonal wall/corner geometry. The high `0x4...`/`0x6...` masks are used by the companion client routefinder and must not be inferred to represent projectile LOS.

The current `ProjectilePathFinder` uses the high routing composites and `DirectionHelper.GetDirection`, so it both reads the wrong layer and changes direction only after it has stair-stepped to an axis. The RuneLite tile LOS implementation traces a 16.16 fixed-point ray, checking the relevant collision mask each time the ray crosses an X or Y tile boundary.

## Decisions

### Consume only the existing LOS layer

The full LOS mask is `ObjectBlock`. When a ray crosses east or west, it checks `ObjectBlock` together with the destination tile's west or east `Blocked*` flag, respectively. When it crosses north or south, it checks `ObjectBlock` together with the destination tile's south or north `Blocked*` flag, respectively. An exact 45-degree ray also checks Hagalaz's matching diagonal `Blocked*` flag as it enters each diagonal tile. No high routing, low movement, floor, or floor-decoration flag participates in this tile-to-tile LOS decision.

Rejected alternative: reinterpret or rename the existing flags. The writers already faithfully preserve the client bit layout; changing producers or aliases would be scope expansion and risk movement behavior.

### Trace the ray with fixed-point boundary crossings

For the dominant axis, represent the other axis in 16.16 fixed point at the source tile centre. Advance one tile on the dominant axis, test that boundary, then test the secondary boundary if the fixed-point coordinate crossed into another tile. The initial half-tile offset and negative-slope rounding match RuneLite's LOS algorithm. On an exact 45-degree ray, test the matching Hagalaz diagonal wall flag as well. Collision probes are not path steps: a successful result appends only the supplied target tile, and combat continues to consume `Successful`.

Rejected alternative: repeated `DirectionHelper.GetDirection`. It cannot preserve a non-45-degree slope and inspects tiles the ray does not cross.

### Preserve the existing runtime boundary

`CreatureCombat` already asks `IProjectilePathFinder` for ranged target reach. The correction remains inside that pathfinder and uses its existing map-region collision source; no combat-side collision evaluator is added.

## Risks and Mitigations

- [Routing/LOS layer confusion] → writer-derived tests cover high-only, full-object, and gateway states separately.
- [Fixed-point rounding regression] → assert exact shallow and steep ray crossings in opposite quadrants and blockers at both X and Y boundaries.
- [Directional wall asymmetry] → cover all four cardinal gateway-wall rotations and both physical sides.
- [Diagonal wall omission] → cover writer-derived corner/diagonal rotations on matching 45-degree rays.
- [Scope expansion] → leave writers, enum names, target-footprint policy, and terrain/arc handling unchanged.

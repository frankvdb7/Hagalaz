## Context

`PathFinderBase.CheckStep` is the shared collision validator used by the simple, smart, and projectile pathfinders, while `Movement.Tick` is the runtime consumer that applies queued movement. The validator accepts only unit directional offsets, but the current implementation has incorrect size-2 coordinates, non-executing size-3+ cardinal loops, and a fail-open fallback. `SmartPathFinder` compresses straight route segments into waypoints, so `Movement.Tick` can receive a multi-tile delta even though it applies only one or two movement units per tick.

The client variable-size routefinder in `Class527.java` is the geometry reference. Existing `CollisionFlag` values and `IMapRegionService` remain the collision source of truth.

## Goals / Non-Goals

**Goals:**

- Make shared footprint checks correct for size 1, size 2, and size 3+ creatures.
- Ensure every public scalar step check either validates a supported unit direction or returns false.
- Ensure distance checks and runtime movement never pass multi-tile offsets to the scalar validator.
- Preserve movement queue ownership and stop at the last valid tile when a queued route becomes blocked.

**Non-Goals:**

- Replacing BFS, route compression, collision storage, or movement queues.
- Changing collision flag definitions or unrelated pathfinding interactions.
- Adding a generic movement/retry framework or changing public method signatures.

## Decisions

### Use `PathFinderBase` as the single footprint-validation owner

All size-specific geometry remains in the existing shared validator. The size-2 southeast condition is corrected, the northwest exposed coordinate is made distinct, and the four size-3+ cardinal loops use the client bound `sizeOffset < size - 1`. Existing diagonal formulas are retained and covered by regression tests against the client geometry.

Changing individual pathfinder consumers would duplicate collision rules and would leave other callers exposed. Rewriting the pathfinder is unnecessary for these localized geometry defects.

### Fail closed for unsupported scalar offsets

The scalar `CheckStep` overload returns false for offsets outside the eight unit directions, including a zero/no-op offset. Callers that encounter a no-op handle it before validation. Returning true would preserve the current fail-open vulnerability; throwing would turn recoverable stale-route movement into an exception.

The distance-based overload advances between sampled locations one unit at a time and delegates each unit to the scalar validator. This preserves its existing collision-flag ownership while preventing its row and diagonal transitions from bypassing validation.

### Validate each movement unit in `Movement.Tick`

`Movement` computes the next one-tile location toward the queued waypoint, calls `CheckStep` with that unit delta, and commits the location only when validation succeeds. It repeats until the existing movement budget is exhausted, the waypoint is reached, or a step is blocked. A blocked step leaves the queued waypoint in place and preserves the last valid `finalLocation`.

Passing the original compressed delta is rejected because it cannot describe the movement actually being applied. Clamping the delta before one final check is also rejected because it could skip a blocked intermediate tile when run or warp movement applies multiple units.

### Tests use existing MSTest and substitution seams

Pathfinder regressions use the existing `IMapRegionService` substitution with `CollisionFlag.Walkable` as the default and `FloorBlock` at targeted footprint coordinates. Movement tests use a minimal `Creature` test double and the existing `ISmartPathFinder`/service-provider seam, covering valid walk, run, and diagonal waypoint movement alongside a waypoint longer than one tile whose collision appears before application.

## Risks / Trade-offs

- [Risk] Unit-by-unit validation adds collision checks for run and warp movement. → Mitigation: retain the existing movement budget and use the already-authoritative in-process map service; focused tests cover the bounded behavior.
- [Risk] Existing callers may have relied on the scalar fall-through returning true. → Mitigation: search all callers, update the distance-based internal caller, and add explicit unsupported-offset regression coverage.
- [Risk] Client and server coordinate conventions could diverge. → Mitigation: preserve existing flag formulas, compare corrected coordinates with `Class527.java`, and cover every size-2 direction plus representative variable-size diagonals.

## Migration Plan

No data or deployment migration is required. Deploy the code and tests together. Rollback is a normal application version rollback because no persisted state or wire contract changes.

## Open Questions

None. The issue acceptance criteria and existing runtime topology define the required behavior.

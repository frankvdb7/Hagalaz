## Context

`PathFinderBase` already owns the shared `QueueSize` safety limit. `DumbPathFinder` counts simple traversal steps from an initial `Path.Steps` value of `-1`, but returns its initially successful result when the limit is reached before the destination. The current projectile finder uses the same `Path` result type and increments `Steps` while tracing, but does not apply the shared limit. `Path.ReachedDestination` and `Path.MovedNearDestination` are derived from the existing `Successful`, `MovedNear`, and `Steps` values, so the correction must preserve that contract.

The projectile finder intentionally does not expose collision-probe tiles as movement points: it adds only the requested destination after a complete successful trace. A capped projectile trace therefore returns no points, just as a collision failure does, while the result status records the failed operation and progress.

## Goals / Non-Goals

**Goals:**

- Make cap exhaustion an unsuccessful result in both affected finders.
- Enforce the existing `QueueSize` limit in both projectile trace loops without changing ray geometry or collision checks.
- Preserve meaningful progress state and existing point/derived-property semantics.
- Add deterministic MSTest regressions for cap boundaries and existing pre-cap failures.

**Non-Goals:**

- No new result abstraction, path state, or public API.
- No changes to `Path`, `IPath`, collision flags, traversal direction, fixed-point ray calculations, or consumers.
- No change to the cap value or to SmartPathFinder's independently owned reconstruction behavior.

## Decisions

### Use the existing cap and result type

Each finder will use `PathFinderBase.QueueSize` as its only safety limit. On a cap check that occurs before the next traversal, the finder will set `Successful = false` and return. `DumbPathFinder` will set `MovedNear` from whether its current coordinates differ from the source, matching its existing collision-failure progress behavior. `ProjectilePathFinder` will retain its existing `MovedNear = path.Steps > 0` failure rule, which distinguishes a first-probe failure from a trace that made progress.

Adding a new cap-specific result state or changing `Path` derived properties would duplicate state and expand the public contract without being necessary to distinguish success from an unreached target.

### Apply the projectile guard inside both axis tracers

The X- and Y-dominant trace loops each increment `path.Steps` and then check the shared cap before advancing or reading collision. This preserves the existing off-axis fixed-point sequence and ensures the exact final permitted step succeeds, while the next attempted step fails before any additional movement or collision probe.

Centralizing the guard in `Find` is rejected because the number of iterations is owned by the two private trace loops and a post-trace check cannot prevent unbounded tracing.

### Test the boundary with deterministic coordinate distances

Focused tests will use clear, deliberately distant horizontal routes to require exactly the permitted number of steps or one more. They will also retain short-path and pre-cap collision cases. Assertions will inspect point contents/count, `Successful`, `MovedNear`, `Steps`, and both derived destination properties so the cap result cannot regress into a superficially successful partial route.

## Risks / Trade-offs

- [Existing consumers may rely on false-success partial routes] → The result contract explicitly defines success as reaching the requested operation; consumers already inspect `Successful`, and no consumer-side change is needed.
- [Projectile cap failures expose no partial points] → Preserve the existing projectile convention that only a fully successful trace adds the destination; use `Steps` and `MovedNear` to report progress.
- [Boundary off-by-one] → Test a target at exactly the permitted traversal count separately from a target one step beyond it.

## Migration Plan

No data or deployment migration is required. Deploy the code and tests together; rollback is a normal code revert if needed.

## Open Questions

None.

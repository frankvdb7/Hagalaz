## Context

`SmartPathFinder` owns breadth-first route expansion for one-tile, two-tile, and variable-size movers. Its existing per-direction branches already contain the collision lookups, queue write, and graph-bound checks required by this change. Issue #366 identifies six local copy/paste defects against the GameClient traversal geometry.

## Goals / Non-Goals

**Goals:**

- Align the six affected checks with their collision-validated direction and footprint.
- Keep queued anchors, collision geometry, and `GraphSize` bounds mutually consistent.
- Add readable route-level regressions that distinguish directional masks and coordinates.

**Non-Goals:**

- Do not replace the BFS, factor it into a new framework, or modify collision flag values.
- Do not alter `DumbPathFinder`, `PathfinderBase`, movement execution, or client protocol behavior.

## Decisions

### Correct the existing branches in place

Each defect is a wrong flag, coordinate, or bound in `SmartPathFinder`; the existing branch remains the sole owner of that expansion. A new traversal helper or pathfinding implementation is rejected because it would broaden a six-line family of corrections into a second mechanism for BFS geometry.

### Use exclusive traversal bits for mask regressions

Tests use the `WallAllowRange*` bits that belong to one relevant traversal composite, instead of `FloorBlock`, which occurs in every composite and would hide a direction swap. A constrained collision map leaves only the geometry for the branch under test walkable.

### Drive variable-size boundaries through public route finding

Boundary tests build a one-direction corridor to the final valid anchor and request the next invalid anchor. They assert the route fails and clipping lookups remain inside the 104x104 graph. This covers queue behavior and reconstruction through `Find` without exposing private BFS state.

## Risks / Trade-offs

- [A side cell shared by an alternative direction can hide a failed direct expansion] → Assert the direct one-step route length where appropriate, and constrain all non-required cells with `FloorBlock`.
- [A collision bit common to multiple masks can hide a swapped mask] → Use direction-exclusive `WallAllowRange*` bits for each regression.
- [A boundary test could only prove failure, not prevent an out-of-graph read] → Record clipping lookups and assert that their local coordinates remain in range.

## Migration Plan

No data migration or deployment sequencing is required. The correction is in-process pathfinding logic; rollback is a normal code rollback.

## Open Questions

None. The audited defects and expected geometry are specified by issue #366.

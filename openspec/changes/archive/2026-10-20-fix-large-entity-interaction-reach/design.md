## Context

`PathfinderBase` already owns the shared reach decision for door and decoration targets. Its large-entity branches query the existing clipping map directly, but their early overlap guards only constrain one axis and two branches pass an X coordinate as the clipping-map Y coordinate. There is no focused interaction-reach test suite today; the existing pathfinder tests exercise route steps rather than these public reach methods.

## Goals / Non-Goals

**Goals:**

- Make large-entity overlap a two-dimensional footprint check in both shared reach methods.
- Query collision from the actual side tile in the two confirmed malformed branches.
- Establish direct, table-driven regressions for sizes 2 and 3 without making equal coordinates hide an X/Y swap.

**Non-Goals:**

- Do not alter the remaining shape/rotation semantics, size-1 branch, route traversal, or collision flag values.
- Do not introduce a generic geometry helper or interaction subsystem.

## Decisions

### Keep `PathfinderBase` as the single interaction-reach owner

Correct the existing guards and clipping calls in place. All pathfinder consumers already use these methods, so moving logic to combat or a new reach service would create a second authority and broaden the change beyond #368.

### Express the overlap correction directly

Each large-entity guard will compare target X and Y against the mover origin and inclusive `size - 1` corner. A shared rectangle helper is rejected because it would only conceal two short checks and add an abstraction with no second current owner.

### Characterize branches through the public methods

Use a focused test fixture with the existing substituted `IMapRegionService`, calling `CanDoorInteract` and `CanDecorationInteract` directly. Parameterized cases will cover the existing supported shapes/rotations, size 2 and 3, actual-side blocking, and unrelated collision. The tests will use deliberately distinct X/Y coordinates; they validate behavior rather than reimplementing pathfinding geometry.

### Use the GameClient selectively

Use the coherent client shape/rotation collision lookup as evidence for the corrected door tile `(toAbsX, currentAbsY)`, but derive overlap behavior from ordinary two-dimensional footprint geometry. The client's malformed initial overlap guard is not copied.

## Risks / Trade-offs

- [A rotation matrix can accidentally mask an incorrect coordinate] → Keep X and Y distinct and pair actual-side blocking with an unrelated-tile control for every touched lookup family.
- [Broader legacy branch defects may be uncovered] → Preserve existing behavior unless a regression demonstrates a defect within this issue's acceptance criteria; record anything else as a follow-up.
- [Table-driven tests become opaque] → Give each case a descriptive display name with shape, rotation, size, side, and collision condition.

## Migration Plan

No migration or deployment sequencing is required. The change is an in-process logic correction with test coverage; rollback is a normal code rollback.

## Open Questions

None. The scope and intended geometry are fixed by the issue's audited defects.

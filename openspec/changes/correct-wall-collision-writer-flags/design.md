## Context

`MapRegion` is the existing owner of collision mutations for map objects. It delegates absolute-location updates to `IMapRegionService`; the service remains the only collision-store boundary. The diagonal/corner solid layer has two incorrect directions in the flag path and two mismatched directions in the unflag path. Unfinished-wall rotation 0 similarly writes a range pair that its inverse does not remove.

## Goals / Non-Goals

**Goals:**

- Preserve the existing writer topology while making low, solid, and range directional pairs geometrically consistent.
- Exercise every writer and the public dispatch methods with deterministic, stateful collision assertions.

**Non-Goals:**

- Change collision enumeration values, projectile traversal, line-of-sight policy, map loading, or collision-store ownership.
- Add a production helper, service, dependency, persistence model, queue, or retry mechanism.

## Decisions

### Keep `MapRegion` as the sole writer

The five corrected assignments stay in the existing `FlagWallObject` and `UnFlagWallObject` branches. Redirecting writes through a new directional mapper would broaden the change and risk altering unrelated wall semantics.

### Use the client geometry for diagonal directions and local writer symmetry for unfinished walls

The existing `CollisionFlag` numeric layout already matches the client. For unfinished-wall rotation 0, the low and solid layers plus the removal path agree on the west/north origin pair, making that pair the authoritative range geometry.

### Use a test-local stateful map-service recorder

`IMapRegionService` is the existing seam. Tests will update an in-memory coordinate-to-flag map from NSubstitute callbacks, allowing exact layer and inverse assertions without changing production APIs or creating a second collision implementation.

### Parameterize geometry instead of duplicating test bodies

Typed MSTest dynamic data will describe rotations, affected coordinates, and expected directional masks. Tests will assert low, solid, and range masks independently so a correct layer cannot hide an error in another.

## Risks / Trade-offs

- [Risk] The comprehensive matrix can accidentally encode an incorrect direction. → Mitigation: use explicit rotation tables, client parity for diagonal walls, and independent per-layer assertions.
- [Risk] Flag bitmasks do not track overlapping object ownership. → Mitigation: validate the supported lifecycle of an object being flagged and then removed, while preserving an unrelated seeded flag; do not redefine collision ownership in this change.
- [Risk] A wider test suite might reveal unrelated writer defects. → Mitigation: the approved unfinished-wall correction is included; any further production defect pauses this change for a follow-up.

## Migration Plan

No data migration or rollout sequencing is required. Deploy the corrected server assembly normally. Roll back by reverting the focused source and test changes together.

## Open Questions

None.

## Why

`GroundItem` and `GameObject` expose entity handles, but map-region loading and
runtime mutation paths did not consistently register those world entities in
the existing `IEntityStore`. This allowed observable entities to have no
resolvable identity and could leave stale handles after permanent removal.

## What Changes

- Make `MapRegionPart` the existing collection/lifecycle boundary that
  registers ground items and game objects when they become observable.
- Unregister entities on permanent removal, region destruction, and loader
  rollback while preserving existing entity-store generation protection.
- Keep disabled static game objects owned by their region and preserve their
  identity across disable/re-enable; unregister them when the region is
  permanently destroyed.
- Cover runtime, loader, removal, generation, static re-enable, and region
  destruction behavior with deterministic tests.

## Non-Goals

- No entity-level `IEntityStore` dependency or lifecycle state.
- No new identity store, coordinator, lease, worker, or retry mechanism.
- No changes to NPC identity registration or the existing `EntityStore`
  generation algorithm.

## Acceptance Criteria

- Every runtime or loader-created ground item/game object that is added to a
  map region receives a non-default handle and resolves through
  `IEntityStore`.
- Permanent removal invalidates resolution, including region destruction of
  disabled static game objects.
- Disabling and re-enabling the same static game object preserves its handle.
- Removed handles cannot resolve a later entity that reuses the slot.
- Focused and full affected test projects and the solution build pass.

## Stop Conditions

Stop and record follow-up work if satisfying these criteria requires changing
the entity contracts, adding another lifecycle owner, or changing the
`EntityStore` slot/generation semantics.

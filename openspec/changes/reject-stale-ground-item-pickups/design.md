## Context

`GroundItemReachTask` keeps the original `IGroundItem` reference until the player reaches the target. `GroundItem.Despawn()` calls the map-region removal path, but the current `void` contract cannot distinguish removal of the active instance from a stale reference. The existing game/region execution model already serializes these operations, so the fix only needs to make that result truthful.

## Goals / Non-Goals

**Goals:**

- Make the existing region removal path return whether the exact reference was removed.
- Preserve successful client update, destroy, and respawn behavior.
- Let `GroundItem.Despawn()` and the existing pickup flow use that result.

**Non-Goals:**

- No claim service, token, lease, lock, transaction, state machine, or inventory rollback mechanism.
- No changes to pathfinding, task scheduling, inventory capacity rules, or unrelated map-region operations.

## Decisions

1. Change only the ground-item overloads of `IMapRegionPart.Remove` and `IMapRegion.Remove` to return `bool`. Callers that do not need the result remain source-compatible, while the pickup path can propagate it.
2. In `MapRegionPart`, locate the item by reference identity in the existing per-location list before any update, list mutation, destruction, or respawn work. Reference identity is required because a stale object must not match a different object with equal values at the same location.
3. Return `false` when the region part or exact instance is absent. Return `true` only after the existing successful removal and respawn path has completed.
4. Keep `GroundItem.Despawn()` as the owner of the map-region lookup and return the region's boolean result. `ItemScript.TakeItem` already gates inventory insertion on `Despawn()`, so no second pickup mechanism is needed.

The simpler alternative of making only `GroundItem.Despawn()` track a local flag cannot know whether the region contained the exact instance. Adding synchronization would duplicate the existing serialized game-world boundary and is outside the issue.

## Risks / Trade-offs

- [Risk] Existing callers may ignore the new boolean return value. → This preserves their behavior while the affected pickup path consumes the result; focused compilation verifies all interface implementations.
- [Risk] A failure after successful removal could still leave inventory insertion unsuccessful. → Inventory capacity is checked before removal as today; broader rollback is explicitly outside this change.

## Migration Plan

No data migration is required. Deploy the code change and rely on the existing region update and respawn flow. Rollback is a code rollback because the public persistence format is unchanged.

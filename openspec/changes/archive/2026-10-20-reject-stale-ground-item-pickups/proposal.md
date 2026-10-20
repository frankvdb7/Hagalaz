## Why

Queued ground-item interactions retain the clicked `IGroundItem` instance. When one interaction removes that instance, a later interaction can still receive the item because the removal API reports success even when the instance is no longer present. This can duplicate visible ground items into player inventories.

## What Changes

- Make ground-item removal return success only when the exact instance is present in the target region part and is removed.
- Propagate the removal result through `IMapRegion` to `GroundItem.Despawn()`.
- Keep pickup inventory mutation conditional on successful despawn.
- Add deterministic coverage for stale references, same-location replacements, normal pickup, inventory-full pickup, and respawn behavior.
- Record no new claim, lock, transaction, state-machine, or item-container mechanism.

## Capabilities

### New Capabilities

- `ground-item-pickup`: A queued pickup may grant an item only after the exact active ground-item instance has been removed.

### Modified Capabilities

None.

## Impact

The change affects the `IMapRegionPart` and `IMapRegion` removal contracts, `MapRegionPart`, `MapRegion`, and `GroundItem.Despawn()`. Existing region update, destroy, and respawn behavior remains unchanged after a successful exact-instance removal. Tests will be added to `Hagalaz.Services.GameWorld.Tests` and, where needed, the existing item-script test project.

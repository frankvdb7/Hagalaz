## Why

Persisted item containers compact sparse slots while dehydrating, so loading moves items to different physical slots. `SetItems` also accepts an array of any length as its backing storage and retains the caller's array.

## What Changes

- Preserve physical slot IDs in all six persisted character item containers.
- Restore persisted items through one protected, validated container operation that does not use gameplay insertion.
- Keep `SetItems` for trade rollback, but require capacity-sized input and copy its array.
- Preserve item extra data when the item builder finishes construction.
- Add focused round-trip and corrupt-state regressions.

### Non-goals

- No changes to normal `Add` or `AddRange` behavior, shop stock semantics, DTO shapes, or persistence infrastructure.
- No generic serializer or new service layer.

### Acceptance Criteria

- Inventory, bank, equipment, familiar inventory, reward, and money pouch dehydration records physical slots; sparse round trips retain every slot.
- Extra data survives a dehydrate/hydrate round trip.
- Restoration rejects negative/out-of-range/duplicate slots and invalid counts, while money pouch accepts zero coins.
- Restoration is separate from gameplay insertion and retains capacity-sized, container-owned storage.
- The trade snapshot use of `SetItems` remains functional and cannot change storage length or retain a caller-owned array.

## Impact

`BaseItemContainer`, the six GameWorld container implementations, `ItemBuilder`, and focused container tests. No schema or dependency change.

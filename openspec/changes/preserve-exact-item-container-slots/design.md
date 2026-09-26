## Context

`BaseItemContainer` owns `Items` and `Capacity`. Character item containers currently build a capacity-sized array from persisted slot IDs and pass it to `SetItems`, which assigns the array directly. Dehydration filters empty entries before assigning sequence indices. Equipment uses semantic positions; money pouch stores a zero-count coin item.

## Decisions

- Add a protected `RestoreItems` operation to `BaseItemContainer` that accepts slot/item pairs, validates slots, duplicates, and counts, builds a new capacity-sized array, then uses the existing `SetItems` path. This retains equipment and money-pouch post-restore behavior and trade locking.
- Make count validation positive by default. The existing `CountToResetTo = 0` setting explicitly opts money pouch and shop stock into their zero-count sentinel semantics. Shop stock does not use persisted restoration.
- Keep `SetItems` for trade rollback. Validate array length and copy the array before assignment. It retains item references because trade rollback restores exact item instances and counts.
- Use one small GameWorld persistence helper to build item/slot pairs and dehydrate by looping over physical slots. Keep the DTO types as they are, including the familiar-specific type.
- Return the configured item from `ItemBuilder.Build` so hydrated extra data is not discarded.

## Risks / Trade-offs

- `SetItems` still accepts item references; callers can mutate an item object they own. The issue requires ownership of storage, and trade rollback relies on item identity, so cloning items would change unrelated behavior.
- Hydration constructs items before the restoration boundary validates the whole set. The container state remains unchanged when validation rejects input.

## Migration Plan

No data migration; existing sparse data uses its stored slot IDs on the next load.

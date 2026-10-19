# Proposal

## Why

`MoneyPouchContainer` currently mirrors every boundary an inventory participant advertises, even though production `InventoryContainer` owns exactly one `ItemContainer`. That makes MoneyPouch participation and its tests more general than the domain requires, and makes standalone pouch operations interpret foreign transaction bindings instead of letting `Begin` handle contention.

## What Changes

- Capture the inventory's single item-storage boundary when MoneyPouch is constructed and contribute exactly the pouch and inventory boundaries.
- For exact pouch operations, participate only when the current thread owns both required boundaries in the same active transaction; reject partial ownership. When the current thread owns neither, create the existing standalone transaction and let `Begin` handle foreign contention.
- Keep `TryTransferCoinsFrom` caller-owned and require its source, pouch, and inventory boundaries to share the active transaction.
- Remove tests and fixtures that advertise arbitrary multiple inventory boundaries.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: specify MoneyPouch's two-boundary contribution and current-thread participation behavior.

## Impact

This changes `MoneyPouchContainer`, one narrow internal mutation-boundary query, focused GameWorld tests, and the item-container storage specification. It reuses `ItemContainerTransaction.ResolveSingleBoundary`, existing transaction begin/commit behavior, and the inventory's existing `ItemContainer`.

## Non-goals

- No public transaction API or general participant-enlistment changes.
- No dynamic enlistment, multi-storage inventory support, or transaction propagation abstraction.
- No changes to coin overflow/underflow semantics, publication ordering, or caller-owned coin transfers.

## Acceptance Criteria

- MoneyPouch captures one inventory boundary and exposes exactly its pouch and inventory storage to transaction resolution.
- Exact add/remove operations join one current-thread transaction containing both boundaries, reject partial current-thread ownership, and use standalone `Begin` when neither boundary is owned by the current thread.
- Foreign transactions are handled by normal `Begin` contention for standalone exact operations.
- `TryTransferCoinsFrom` still requires its source and both MoneyPouch boundaries in one caller-owned transaction.
- Artificial multiple-boundary inventory tests and test-only contribution plumbing are removed; focused domain regressions remain.

## Stop Conditions

Stop if production inventory construction does not guarantee one stable `ItemContainer`, or if the change requires a new public API or transaction-enlistment mechanism.

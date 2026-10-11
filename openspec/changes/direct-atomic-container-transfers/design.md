# Design

## Context

The current `ItemContainerMutationBoundary` already owns each storage's monitor, immutable `LockOrder`, transaction binding, standalone publication ownership, and the enlisted exact-transfer delegation. `ItemContainerStorage.TryTransferTo` is the single mutation algorithm. This change adds only the direct two-boundary standalone path; explicit transaction lifecycle stays in `ItemContainerTransaction`.

## Goals / Non-Goals

**Goals:**

- Keep standalone two-container transfer synchronization and publication ownership in the existing mutation boundary.
- Reuse the current storage algorithm and transaction participation checks.
- Keep Equipment validation and lifecycle behavior in `EquipmentContainer`.

**Non-Goals:**

- Alter transaction internals, single-container publication, other domain workflows, or public signatures.
- Add a coordinator, ambient context, lock lease, or generalized callback mechanism.

## Decisions

### Standalone ownership is a pair of boundary claims

Use the two existing monitors in ascending `LockOrder`. Inspect transaction and standalone ownership while both are held. If foreign logical ownership remains after acquiring the pair, release the pair in reverse order, wait on only the busy monitor until its binding and claim clear, then retry. If current-thread ownership is encountered, throw rather than wait on itself. This uses the same ordering and condition signaling already used by transaction acquisition and avoids a nested transaction for one transfer.

After the one existing storage transfer succeeds, claim standalone publication ownership on both boundaries before releasing either lock. Keep both claims through Equipment's optional immediate lifecycle action and source-then-destination publication. Reacquire the pair in lock order in `finally`, clear both claims together, pulse both monitors, and unlock in reverse order.

### Transaction detection is boundary-owned

`GetSharedCurrentThreadTransaction(destination)` handles only current-thread participation: neither held returns null after rejecting self-owned retained claims; exactly one held rejects partial participation; both held resolve and compare their active transaction bindings. Foreign ownership is left to the ordered standalone wait/retry path. The existing single-boundary helper remains unchanged.

### Equipment supplies only its domain decisions

For an enlisted transaction, Equipment validates the expected item under both already-owned locks, uses the enlisted transfer, and records the existing typed effect only on full removal. For standalone operation, it supplies a validation delegate that runs under the pair locks and an immediate `beforePublication` action that runs after unlock. No lifecycle behavior enters storage or the transaction.

### Failure ordering remains local and explicit

Standalone Equipment lifecycle failure is retained while both publishers are attempted. Source publication is followed by destination publication only when the source publisher succeeds. One failure is rethrown with its original stack; multiple independent failures use a flattened `AggregateException`. Pair ownership cleanup runs in `finally`; publication never runs under either mutation lock.

## Risks / Trade-offs

- **A publisher may throw after storage has changed** → Keep the storage mutation irreversible and release both claims in `finally`; do not attempt to roll back notification failures.
- **Opposite-direction transfers could deadlock if lock order diverges** → Always use the boundaries' immutable `LockOrder`, including cleanup reacquisition.
- **A transaction may hold a retained binding while its locks are released for publication** → Release the pair before waiting and condition-wait only on the boundary with logical ownership.

## Migration Plan

No data migration is required. Update the two standalone entry points, their tests, XML documentation, and the canonical item-container storage specification. Preserve explicit grouped transactions unchanged.

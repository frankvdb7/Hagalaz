# Design

## Existing mechanisms

- `ItemContainerTransaction` acquires deterministic boundary locks, binds the transaction to boundaries, snapshots storage state, restores through its boundary, and publishes committed changes.
- `ItemContainerTransaction.Begin(...)` already waits for retained transaction bindings by releasing any acquired lock prefix and retrying ordered acquisition.
- `ItemContainerMutationBoundary` owns mutation locks, ordinary publication, transaction attribution, and mutation authorization.
- `ItemContainerStorage` contains no synchronization or transaction checks; it implements item-state algorithms.
- Equipment owns typed lifecycle completion; Shop owns stock normalization; MoneyPouch owns its composite transaction semantics.

## Decision

The internal nested `MutationScope` created by `ItemContainerMutationBoundary.BeginMutation()` makes one boundary-lock ownership decision and validates access. The scope records changes once. A transaction-bound scope forwards them directly to `ItemContainerTransaction.RecordChanges`; an unbound scope claims a boundary-owned publication marker while still holding the lock, stores publication slots, then publishes from `Dispose()` only after releasing its physical lock. The marker remains until publication and any domain-owned lifecycle effects finish. Cleanup reacquires the boundary lock, clears the marker, and pulses waiters; interrupted reacquisition is retried and reported after cleanup.

Ordinary `ItemContainer` methods use the scope and call pure storage algorithms. During rollback, the transaction retains its boundary locks and bindings while attempting every snapshot restore. The boundary validates rollback ownership before calling synchronization-free storage restoration.

Shop normalization uses an internal `ItemContainer.BeginMutation()` forwarding method so its in-place storage algorithm runs within the same scope. Hydration and restore paths use the existing boundary transaction lock or an operation scope without recording changes.

Equipment keeps lifecycle callbacks before standalone publication. Its mutation scope borrows/acquires the boundary lock and attributes transaction changes as usual, while standalone completion remains domain-owned after the lock is released.

Transfer remains transaction-only and records its source/destination change sets directly into the validated transaction. MoneyPouch composite operations remain transaction-owned and do not start ordinary scopes for already-enlisted storage.

`ItemContainerTransaction.Begin(...)` treats the standalone publication marker like a retained logical owner: same-thread overlap throws; a foreign Begin releases any acquired lock prefix, waits under the boundary lock, and retries the full deterministic order after cleanup pulses waiters. Ordinary mutations reject the marker. `RecordChanges` verifies exact boundary binding and current-thread ownership of its mutation lock. The interactive non-weapon Equipment command checks both inventory and Equipment for standalone availability before removing the incoming item or invoking the custom unequip command.

## Non-goals

No ambient context, generic callback executor, additional interfaces, transaction changes, result types, or publication pipeline. No read-side synchronization changes.

## Failure and ordering

- Scope construction releases a newly acquired monitor if access validation fails.
- Storage exceptions release a standalone-owned lock through scope disposal and do not publish unless change recording completed.
- A transaction-owned scope never releases the transaction's monitor.
- Standalone publisher exceptions propagate after unlock.
- Standalone publication ownership prevents explicit transaction binding and ordinary mutation until callbacks finish, and cleanup still clears it after publication failure or lock reacquisition interruption.
- Transaction commit, rollback, participant order, and completion failure semantics remain unchanged.
- Equipment effects continue to precede its single standalone publication and retain existing attempt-all behavior.

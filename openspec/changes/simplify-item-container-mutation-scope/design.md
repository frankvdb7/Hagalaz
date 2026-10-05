# Design

## Existing mechanisms

- `ItemContainerTransaction` already acquires deterministic storage locks, binds the transaction to storage, snapshots, restores, and publishes committed changes.
- `ItemContainerTransaction.Begin(...)` already waits for retained transaction bindings by releasing any acquired lock prefix and retrying ordered acquisition.
- `ItemContainerMutationBoundary` already owns ordinary publication and transaction attribution.
- `ItemContainerStorage.EnsureMutationAccess()` already asserts caller-owned locking and active transaction access.
- Equipment owns typed lifecycle completion; Shop owns stock normalization; MoneyPouch owns its composite transaction semantics.

## Decision

Add a small internal nested `ref struct MutationScope` created by `ItemContainerMutationBoundary.BeginMutation()`. The boundary makes one lock-ownership decision, validates access, and passes the resolved transaction into the scope. The scope records changes once. A transaction-bound scope forwards them directly to `ItemContainerTransaction.RecordChanges`; an unbound scope claims a storage-owned publication marker while still holding the lock, stores the publication slots, then publishes from `Dispose()` only after releasing its physical lock. The marker remains until publication and any domain-owned lifecycle effects finish. Its cleanup reacquires the same mutation lock, clears the marker, and pulses waiters; interrupted reacquisition is retried and reported after cleanup.

Ordinary `ItemContainer` methods use the scope directly and call lock-required storage algorithms. Storage methods retain `EnsureMutationAccess()` but drop their internal monitor acquisition. During rollback, the transaction retains its participant locks and bindings while attempting every snapshot restore. `RestoreTransactionState` checks `Monitor.IsEntered` directly; it does not reacquire the monitor or use active-transaction mutation authorization because rollback has already moved the transaction out of Active.

Shop normalization uses an internal `ItemContainer.BeginMutation()` forwarding method so its in-place storage algorithm runs within the same scope. Hydration and restore paths are audited and use the existing transaction lock or an operation scope without recording changes.

Equipment keeps lifecycle callbacks before standalone publication. Its mutation scope borrows/acquires the storage lock and attributes transaction changes as usual, while standalone completion remains domain-owned after the lock is released. This exception is limited to Equipment and will be implemented only where required by existing ordering tests.

Transfer remains transaction-only and records its source/destination change sets directly into the validated transaction. MoneyPouch composite operations remain transaction-owned and do not start ordinary scopes for already-enlisted storage.

`ItemContainerTransaction.Begin(...)` treats the standalone publication marker like a retained logical owner: same-thread overlap throws; a foreign Begin releases any acquired lock prefix, waits under the owned storage monitor, and retries the full deterministic order after cleanup pulses waiters. Ordinary mutations reject the marker. `RecordChanges` verifies both exact transaction binding and current-thread ownership of the storage mutation lock. The interactive non-weapon Equipment command checks both inventory and Equipment for standalone availability before removing the incoming item or invoking the custom unequip command.

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

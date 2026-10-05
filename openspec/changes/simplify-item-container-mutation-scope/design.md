# Design

## Existing mechanisms

- `ItemContainerTransaction` already acquires deterministic storage locks, binds the transaction to storage, snapshots, restores, and publishes committed changes.
- `ItemContainerMutationBoundary` already owns ordinary publication and transaction attribution.
- `ItemContainerStorage.EnsureMutationAccess()` already asserts caller-owned locking and active transaction access.
- Equipment owns typed lifecycle completion; Shop owns stock normalization; MoneyPouch owns its composite transaction semantics.

## Decision

Add a small internal nested `ref struct MutationScope` created by `ItemContainerMutationBoundary.BeginMutation()`. The boundary makes one lock-ownership decision, validates access, and passes the resolved transaction into the scope. The scope records changes once. A transaction-bound scope forwards them directly to `ItemContainerTransaction.RecordChanges`; an unbound scope stores the publication slots and publishes from `Dispose()` only after releasing a lock it acquired.

Ordinary `ItemContainer` methods use the scope directly and call lock-required storage algorithms. Storage methods retain `EnsureMutationAccess()` but drop their internal monitor acquisition. Transaction rollback, snapshot, binding, and teardown synchronization are unchanged.

Shop normalization uses an internal `ItemContainer.BeginMutation()` forwarding method so its in-place storage algorithm runs within the same scope. Hydration and restore paths are audited and use the existing transaction lock or an operation scope without recording changes.

Equipment keeps lifecycle callbacks before standalone publication. Its mutation scope borrows/acquires the storage lock and attributes transaction changes as usual, while standalone completion remains domain-owned after the lock is released. This exception is limited to Equipment and will be implemented only where required by existing ordering tests.

Transfer remains transaction-only and records its source/destination change sets directly into the validated transaction. MoneyPouch composite operations remain transaction-owned and do not start ordinary scopes for already-enlisted storage.

## Non-goals

No ambient context, generic callback executor, additional interfaces, transaction changes, result types, or publication pipeline. No read-side synchronization changes.

## Failure and ordering

- Scope construction releases a newly acquired monitor if access validation fails.
- Storage exceptions release a standalone-owned lock through scope disposal and do not publish unless change recording completed.
- A transaction-owned scope never releases the transaction's monitor.
- Standalone publisher exceptions propagate after unlock.
- Transaction commit, rollback, participant order, and completion failure semantics remain unchanged.
- Equipment effects continue to precede its single standalone publication and retain existing attempt-all behavior.

# Proposal

## Why

Equipment domain code currently casts `IItemContainerMutationBoundary` back to its internal implementation to check whether storage is transaction-bound. This leaks implementation knowledge and prevents the existing interface from expressing a lifecycle requirement the domain already enforces.

## What Changes

- Expose the existing outside-transaction guard as `EnsureOutsideTransaction` on `IItemContainerMutationBoundary`.
- Migrate Equipment's existing outside-transaction checks to the interface.
- Add focused tests for the guard when storage is outside and inside an active transaction.

**Non-goals:** Do not change transaction lifecycle, transfer, rollback, locking, publication, completion ordering, Equipment effects, or MoneyPouch behavior. Do not expose transaction state or add another abstraction.

## Acceptance Criteria

- Domain code calls `EnsureOutsideTransaction` through `IItemContainerMutationBoundary` without concrete casts.
- The guard succeeds outside an active transaction and throws `InvalidOperationException` when storage is bound, without changing storage or transaction state.
- Production/domain callers contain no concrete `ItemContainerMutationBoundary` casts.
- Existing transaction semantics and custom unequip behavior remain unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: mutation boundaries expose a semantic guard for operations that require storage outside an active transaction.

## Impact

The public `IItemContainerMutationBoundary` interface, its internal implementation, the Equipment domain call site, focused abstraction tests, and the item-container-storage specification. No dependency changes.

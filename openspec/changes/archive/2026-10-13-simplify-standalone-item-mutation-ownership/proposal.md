# Proposal

## Why

Ordinary standalone domain mutations use single-participant `ItemContainerTransaction` instances only to keep storage owned while the domain publishes its tracker changes. Those operations already mutate atomically, so the full-storage snapshots and rollback machinery are unnecessary.

## What Changes

- Reuse `ItemContainerMutationBoundary.MutationScope` for synchronous standalone domain mutation and publication ownership.
- Migrate straightforward Inventory, Bank, Familiar Inventory, and Shop Stock mutations away from single-participant rollback transactions.
- Preserve `ItemContainerTransaction` for operations whose several mutations or storage participants require rollback together.
- Keep publication owned by each domain and driven by the existing `ItemContainerChangeTracker`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: standalone domain mutations retain logical ownership through explicit publication without requiring rollback snapshots; caller-owned transactions remain provisional until commit.

## Impact

- Affected: `ItemContainerMutationBoundary`, `ItemContainer`, Inventory, Bank, Familiar Inventory, Shop Stock, related tests, and the canonical item-container storage specification.
- No new public API, publisher callback, tracking collection, synchronization primitive, package, or transaction abstraction.

## Scope and Acceptance

- Simple standalone domain operations do not create single-participant rollback transactions.
- The boundary releases its physical lock before publication and retains logical ownership until the scope is disposed.
- Mutations inside an existing transaction remain provisional and do not publish early.
- Existing multi-container rollback and publication ordering remain unchanged.

## Non-Goals

- No changes to publication exception handling or aggregation.
- No redesign of `ItemContainerTransaction`, MoneyPouch, Equipment lifecycle, or domain event contracts.
- No migration of operations that require atomic rollback across multiple containers or multiple dependent mutations.

## Stop Conditions

- If an ordinary operation requires partial rollback or cannot publish safely through the existing boundary, keep its transaction and record the concrete reason rather than adding another scope mechanism.
- Do not add counters or allocation instrumentation to prove transaction removal.

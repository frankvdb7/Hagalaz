# Proposal

## Why

Item-container completion currently uses a global mutation ordinal across all owners. That ordering guarantee is synthetic: production requires FIFO within each owner, while the transaction participant order already provides a clear owner order. Removing the ordinal and transaction-keyed owner queues makes completion easier to follow without changing container publication or lock ordering.

## What Changes

- Keep the optional completion owner attached to each mutation boundary.
- Derive a reference-deduplicated completion-owner sequence from resolved boundaries in participant encounter and contribution order, before lock sorting.
- Replace global completion ordinals and transaction-keyed queues with owner-local FIFO facts.
- Keep the existing pre-publication, container-publication, and post-publication phases and their failure behavior.
- Replace the synthetic cross-owner mutation-order expectation with focused FIFO, participant-order, and alias-deduplication regressions.

### Acceptance Criteria

- Each unique completion owner is invoked once in each applicable phase, in first-seen resolved-boundary order, independent of lock and mutation order.
- Facts for one owner complete in the order they were recorded.
- Completion facts are discarded on rollback or unsuccessful completion before the boundary is unbound.
- Existing equipment attempt-all behavior, pouch publication behavior, and container publication ordering remain unchanged.
- Focused tests, the solution build, strict OpenSpec validation, and `git diff --check` pass.

### Non-goals

- Do not change transaction/public API shape, locking, rollback, or publication ownership.
- Do not add generic completion/event infrastructure or phase-specific owner interfaces.
- Do not change trade, equipment, or money-pouch domain semantics beyond their completion queue implementation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: define completion-owner order independently from mutation and lock order, with FIFO retained within each owner.

## Impact

Affected code is limited to `ItemContainerTransaction`, `ItemContainerMutationBoundary`, the internal completion-owner contract, Equipment and MoneyPouch completion queues, focused regression tests, and the canonical item-container-storage specification. No dependencies or public APIs change.

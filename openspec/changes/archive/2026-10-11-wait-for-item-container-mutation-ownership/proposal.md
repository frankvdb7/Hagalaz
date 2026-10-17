# Proposal

## Why

Ordinary single-container mutations can throw when a foreign transaction or standalone publisher has released the physical lock but still retains logical ownership. These operations should wait for that existing owner to finish, matching standalone transfer behavior, while same-thread reentrancy remains invalid.

## What Changes

- Make `CurrentTransaction` the only single-boundary transaction discovery member and simplify MoneyPouch's participant comparison to use it.
- Make standalone mutation scopes wait on the boundary monitor until foreign transaction and publication ownership is cleared.
- Preserve immediate rejection of same-thread reentrancy and manually held locks without an active transaction.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: foreign ordinary mutations wait through transaction or standalone publication ownership; same-thread reentrant mutations remain rejected.

## Impact

Changes are limited to `ItemContainerMutationBoundary`, MoneyPouch transaction discovery, focused concurrency tests, and this capability's specification. No public API or transaction architecture changes.

## Non-goals

- Changing transaction construction, lock ordering, transfer atomicity, rollback, publication ordering, Equipment lifecycle, or MoneyPouch domain semantics.
- Adding transaction registries, ambient state, synchronization abstractions, or public APIs.

## Acceptance Criteria

- A foreign ordinary mutation waits until transaction or standalone publication ownership is released, including when publication throws.
- Same-thread reentrant mutations and manually held locks without a transaction still fail immediately.
- Current-thread transaction discovery stays boundary-local; MoneyPouch rejects partial participation.
- Existing transaction, rollback, transfer, Equipment, and MoneyPouch behavior remains covered by passing tests.

## Stop Conditions

- Stop if the change requires modifying transaction construction, the standalone two-boundary transfer algorithm, or domain behavior beyond synchronization and transaction discovery.

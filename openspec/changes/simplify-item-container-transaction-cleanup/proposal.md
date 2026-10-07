# Proposal

## Why

`ItemContainerTransaction` catches synchronization invariant failures and combines them with domain completion errors. That obscures the difference between failed domain work and broken lock bookkeeping, while construction and rollback also perform waiter cleanup their lifecycle does not require.

## What Changes

- Let monitor exit, pulse, and binding cleanup invariant failures propagate normally instead of collecting them with domain failures.
- Keep attempt-all aggregation for rollback restoration and domain completion where the contract requires preserving independent hook and publication failures.
- On construction failure, release acquired locks and rethrow; snapshots finish before bindings are established, so no binding cleanup or pulse is needed.
- On rollback, restore all snapshots, discard pending completion, clear bindings, and release locks without pulsing waiters; rollback retained all participant locks.
- Keep ordered post-commit binding cleanup and waiter pulses, retrying interrupted lock acquisition and surfacing one interruption when no earlier domain failure exists.
- Do not aggregate a cleanup interruption with an earlier standalone publication exception.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: clarify construction, rollback, commit cleanup, and synchronization failure behavior.

## Impact

The transaction and mutation-boundary cleanup implementations, their existing regression tests, and the canonical item-container storage specification. No public API, transaction model, or participant behavior changes.

## Non-goals

- No new completion, cleanup, transaction, or exception abstraction.
- No change to commit ordering, rollback state, domain operation semantics, or required domain failure aggregation.
- No PR, remote branch, or hosted check updates.

## Acceptance Criteria

- Construction failure releases acquired locks without attempting binding cleanup or pulsing waiters.
- Rollback attempts every snapshot restore, clears transaction bindings, releases locks, and emits no publication; it does not pulse waiters.
- Commit runs no callbacks before transaction locks are released, then clears bindings and pulses waiters before completing.
- Cleanup interruption is retried and surfaced as one exception when it is the only failure; synchronization failures are never aggregated with domain exceptions.
- Existing rollback restoration and Equipment attempt-all and hook-plus-publication aggregation remain intact.

## Stop Conditions

Stop if the cleanup changes require new public APIs, dynamic enlistment, or a generic completion/cleanup framework.

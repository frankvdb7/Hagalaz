# Proposal

## Why

The current transaction exposes execution, mutation, committed-status, and publication machinery to callers. The approved design replaces that ceremony with one synchronous disposable scope around existing domain operations.

## What Changes

- **BREAKING**: expose only `ItemContainerTransaction.Begin(participants)`, `Commit()`, and `Dispose()` as the transaction lifecycle.
- Resolve all participants before locking, capture snapshots before binding, and make construction and cleanup exception-safe.
- Reuse existing storage locks, mutation algorithms, snapshots, domain hooks, and publishers; defer notifications internally and publish automatically after irreversible commit and unlock.
- Reject incomplete, conflicting, nested, and wrong-thread participation as misuse; retain existing expected domain rejection results.
- Migrate current item, pouch, equipment, bank, familiar, shop, trade, and duel consumers. Remove execution/staging interfaces, receipts, and trade result plumbing.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: disposable transaction lifecycle, automatic publication, participation, construction safety, thread affinity, and failure sequencing.
- `trading-completion`: checked domain mutations and terminal session state before automatic commit publication.

## Impact

Abstractions, existing economic callers, and their tests change together. No dependency, persistence, protocol, worker, or coordinator is added.

## Acceptance Criteria

1. Begin validates all arguments/contributions before locking; a construction failure leaves no bindings, locks, mutations, or publication.
2. Early return and mutation exceptions restore all enlisted references, counts, and revisions on disposal; disposal is idempotent and thread-affine.
3. Commit marks storage irreversible before dropping snapshots and best-effort unbinding/unlocking every participant; no callback executes while a lock remains intentionally held.
4. Successful commit publishes once in existing observable order independently of lock order. Container failure skips later containers and all post-publication domain completion; post-publication failure skips later completion in mutation order.
5. Equipment retains owned hook ordering and attempt-all behavior; hook and publication failures survive together as original exceptions in AggregateException.
6. Helpers join only when all required storage belongs to the same originating-thread transaction; partial/conflicting current-thread participation never creates a scope or acquires missing locks. Independent operations on another thread wait through deterministic storage locks.
7. Existing pouch overflow, equipment/shop behavior, and all issue #347 terminal, retry, stale-callback, and race guarantees remain intact.

## Non-goals and Stop Conditions

Keep the approved single-scope architecture. Do not add a separate unit of work, savepoints, dynamic enlistment, generic results/events/pipelines, async transactions, compensation, or unrelated domain redesign. Stop and document any concrete requirement beyond this boundary rather than expanding it.

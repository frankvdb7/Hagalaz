# Design

## Context

See proposal.md for the motivation and specs/item-container-storage/spec.md for the required completion and failure behavior. The current transaction already retains resolved boundaries in participant encounter order and a separate sorted lock array. Each boundary already stores an optional completion owner.

## Goals / Non-Goals

**Goals:**
- Keep resolved boundary order for storage publication and derive a separate unique completion-owner order from it.
- Keep deterministic lock order independent from both observable orders.
- Keep pending completion facts local to their domain owner and drain or discard them before the transaction releases boundary ownership.

**Non-Goals:**
- Change public transaction API, transaction state transitions, locking, snapshots, rollback, or publication ownership.
- Add generic completion infrastructure, dynamic enlistment, or cross-owner mutation interleaving.

## Decisions

- Retain `ItemContainerMutationBoundary`'s optional owner association. Resolve the owner array from the already deduplicated boundary array before sorting locks; deduplicate owners by reference. This avoids another owner registry and makes participant/contribution order explicit.
- Keep `_boundaries`, `_lockOrder`, and completion owners as distinct orderings. Publication continues over `_boundaries`; lock acquisition/release continues over `_lockOrder`; each completion phase iterates the owner array.
- Replace the transaction ordinal with owner-local FIFO queues. Equipment drains all its batches and attempts every effect before surfacing failures, preserving its domain policy. MoneyPouch drains its own facts in order and stops at the first publication failure, preserving transaction post-publication semantics.
- Enqueue under the owning boundary's mutation lock and verify the active transaction is the boundary's binding. Equipment obtains the existing mutation scope when its current call site is outside a boundary mutation; MoneyPouch already enqueues inside one.
- Parameterless owner phase methods operate on the one pending FIFO protected by the owner's transaction-bound boundary. The transaction still discards every unique owner's pending facts before unbinding boundaries.

## Risks / Trade-offs

- **Owner order now follows participant/contribution order rather than interleaved mutations** → This is the approved structural order; production requires FIFO only within each owner, and regressions explicitly distinguish it from lock and mutation order.
- **Plain queues are not independently concurrent** → Enqueue requires the owning boundary lock and active transaction; completion is synchronous on the originating thread after transaction locks are released, while the boundary remains bound and rejects new overlapping mutation.

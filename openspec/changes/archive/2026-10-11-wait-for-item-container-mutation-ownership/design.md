# Design

## Context

See `proposal.md` and the `item-container-storage` delta. The mutation boundary already owns the monitor, transaction binding, standalone publication owner, and waiter notifications. `CurrentTransaction` is the current-thread discovery point.

## Goals / Non-Goals

**Goals:** Wait on the existing boundary monitor while foreign transaction or publication ownership remains. Keep same-thread reentrancy invalid and preserve transaction-bound lock authorization.

**Non-Goals:** Change transaction creation, transfer lock ordering, rollback, publication order, or domain operation semantics.

## Decisions

- Keep one `CurrentTransaction` property. Remove the forwarding getter and have MoneyPouch compare the current transaction from its pouch, inventory, and optional source boundaries directly. This avoids maintaining a second discovery entry point.
- When `BeginMutation` acquires an unheld boundary monitor, recheck both ownership fields under that monitor. Throw for ownership held by the current thread; use `Monitor.Wait` for foreign ownership so the boundary's existing cleanup can reacquire the monitor and pulse. Once ownership clears, retain existing mutation authorization and scope construction.
- When the caller already holds the monitor, do not wait. Require `CurrentTransaction` to identify the active current-thread transaction; a manually held monitor remains invalid.
- Keep publication and transaction cleanup unchanged. Existing `PulseAll` calls release waiting mutations after successful or failed publication and after transaction binding cleanup.

Alternatives rejected: throwing on all logical ownership preserves transient failures for ordinary container operations; polling or adding another synchronization primitive duplicates the boundary's existing monitor and notifications.

## Risks / Trade-offs

- A foreign ordinary mutation may block for the duration of a synchronous callback. This matches existing transaction/transfer contention and prevents it from observing or changing storage before publication completes.
- A missed pulse could leave a waiter blocked; tests cover normal cleanup and publisher failure, and existing ownership-release paths already pulse under the monitor.

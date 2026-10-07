# Design

## Context

See proposal.md for motivation and specs/item-container-storage/spec.md for the updated cleanup behavior. Transaction construction captures all snapshots before binding participants. Rollback retains all participant locks; commit releases them before callbacks and reacquires them to clear committed bindings.

## Goals / Non-Goals

**Goals:**
- Keep transaction cleanup small and preserve the separation between domain failures and broken synchronization invariants.
- Retain all-snapshot rollback restoration, Equipment attempt-all behavior, and required hook/publication aggregation.
- Ensure waiters are signaled only after committed or standalone logical ownership is released.

**Non-Goals:**
- Redesign transaction participation, completion ownership, callback ordering, or public APIs.
- Add recovery infrastructure for impossible Monitor bookkeeping failures.

## Decisions

- Construction failure releases its acquired lock prefix and rethrows. Snapshot capture precedes all binding, so there is no binding cleanup or waiter pulse during construction.
- Rollback still attempts every snapshot restore and aggregates restoration failures. It then discards pending owner facts, clears bindings, and releases locks. It does not pulse: contenders cannot enter a participant monitor while rollback owns it, and see the cleared binding after lock release.
- Commit releases mutation locks directly before invoking owner completion or publishers. A failed `Monitor.Exit` propagates as an invariant failure and prevents callbacks from starting. Lock bookkeeping decrements only after each successful exit.
- Commit cleanup retries `ThreadInterruptedException` while reacquiring ordered boundary locks, then clears bindings, pulses waiters, and releases those locks. It surfaces one recovered interruption after cleanup only when no earlier domain completion/publication failure exists.
- Completion and publication failures keep their current policy: Equipment attempts all owned effects, normal publication stops at its first failure, and required independent hook/publication failures remain aggregated. Cleanup failures are not appended to those domain failures.
- Standalone publication uses the same first-failure rule: retry interrupted cleanup-lock acquisition and always release ownership; propagate the earlier publisher exception directly if one occurred, otherwise surface the recovered interruption. Monitor exit, pulse, and ownership invariant failures propagate directly.
- Pending owner facts are cleared through the existing completion-owner contract. Concrete queue clearing is not treated as a recoverable external callback.

## Risks / Trade-offs

- **Synchronization bookkeeping is already inconsistent** → Propagate the monitor invariant failure directly and do not run external callbacks; do not obscure it inside an aggregate.
- **Thread interruption arrives during cleanup lock acquisition** → Retry until transaction or publication ownership can be cleared, then surface one interruption only when an earlier domain failure does not already explain the operation failure.
- **Rollback restoration fails for multiple participants** → Continue attempting every restore and aggregate those independent restoration failures after cleanup.

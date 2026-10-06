# Retain Item Container Scope Through Publication

## Problem

`ItemContainerTransaction.Commit()` currently clears boundary bindings when it releases mutation locks, before committed hooks and publishers run. A new transaction can therefore mutate overlapping live storage while the earlier transaction is still publishing it.

## Scope

Keep the existing transaction API, participants, live storage, boundary lock ordering, and domain-owned completion facts. Separate mutation-lock release from transaction-scope release. Retain boundary bindings through committed completion, publication, and pending-fact cleanup; wait for foreign overlapping `Begin(...)` calls on the existing boundary lock; reject same-thread reentrant overlap. Validate mutation access and attribute changes to the boundary-bound transaction under each boundary lock; publish standalone changes only after unlocking. Make committed teardown interruption-safe without clearing bindings outside their locks. Require terminal trade staging to use exact operations that prove all relevant boundaries participate in the caller-owned scope.

## Acceptance Criteria

- `Commit()` releases mutation locks before any domain hook or publisher while retaining every participant binding until completion and pending cleanup finish.
- A foreign overlapping `Begin(...)` waits and can proceed only after the complete prior scope releases its bindings; partial lock prefixes are released before waiting to preserve deterministic multi-storage teardown.
- Same-thread overlapping `Begin(...)` and ordinary mutation during committed completion are rejected.
- Scope bindings are cleared together under ordered locks, waiters are pulsed, failures never roll back committed storage, and publication is never retried.
- Rollback restores snapshots before clearing bindings and releasing locks.
- The mutation boundary validates mutation access and attributes changes under its lock; storage algorithms remain synchronization-agnostic. Standalone publication happens only after unlocking; ordinary operations never carry transaction identity or use ambient transaction accessors.
- Committed teardown never clears a binding without the corresponding lock, retries `ThreadInterruptedException`, and propagates it only after all bindings and locks are cleaned up.
- MoneyPouch coin transfer and terminal TradeExchange movement require the complete caller-owned transaction and cannot open nested scopes through standalone-capable additions or clears.
- Rollback metadata guarantees remain limited to slot topology, item references, counts, and storage revision; arbitrary `ExtraData` is not deep-copied.
- Public transaction and participant APIs, transfer semantics, pouch semantics, and equipment ordering remain unchanged.
- Targeted concurrency regression, affected tests, build, strict OpenSpec, duplication gate, and diff checks pass.

## Non-Goals

- New public transaction APIs, ambient state, enlistment, wait abstractions, or async support.
- Changes to the established transfer algorithm, participant contracts, MoneyPouch facts, or equipment completion policy.
- Commits, pushes, or remote PR updates.

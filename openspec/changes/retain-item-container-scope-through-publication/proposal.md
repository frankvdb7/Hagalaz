# Retain Item Container Scope Through Publication

## Problem

`ItemContainerTransaction.Commit()` currently clears storage bindings when it releases mutation locks, before committed hooks and publishers run. A new transaction can therefore mutate overlapping live storage while the earlier transaction is still publishing it.

## Scope

Keep the existing transaction API, participants, live storage, lock ordering, and domain-owned completion facts. Separate mutation-lock release from transaction-scope release. Retain bindings through committed completion, publication, and pending-fact cleanup; wait for foreign overlapping `Begin(...)` calls on the existing storage monitor; reject same-thread reentrant overlap.

## Acceptance Criteria

- `Commit()` releases mutation locks before any domain hook or publisher while retaining every participant binding until completion and pending cleanup finish.
- A foreign overlapping `Begin(...)` waits and can proceed only after the complete prior scope releases its bindings; partial lock prefixes are released before waiting to preserve deterministic multi-storage teardown.
- Same-thread overlapping `Begin(...)` and ordinary mutation during committed completion are rejected.
- Scope bindings are cleared together under ordered locks, waiters are pulsed, failures never roll back committed storage, and publication is never retried.
- Rollback restores snapshots before clearing bindings and releasing locks.
- Public transaction and participant APIs, transfer semantics, pouch semantics, and equipment ordering remain unchanged.
- Targeted concurrency regression, affected tests, build, strict OpenSpec, duplication gate, and diff checks pass.

## Non-Goals

- New public transaction APIs, ambient state, enlistment, wait abstractions, or async support.
- Changes to transfer algorithms, participant contracts, domain callers, MoneyPouch facts, or equipment completion policy.
- Commits, pushes, or remote PR updates.

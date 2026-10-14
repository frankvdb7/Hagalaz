# Proposal

## Why

Domain containers currently require callers to publish committed item changes manually. A forgotten call leaves server storage correct while clients or domain consumers remain stale, and the repeated orchestration obscures the transaction boundary.

## What Changes

- Registered domain containers automatically complete successful standalone outer mutation scopes and committed transactions through the existing completion-owner contract.
- Successful standalone cross-container transfers complete each distinct registered owner while retaining ownership of both boundaries.
- Inventory, Bank, Reward, Familiar Inventory, Equipment, MoneyPouch, and Shop Stock own their existing domain effects and notifications; raw temporary `ItemContainer` instances retain pull-based tracking.
- Remove obsolete manual publication APIs and redundant production call sites while preserving initial refreshes, suppressed tracking, domain events, and ordering.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: registered domain owners automatically complete committed changes after physical locks are released and before logical ownership is released; raw containers remain manually consumable.

## Impact

The change affects item-container completion contracts and boundaries in Game Abstractions, the listed GameWorld domain containers, Shop, and Trade/Duel gameplay call sites. It adds no dependencies and reuses the existing change tracker, mutation boundary, lock set, transaction, and domain event types.

## Non-goals

- No tick-based batching, generic event system, publisher abstraction, unit of work, new lock, or standalone rollback snapshots.
- No changes to item semantics, event types, gameplay validation, or unrelated network/gameplay flows.

## Acceptance Criteria

- Registered owners publish exactly once after successful standalone mutation, transfer, or transaction commit, with no callback under a physical mutation lock and no mutation interleaving before publication ends.
- Transaction rollback publishes nothing; committed storage is never rolled back by completion failures.
- Equipment effects precede inventory notifications and equipment publication; Inventory notifications precede MoneyPouch notifications.
- Ownerless temporary containers remain pull-based, and production gameplay does not require manual `PublishChanges()` calls.
- Focused tests, directly affected project tests, solution build, strict OpenSpec validation, duplication checks, and `git diff --check` pass.

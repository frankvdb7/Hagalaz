# Proposal

## Why

The item-container layer currently permits a thread that already owns storage to wait on another boundary, which can form a cross-container publication deadlock. A few failure and no-op paths also leave item recovery, command participation, or storage revision behavior less explicit than the domain requires.

## What Changes

- Prevent blocking acquisition of additional container storage when the current thread already owns a transaction or standalone publication, while preserving normal waiting and uncontended disjoint scopes.
- Restore an incoming item when interactive Equipment replacement fails before it is equipped.
- Make MoneyPouch inventory commands explicitly standalone-only and reject caller-owned participation before doing work.
- Make clearing an already-empty storage a true no-op.
- Correct the PR description to describe the current standalone transfer and transaction behavior.

### Scope

One correctness pass over existing item-container synchronization and domain operations. Existing transaction, boundary, publication, and container APIs remain in place.

### Non-goals

- Redesigning transactions or standalone transfers.
- Adding public APIs, transaction registries, coordinators, or generic recovery mechanisms.
- Changing weapon/shield Equipment replacement, MoneyPouch composable operations, or trade terminal lifecycle.
- Claiming item conservation if every existing recovery destination rejects an item.

### Acceptance Criteria

- A thread that already owns storage fails promptly instead of waiting on a contended additional boundary; a thread with no such ownership still waits normally.
- Ownership accounting is cleared after commit, rollback, and publisher failure.
- Interactive Equipment replacement restores the same item instance through the documented destination order when the operation fails before equip, and never overwrites or duplicates it.
- MoneyPouch standalone gameplay commands reject current-thread transaction participation before validation, messages, or mutation.
- Clearing empty storage leaves contents, revision, and publication unchanged; existing nonempty and zero-count-sentinel behavior remains intact.
- Focused regression tests, required local validation, and a current PR description are completed without commit or push.

### Stop Conditions

- If the ownership guard requires a new transaction-discovery mechanism or changes existing transaction/transfer architecture, stop and reassess.
- If Equipment recovery requires a new public API or generic compensation framework, stop and reassess.
- If a required behavior cannot be expressed with existing boundaries, containers, and persistent recovery destinations, report the limitation rather than broadening scope.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: clarify safe acquisition while the current thread owns storage, interactive Equipment recovery, standalone MoneyPouch commands, and empty-clear behavior.

## Impact

Affected code is limited to existing item-container mutation boundaries, explicit transaction acquisition, Equipment replacement, MoneyPouch gameplay commands, storage clearing, their tests, the `item-container-storage` OpenSpec behavior, and the PR #516 description. No dependency or public API changes are planned.

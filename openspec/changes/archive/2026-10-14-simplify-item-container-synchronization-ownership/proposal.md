# Proposal

## Why

`ItemContainerMutationBoundary` currently combines per-storage access ownership, write-scope lifetime, change tracking coordination, transfer orchestration, and standalone completion. `ItemContainerLockSet` also interprets transaction and publication state. This makes the rules hard to locate and increases the number of places that can disagree about whether a storage may be accessed.

## What Changes

- Replace the mutation boundary with one focused per-storage synchronization owner and a named write scope.
- Keep transaction state, snapshots, rollback, change promotion, and completion order in `ItemContainerTransaction`.
- Keep deterministic multi-storage lock coordination in `ItemContainerLockSet`, delegating access authorization to synchronization owners.
- Move the shared atomic transfer orchestration out of the synchronization owner while retaining one implementation.
- Preserve domain-owned completion, tracker behavior, public APIs, and all current synchronization and publication semantics.

### Non-goals

- Change item storage algorithms, item reads, domain gameplay rules, public container contracts, trade behavior, or transaction semantics.
- Add async synchronization, new lock frameworks, event/observer systems, generic coordinators, or compatibility adapters.

## Acceptance Criteria

- There is one synchronization authority per storage; `ItemContainerMutationBoundary` is removed.
- A write scope represents one operation and preserves nested-write and standalone completion behavior.
- Lock-set code does not inspect transaction or publication state.
- Transactions remain the sole owner of snapshots, commit/rollback, and transaction-wide completion ordering.
- Transfers use one shared atomic orchestration path.
- Existing observable item, tracker, publication, and concurrency behavior remains unchanged.
- Required tests, solution build, OpenSpec validation, duplication check, warning comparison, and diff check pass.

## Stop Conditions

- Stop and reassess if the target split requires a second ownership store, changes a public API, or cannot preserve an existing synchronization guarantee.
- Record unrelated findings as follow-up work rather than expanding this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: clarify per-storage synchronization ownership and its relationship to write scopes, transfers, transactions, and tracker access.

## Impact

The internal item-container implementation in Game.Abstractions, its GameWorld composition sites, and synchronization/publication tests. No new dependencies or public API changes are intended.

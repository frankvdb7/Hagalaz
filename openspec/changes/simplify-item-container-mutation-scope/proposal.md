# Proposal

## Why

Ordinary item-container mutations currently acquire the same monitor in both the aggregate and storage algorithm, and each caller carries transaction/publication branching. A single per-operation scope can own the monitor once and attribute changes before unlock while preserving the existing transaction behavior.

## What Changes

- Add a narrow internal `MutationScope` owned by `ItemContainerMutationBoundary` for lock ownership, transaction attribution, and standalone publication after unlock.
- Make ordinary storage mutators require caller-owned mutation-lock access instead of acquiring the monitor themselves.
- Make rollback restoration require the transaction's already-owned lock without reacquiring it or using active-mutation authorization.
- Migrate ordinary `ItemContainer`, Shop, and Equipment mutation paths to the scope; keep specialized transaction-only MoneyPouch and transfer behavior.
- Preserve equipment completion order, transaction behavior, hydration semantics, and rollback infrastructure.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: specify caller-owned mutation locking and operation-scope attribution/publication behavior.

## Impact

Affected areas are `ItemContainerStorage`, `ItemContainerMutationBoundary`, `ItemContainer`, Equipment, Shop stock normalization, MoneyPouch transaction code, item-container tests, and the existing storage specification. No dependencies, public interfaces, or transaction architecture changes are intended.

## Acceptance Criteria

- A standalone operation enters its storage monitor once; a transaction-owned operation borrows the lock without entering or releasing it.
- Storage mutation algorithms verify caller lock ownership and do not lock themselves.
- Transaction rollback restores every snapshot while retaining participant locks and bindings, then clears bindings and releases locks.
- Change attribution occurs under the lock; standalone publication occurs after unlock; failed operations publish nothing.
- Equipment ordering and all existing transaction, MoneyPouch, transfer, hydration, and trade semantics remain unchanged.
- Focused tests, solution build, full serial tests, strict OpenSpec validation, duplication gate, and `git diff --check` are run and reported.

## Non-goals

- Redesigning or renaming transaction, storage, or mutation-boundary types.
- Ambient transaction access, nested transactions, dynamic enlistment, or generic mutation execution frameworks.
- Changing domain operation semantics, result types, equipment lifecycle policy, or transaction completion ordering.

## Stop Conditions

Stop and reassess if preserving equipment callback/publication ordering requires a new generic completion mechanism, if production callers require a second transaction ownership model, or if any unrelated domain behavior must change to satisfy the mutation-scope contract.

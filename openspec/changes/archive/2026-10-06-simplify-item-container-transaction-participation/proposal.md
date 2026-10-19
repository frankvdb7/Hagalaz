# Proposal

## Why

Transaction membership currently requires callers to navigate a separate mutation property. This leaks implementation composition into domain call sites and makes a container's transaction capability less direct than its other operations.

## What Changes

- Introduce the public empty capability marker `IItemTransactional`.
- Make `IItemContainer` and `IMoneyPouchContainer` inherit that marker and remove their public `Mutations` properties.
- Make `ItemContainer` and `MoneyPouchContainer` internal `IItemTransactionSource` implementations; the pouch source contributes its own storage and its inventory storage.
- Accept the domain objects directly in `ItemContainerTransaction.Begin(...)`, preserving eager mutation, deterministic locking, rollback, and post-commit publication.
- Resolve transfer destinations through the internal source contract, without requiring a concrete `ItemContainer`.
- Migrate callers and tests, then reconcile the canonical and active delta specifications.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: transaction membership is declared with domain objects that implement `IItemTransactional`.

## Non-goals and acceptance criteria

Non-goals: transaction-engine redesign; dynamic enlistment; ambient transactions; new transaction lifecycle phases; changes to item transfer semantics; public storage or boundary access; public transaction-state access; friend-assembly access for Scripts; remote PR operations.

Acceptance criteria: direct aggregate arguments work with `Begin`; public `Mutations` properties and the old participant interfaces are removed; ordinary container transfers support interface decorators backed by the internal source contract; existing storage, locking, rollback, publication, pouch, equipment, shop, and trade behavior remains unchanged; targeted tests, build, strict OpenSpec validation, jscpd, and diff checks are run and reported.

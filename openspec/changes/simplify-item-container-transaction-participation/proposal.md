# Proposal

## Why

Ordinary container mutations currently use a separate `.Mutations` operation surface and reject storage enlisted in a transaction. This duplicates domain APIs and makes callers use different mutation syntax for the same container operation.

## What Changes

- Make `ItemContainerTransaction.Begin(...)` the explicit source of storage membership; ordinary mutations on enlisted storage participate automatically and defer publication through the existing storage notification path.
- Keep ordinary mutations and atomic `TryTransferTo` on `IItemContainer`; expose `.Mutations` only as an opaque transaction participant.
- Make MoneyPouch exact operations use their normal public methods, joining a complete existing transaction or owning a transaction when all required storage is unbound.
- Allow simple Equipment storage mutations to participate in an existing scope and defer lifecycle effects through existing completion ownership.
- Migrate callers, replace rejection tests with automatic-participation tests, and update the item-container behavior specification.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: transaction membership and ordinary mutation participation behavior changes.

## Impact

Affected areas are the public item-container contracts, `ItemContainer` and its mutation boundary, MoneyPouch and Equipment containers, composed Trade/Duel/Bank/Shop/Reward/Familiar workflows, their tests, and the current item-container storage specification. The existing storage access checks, transaction binding, snapshot/rollback, publication deferral, MoneyPouch completion facts, and Equipment completion owner are reused. No new dependency or transaction abstraction is introduced.

## Non-goals and acceptance criteria

Non-goals: dynamic enlistment; ambient transaction services; new transaction wrappers or state APIs; changes to storage algorithms or item identity/count semantics; broad changes to transaction-owning Equipment workflows; changing `TryTransferTo` requirements; or remote PR operations.

Acceptance criteria: enlisted ordinary mutations publish once only after commit and roll back on uncommitted disposal; unenlisted mutations remain standalone; MoneyPouch and simple Equipment preserve their specified completion semantics; obsolete mutation APIs and terminology are removed; targeted tests, build, strict OpenSpec validation, jscpd, and diff checks are run and reported.

Stop condition: stop if the existing transaction binding/publication mechanism cannot provide these semantics without an additional transaction abstraction or a change to unrelated domain behavior.

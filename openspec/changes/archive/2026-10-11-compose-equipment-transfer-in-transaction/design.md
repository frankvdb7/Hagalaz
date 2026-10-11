# Design

## Context

See proposal.md for motivation. `ItemContainer.TryTransferTo()` already owns a short transaction when neither boundary is held by the current thread, joins when both are held, and rejects partial participation. `ItemContainerMutationBoundary.TryTransferTo()` verifies that both boundaries share the same active transaction and records their changes. `EquipmentContainer.TryMoveTo()` currently bypasses that ownership decision by always calling `Begin()`.

## Goals / Non-Goals

**Goals:**

- Give `TryMoveTo()` the same standalone-or-participating behavior as the existing exact transfer operation.
- Preserve typed Equipment completion for full removal and let the owning transaction decide when to commit or roll back.
- State clearly that `EquipItem()` and `UnEquipItem()` are standalone gameplay commands.

**Non-Goals:**

- Make `EquipItem()` or `UnEquipItem()` join an existing transaction.
- Add generic transaction-joining infrastructure or change transaction APIs.
- Change transfer validation, lifecycle ordering, or the read-only Equipment item view.

## Decisions

- Resolve the destination boundary, then compare current-thread lock ownership for Equipment and destination, following `ItemContainer.TryTransferTo()`. A one-boundary match is rejected before mutation.
- When both boundaries are already held, obtain the current transaction from the Equipment boundary and call its existing exact transfer primitive. That primitive verifies destination ownership. Enqueue the existing typed unequip effect only after a successful full removal, and do not commit or dispose the caller's scope.
- When neither boundary is held, keep the existing `Begin(this, destination)` standalone path, transfer, enqueue the effect, and commit only on success.
- Do not add a shared helper: this is one call site and the existing `ItemContainer` implementation is the behavioral reference.
- Document `EquipItem()` and `UnEquipItem()` as standalone operations on the public interface. Their current script validation and command flows remain unchanged.

## Risks / Trade-offs

- **Partial or conflicting scope:** The lock-ownership check rejects partial participation, while the boundary transfer verifies both active transaction identities before mutation.
- **Caller rollback:** Equipment completion and publication remain deferred on the same transaction; disposing without commit restores storage and discards the queued effect.

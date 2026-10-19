# Design

## Context

Production `InventoryContainer` owns one `ItemContainer`, and `Character` constructs its Inventory before MoneyPouch. See the proposal and spec delta for the behavior being changed.

## Goals / Non-Goals

**Goals:**
- Keep MoneyPouch's transaction contribution fixed to its own storage and the inventory item storage.
- Let current-thread boundary ownership decide whether an exact operation participates or starts its own transaction.

**Non-Goals:**
- Generalize transaction enlistment for arbitrary inventory aggregates.
- Change the caller-owned transaction required by `TryTransferCoinsFrom`.

## Decisions

- Capture the single inventory boundary in the MoneyPouch constructor by reusing `ItemContainerTransaction.ResolveSingleBoundary`.
- Expose a narrow internal boundary query that returns its active transaction only when the current thread holds that boundary lock. It reuses the existing active-transaction validation; a foreign transaction is observed as no current-thread ownership.
- MoneyPouch compares the returned transaction for its two fixed boundaries and optional source boundary. Neither owned means exact add/remove uses its existing `Begin(this)` path, which handles foreign contention. Partial or mismatched current-thread ownership throws before mutation.
- Keep the source coin-transfer path caller-owned: it requires all three boundaries to report the same active current-thread transaction and never starts a scope.
- Replace the artificial multi-boundary inventory fixtures with a direct two-boundary enlistment/rollback regression. Keep the existing partial and conflicting transaction regressions.

## Risks / Trade-offs

- **An inventory implementation has no single transaction boundary** → fail at MoneyPouch construction through the existing single-boundary resolver; production `InventoryContainer` satisfies the contract.
- **A foreign transaction overlaps an exact pouch operation** → do not classify its binding as a partial current-thread transaction; let `Begin(this)` wait and proceed after it releases ownership.

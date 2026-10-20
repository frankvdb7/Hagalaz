## Context

`ShopStockContainer.BuyFromShop` currently calls the partial-removal API and accepts any positive result as payment. `MoneyPouchContainer` already has a checked `RemoveForTrade` operation that validates the combined pouch and inventory balance before mutating either container. The shop needs that same behavior without depending on trade-specific naming. Non-coin shop currencies live in the inventory and must use the inventory's existing checked removal operation.

The purchase flow remains the owner of the payment decision. The money pouch owns coin validation and mutation, while the inventory owns alternative-currency validation and mutation. Stock and item delivery remain unchanged by this issue. Full cross-container purchase rollback belongs to #449.

## Goals / Non-Goals

**Goals:**

- Reject every paid purchase unless the complete calculated cost is removed.
- Preserve all currency when the player cannot pay the complete cost.
- Allow coins split between the money pouch and inventory to pay exactly.
- Keep free/sample purchases free and remove non-coin currencies exactly from inventory.
- Add a domain-neutral `TryRemoveExact` money-pouch contract that delegates to the existing checked removal algorithm.

**Non-Goals:**

- Do not redesign item-container atomicity from #434 or #437.
- Do not make stock removal and inventory delivery atomic with payment. That is #449.
- Do not add locks, queues, transaction objects, coordinators, async APIs, or race handling.

## Decisions

1. **Use `TryRemoveExact` as the shop-facing money operation.** The method will delegate to the existing `RemoveForTrade` implementation, so there is one checked pouch-plus-inventory removal algorithm. Keeping `RemoveForTrade` preserves the current trade contract; adding a domain-neutral name avoids coupling shop code to trade terminology. A second removal implementation or a direct `Remove` followed by an equality check was rejected because both would either duplicate logic or destroy partial currency before failure.

2. **Select the payment owner by currency type.** Coin currency (`995`) uses the money pouch exact operation, which also checks inventory overflow coins. Every other currency uses `IInventoryContainer.RemoveForTrade` with the exact requested item. Reusing `MoneyPouch.Contains` as the branch condition was rejected because that method reports inventory fallback semantics and cannot distinguish a coin payment from an alternative currency.

3. **Keep the existing purchase ordering and checks.** Stock selection, count limiting, capacity checks, price overflow checks, restrictions, and delivery stay in `BuyFromShop`. The payment gate returns before stock or inventory mutation when exact removal fails. A broader rollback coordinator is intentionally deferred to #449.

## Risks / Trade-offs

- [Risk] Adding a method to `IMoneyPouchContainer` requires test doubles to implement it. → Update the existing deterministic money-pouch test double by delegating to its checked removal path; no production adapter is needed.
- [Risk] Payment can still succeed before a later stock or inventory mutation fails. → This existing atomicity gap is explicitly outside issue 441 and tracked by #449; this change only closes underpayment authorization.
- [Risk] The shop depends on the coin item ID. → `995` is the established coin ID used by the money pouch and existing shop flow; no new currency registry is introduced.

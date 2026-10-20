## Why

`ShopStockContainer.BuyFromShop` treats any positive result from the money pouch as full payment. When the player has less than the price, the pouch can remove the player's remaining coins and the shop still grants the item. This is a direct economy exploit and needs a narrow payment-flow fix before broader shop atomicity work in #449.

## What Changes

- Require a paid shop purchase to remove the exact calculated cost before the item is granted.
- Reuse the money pouch's existing checked removal behavior for shop payments, exposing only the smallest domain-neutral contract adjustment needed by the shop.
- Add deterministic MSTest coverage for underpayment, exact payment, overpayment, pouch plus inventory coins, free/sample stock, and non-coin currencies.
- Preserve stock limits, inventory checks, shop restrictions, and all existing free and alternative-currency behavior.

### Non-goals and stop conditions

- Do not implement the broader container changes in #434 or #437.
- Do not implement full purchase atomicity from #449.
- Do not add a transaction framework, coordinator, lock, async container API, or concurrency behavior.
- If the fix requires those excluded changes, stop and record the blocker instead of expanding this change.

## Capabilities

### New Capabilities

- `shop-purchase-payment`: A paid shop purchase authorizes item delivery only after exact currency removal, while free/sample purchases remain free and alternative shop currencies retain exact-payment semantics.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Logic/Shops/ShopStockContainer.cs` will use exact money removal for the payment gate.
- `Hagalaz.Game.Abstractions/Collections/IMoneyPouchContainer.cs` and its implementation may gain the smallest domain-neutral exact-removal contract needed by the shop.
- Focused shop and money-pouch tests will cover the payment boundary. No database, messaging, dependency, or public network protocol changes are expected.

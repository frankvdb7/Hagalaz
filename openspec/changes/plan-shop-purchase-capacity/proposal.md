## Why

`ShopStockContainer.BuyFromShop` clamps or rejects a purchase using the inventory's pre-payment `FreeSlots`. Exact payment can empty an inventory currency stack and release a slot, so a valid purchase is rejected or reduced too far (issue #449). The atomic `ItemContainerTransaction` boundary from #515 already exists and is kept as-is.

## What Changes

- Determine one final purchase quantity against the effective post-payment inventory state.
- Count only inventory currency slots that exact payment completely empties (pouch-first for coins; partial stacks stay occupied; sample stock releases nothing).
- Use that quantity for cost, payment, stock removal, inventory addition and `ShopItemBoughtEvent`.
- Preserve partial purchases, messaging, alternative currencies and sample stock.

### Non-goals

- No new transaction, lock, strategy or planner abstraction; no pricing, restock, `SellFromInventory` or UI changes.
- Execution model: shop clicks run on the character's task queue (`ComponentHub.OnComponentClick` -> `QueueTask`), so one character's purchases are serialized; cross-character stock access is covered by the existing transaction lock ordering. No shop-level concurrency machinery is added.

## Capabilities

### Modified Capabilities

- `shop-purchase-payment`: purchase quantity accounts for capacity released by exact payment.

## Impact

- `Hagalaz.Services.GameWorld/Logic/Shops/ShopStockContainer.cs` (private planning near `BuyFromShop`).
- `Hagalaz.Services.GameWorld.Tests/ShopStockContainerTests.cs`.

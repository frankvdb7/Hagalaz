## Context

#515 made `BuyFromShop` transactional. Quantity planning still used `Inventory.Items.FreeSlots` before payment.

## Decisions

1. **Shop-local planning, no new abstraction.** `BuyFromShop` evaluates `Fits(quantity)`: slots needed (`quantity` for non-stackable, 1 or 0 for stackable) <= `FreeSlots` + slots emptied by paying `unitPrice * quantity`.
2. **Emptied slots mirror removal.** `CountSlotsEmptiedByPayment` subtracts the pouch balance first for coins, then walks inventory slots in order like `TryRemoveExact`, counting a slot only when its whole count is consumed.
3. **Non-stackable requests shrink** one at a time until they fit (preserves existing partial-purchase behaviour). Stackable requests are all-or-nothing on capacity since fewer items need no fewer slots.
4. **Cost uses `long`** and is rejected above `int.MaxValue` before any mutation.
5. **Transaction stays authoritative.** Planning is a pre-check; payment and transfer still revalidate and roll back on failure.

## Risks

- A planned release can be wrong if state changes between planning and commit; the transaction then rejects and rolls back, leaving storage unchanged.

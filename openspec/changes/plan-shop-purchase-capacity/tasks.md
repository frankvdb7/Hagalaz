## 1. Implementation

- [x] 1.1 Plan one final purchase quantity against post-payment inventory capacity in `ShopStockContainer.BuyFromShop`.
- [x] 1.2 Add `CountSlotsEmptiedByPayment` mirroring pouch-first coin payment and in-order inventory removal.

## 2. Tests

- [x] 2.1 Cover released capacity for coins (exact, partial, split, pouch-only) and alternative currency (exact, partial, multi-slot).
- [x] 2.2 Cover partial quantity, cost coupling, event count, stackable stacks, overflow, sample stock, repeated calls and integer boundaries.

## 3. Verification

- [x] 3.1 Run the GameWorld test project.
- [x] 3.2 Run the full solution build (jscpd not run).

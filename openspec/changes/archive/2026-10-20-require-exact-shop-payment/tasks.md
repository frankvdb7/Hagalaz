## 1. Exact payment implementation

- [x] 1.1 Add the domain-neutral `TryRemoveExact` money-pouch contract and delegate it to the existing checked pouch-plus-inventory removal algorithm, updating affected test doubles.
- [x] 1.2 Change `ShopStockContainer.BuyFromShop` to authorize coin purchases only after exact money-pouch removal and non-coin purchases only after exact inventory removal, without changing stock, capacity, restriction, or sample-stock checks.

## 2. Regression coverage

- [x] 2.1 Add deterministic MSTest coverage for one-coin and `cost - 1` underpayment with currency preservation, exact payment, overpayment, split pouch and inventory coins, free/sample stock, and exact non-coin currency payment.
- [x] 2.2 Run the focused shop and money-pouch tests, validate the OpenSpec change, and review the final diff for excluded #434, #437, and #449 work.

## 1. Regression Coverage

- [x] 1.1 Locate the existing `MoneyPouchContainer` test fixture and add direct containment cases for pouch-only, inventory-only, combined, insufficient, and exact combined coin balances; verify the focused test class runs.
- [x] 1.2 Add a large pouch-plus-inventory balance case whose mathematical sum exceeds `int.MaxValue`; verify the containment result is correct and does not overflow.

## 2. Containment Fix

- [x] 2.1 Update the coin branch of `MoneyPouchContainer.Contains` to read pouch and eligible inventory balances, combine them in an overflow-safe intermediate type, and compare with the request; verify all focused containment tests pass.
- [x] 2.2 Confirm non-coin containment and all mutation/payment/notification paths remain unchanged through the focused test suite and a clean diff review.

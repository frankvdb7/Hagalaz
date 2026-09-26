## Why

`MoneyPouchContainer.Contains` can report that a requested coin amount is unavailable when the player has enough coins across the pouch and eligible inventory. This makes containment queries inconsistent with the actual available balance and can reject valid callers.

## What Changes

- Make the coin containment query use the sum of the current pouch coins and eligible inventory coins.
- Use an overflow-safe intermediate type when combining the balances.
- Add focused regression coverage for pouch-only, inventory-only, combined, insufficient, exact-boundary, and large-balance cases.
- Preserve all mutation, payment, notification, and generic item-container behavior.

## Capabilities

### New Capabilities

- `money-pouch-containment`: Defines the combined-balance behavior of coin containment queries.

### Modified Capabilities

- None.

## Impact

- Affected production code: `MoneyPouchContainer.Contains` only.
- Affected tests: focused `MoneyPouchContainer.Contains` regression tests.
- No new dependencies, APIs, persistence, messaging, or cross-container abstractions.
- Explicit non-goals include mutation methods, exact-payment/trade behavior, shop logic, and generic container semantics.

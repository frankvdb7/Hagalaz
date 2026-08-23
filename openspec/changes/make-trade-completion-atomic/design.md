## Context

Trade callbacks can run concurrently. The session gate serializes lifecycle decisions, but it does not by itself protect inventory, money-pouch, or offer containers from unrelated synchronous mutations. There is no existing pairwise character lock.

The design therefore uses a small opt-in boundary owned by `TradeItemContainer`: a private mutation lock and stable lock order. Its inherited normal mutators and checked trade operations use that boundary, while unrelated `BaseItemContainer` descendants remain unlocked and unchanged. No trade concept is placed in the base class.

## Decisions

1. **One owner and four states.** `TradingCharacterScript` owns `Active`, `Completing`, `Completed`, and `Cancelled`. The session gate prevents duplicate completion or cancellation. A failed completion returns to active processing and can then follow the ordinary cancellation path.

2. **Lock before snapshot and validation.** `TradeExchange` gathers the two offer containers, both inventories, and both money pouches, sorts distinct participating `TradeItemContainer` instances by their stable order, and locks them synchronously. It snapshots offers and validates capacity only after the locks are held.

3. **Boolean domain operations.** `ITradeItemContainer` exposes only checked add/remove success. `IMoneyPouchContainer` exposes boolean `AddForTrade`/`RemoveForTrade`; the pouch owns its overflow and underflow rules, including inventory coins and pouch notifications. No mutation handle escapes the operation.

4. **Complete or untouched.** Completion validates both opposite offers, applies both recipient deliveries, and clears both offers only after both deliveries report success. There is no partial settlement or fallback that returns an opposite offer after only one side was delivered. The lock and preflight boundary are the correctness mechanism; observer notification is not treated as an economy failure.

5. **Refund is a normal move.** Cancellation delivers each offer back to its original owner under the same lock boundary and clears escrow only after both returns succeed. Forced destruction may use the existing Rewards/Bank containers for untouched escrow when normal refund cannot fit; those containers are not participants in normal exchange completion.

## Rejected alternatives

- Deferred transfer receipts, compensation states, and cross-container rollback were rejected because they retain nested transaction state and can become unexecutable after disconnect.
- Whole-container snapshots/replacement were rejected because unrelated gameplay can change a character container outside the trade operation.
- Rewards/Bank recovery during normal exchange was rejected because it creates a third exchange participant and permits partial settlement.
- A new character-wide transaction service was rejected because the required boundary is synchronous and local to the involved containers.

## Exceptional behavior

Expected capacity or checked-operation failure returns `false` without clearing offers. Unexpected domain exceptions are caught at the exchange boundary and reported as a failed attempt; the session does not enter a persistent compensation state. Forced destruction retains the existing recovery-container path for untouched cancellation escrow.

## Why

Trade completion currently performs several recipient and escrow mutations from connection callbacks. A second accept, cancellation, or disconnect can observe the session between those mutations and duplicate, lose, or refund value.

## What Changes

- Serialize the terminal trade decision through the existing session gate.
- Use a neutral per-container mutation lock, acquired in deterministic order for the short completion/refund operation.
- Validate both offers and both recipients while the involved containers are locked, then perform one complete exchange or leave escrow untouched.
- Keep trade-container and money-pouch operations boolean and domain-specific; no caller-visible mutation or receipt objects.
- Keep cancellation/refund as the normal operation for returning escrow. Use existing Rewards/Bank recovery only for forced destruction when untouched escrow cannot fit.
- Keep only Active, Completing, Completed, and Cancelled session states.
- Add focused business-invariant tests for races, capacity, coins, stackability, refund, disconnect conservation, and idempotence.

## Non-goals

- No generic inventory transaction framework or public reversible mutation API.
- No trade-specific behavior in `BaseItemContainer` beyond the neutral synchronization boundary.
- No durable trade ledger, new coordinator, persistence pipeline, or partial exchange settlement.
- No changes to unrelated container semantics.

## Capabilities

### New Capabilities

- `trading-completion`: Atomic, idempotent completion and terminal cleanup of an in-memory player trade.

### Modified Capabilities

None.

## Impact

The change affects the trading script, opt-in trade containers, money-pouch trade entry points, and focused tests. Existing character persistence is reused only by forced cancellation conservation.

## Acceptance Criteria

- Completion executes at most once and either transfers both offers completely or leaves both recipients and escrow unchanged.
- Refund returns each offer exactly once and never runs after successful completion.
- Concurrent terminal callbacks cannot duplicate or lose items or coins.
- Offers changed after acceptance invalidate the confirmation.
- No mutation/receipt/compensation state is retained by a trade session.

## Stop Conditions

Do not introduce a general transaction framework, durable trade escrow, a parallel trade service, or unrelated inventory/container behavior changes. Broader persistence or distributed character-save work is follow-up scope.

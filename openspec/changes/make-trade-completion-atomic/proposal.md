## Why

Trade completion currently mutates two players and the in-memory offer containers through an unchecked multi-step sequence. Because component handlers run directly on connection threads, two final accepts or a cancellation/disconnect can interleave and duplicate, lose, or refund already-transferred items and coins. This is a critical economy-integrity defect that must be fixed in the existing trade lifecycle.

## What Changes

- Give each active trade one shared serialized operation gate and explicit terminal state.
- Make final acceptance single-entry and idempotent, including repeated packets and concurrent accept/cancel paths.
- Validate accepted state, offer contents, destination capacity, and money-pouch capacity before applying an exchange.
- Check every inventory, trade-container, and money-pouch mutation, restoring the pre-operation destination state when a checked operation fails.
- Route completion, cancellation, disconnect, and logout cleanup through one terminal cleanup path, with no escrow refund after successful completion.
- Keep a refund that cannot currently fit in a character container pending and retry it through the linked trade lifecycle instead of discarding in-memory escrow.
- Link the target character's lifecycle to the owning trade session so target disconnect/logout cleanup uses the same operation gate.
- Preserve the existing trade containers, character inventory/money-pouch APIs, and character persistence flow.
- Add deterministic MSTest coverage for races, failure paths, conservation, capacity, modification invalidation, and independent trades.

## Capabilities

### New Capabilities

- `trading-completion`: Atomic, idempotent completion and terminal cleanup of an in-memory player trade.

### Modified Capabilities

None.

## Impact

The change is limited to `TradingCharacterScript`, its focused test suite, and the existing item-container mutation behavior used by the script. It adds no service, queue, persistence, database, or third-party dependency and does not change the existing character persistence pipeline.

## Acceptance Criteria

- Trade completion executes at most once; repeated final accepts have no additional effect.
- Accepted, cancelled, disconnected, failed, and concurrent paths conserve all item quantities and currency.
- Completion either transfers both offers fully or leaves the players and escrow consistent.
- Every mutation result is checked, and cancellation/refund cannot run after successful completion.
- Offer changes invalidate acceptance through the existing trade-change flow.
- Unrelated trade sessions remain independently usable.
- Deterministic barrier/controlled-task tests reproduce the previous race and cover all issue-required failure cases.

## Stop Conditions

Do not introduce a general transaction framework, durable trade escrow, a parallel trade coordinator/service, a new persistence pipeline, or unrelated inventory/container rewrites. If correctness requires a broader distributed character-save change, record it as follow-up work for issue #346.

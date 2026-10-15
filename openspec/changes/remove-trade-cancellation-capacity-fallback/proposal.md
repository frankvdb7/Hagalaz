# Proposal

## Why

Trade offers are funded by moving items out of the offering character's Inventory or MoneyPouch. Returning an offer restores the capacity that operation freed, so the forced Rewards/Bank fallback models a capacity failure that the supported trade flow cannot produce.

## What Changes

- Remove the forced-destruction recovery path that moves trade offers to Rewards or Bank when a refund does not fit.
- Keep the normal transactional refund and its operation-result check so a rejected transfer rolls back instead of partially committing.
- Keep insufficient recipient capacity as a failed trade completion; rollback leaves the original offers available for refund.

## Scope

### In Scope

- Trade cancellation and detach cleanup.
- The recovery-only trade transfer helper and tests for its unsupported capacity state.
- The canonical `trading-completion` behavior specification.

### Non-Goals

- Changing trade confirmation or recipient-capacity behavior.
- Adding a Bank/drop overflow policy, persistent recovery store, or generic recovery framework.
- Changing duel settlement or other item-container transactions.

## Acceptance Criteria

- All production cancellation paths attempt one atomic refund to the original Inventory and MoneyPouch owners.
- Cancellation marks the trade terminal only after the refund is staged and committed.
- Failed recipient delivery leaves the transaction rolled back, then the ordinary cancellation refund returns the untouched offers.
- No forced Rewards/Bank capacity selection remains in trade cancellation.

## Stop Conditions

- If a production offer can be funded from storage other than the owner's Inventory or MoneyPouch, stop and retain the recovery path until that ownership path is understood.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `trading-completion`: cancellation refunds offers to their original owners without capacity-based recovery destinations.

## Impact

`TradingCharacterScript`, `TradeExchange`, focused trade tests, and the `trading-completion` specification. No public API or storage behavior changes.

# Proposal

## Why

Trade offers are funded by moving items out of the offering character's Inventory or MoneyPouch. However, the GameFrame money-pouch withdrawal can consume the inventory capacity freed by an offer while the trade is open. Reject that withdrawal when its deferred amount is submitted so cancellation and detach can refund the offer to its owner.

## What Changes

- Remove the forced-destruction recovery path that moves trade offers to Rewards or Bank when a refund does not fit.
- Reject GameFrame money-pouch withdrawals while the character is busy, including an active trade.
- Keep the normal transactional refund and its operation-result check so a rejected transfer rolls back instead of partially committing.
- Keep insufficient recipient capacity as a failed trade completion; rollback leaves the original offers available for refund.
- Replace pre-snapshot dispatch of every script's destruction callback with a narrow script detach callback for economic state that must be reflected in the final snapshot.

## Scope

### In Scope

- Trade cancellation and detach cleanup.
- Character script detach callbacks for trade and pre-combat duel refunds.
- The GameFrame money-pouch withdrawal path that can consume refund capacity during an active trade.
- The recovery-only trade transfer helper and tests for its unsupported capacity state.
- The canonical `trading-completion` behavior specification.

### Non-Goals

- Changing trade confirmation or recipient-capacity behavior.
- Adding a Bank/drop overflow policy, persistent recovery store, or generic recovery framework.
- Changing combat-stage duel settlement, unrelated script destruction callbacks, the equipment degradation timer, or other item-container transactions.

## Acceptance Criteria

- All production cancellation paths attempt one atomic refund to the original Inventory and MoneyPouch owners.
- A submitted GameFrame money-pouch withdrawal cannot consume Inventory slots while a trade is active.
- Character detachment invokes script-specific detach callbacks before the final snapshot, while general script destruction callbacks remain at actual character destruction.
- A trade funded through the inventory click handler is refunded by the trade script's detach callback and appears in the inventory dehydration payload.
- A pre-combat duel's stakes are returned by the duel script's detach callback before the final snapshot.
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

`ICharacterScript`, `CharacterScriptBase`, `Character`, trade and pre-combat duel scripts, logout lifecycle tests, and the character lifecycle and trading specifications. The script contract gains a narrow `OnDetach()` callback; item storage behavior is unchanged.

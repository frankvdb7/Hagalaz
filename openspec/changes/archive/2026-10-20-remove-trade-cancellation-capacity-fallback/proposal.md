# Proposal

## Why

Trade offers are funded by moving items out of the offering character's Inventory or MoneyPouch. Funding a non-coin offer frees Inventory capacity needed for a refund, but active-trade Inventory-credit actions can consume it. Reject those actions during an active trade while allowing ordinary familiar withdrawals from their open screen, and make equipment unequip actions cancel and refund the trade before moving an item into Inventory.

## What Changes

- Remove the forced-destruction recovery path that moves trade offers to Rewards or Bank when a refund does not fit.
- Reject GameFrame money-pouch withdrawals during an active trade.
- Reject Take BoB withdrawals from the Summoning Orb and Summoning Tab during an active trade, while allowing normal familiar withdrawals from the familiar inventory screen outside gameplay-busy states.
- Preserve the existing interruption behavior for aura unequip actions so cancellation refunds a funded offer before the unequipped aura can use its freed Inventory slot.
- Keep the normal transactional refund and its operation-result check so a rejected transfer rolls back instead of partially committing.
- Keep insufficient recipient capacity as a failed trade completion; rollback leaves the original offers available for refund.
- Replace pre-snapshot dispatch of every script's destruction callback with a narrow script detach callback for economic state that must be reflected in the final snapshot.

## Scope

### In Scope

- Trade cancellation and detach cleanup.
- Character script detach callbacks for trade and pre-combat duel refunds.
- GameFrame money-pouch and Summoning Orb/Summoning Tab familiar withdrawals, plus aura unequips from the Equipment tab, that can consume refund capacity during an active trade.
- Removal of the recovery-only trade transfer helper and synthetic no-capacity cancellation tests, with regressions for the supported capacity-consuming paths and detach refund.
- The canonical `trading-completion` behavior specification.

### Non-Goals

- Changing trade confirmation or recipient-capacity behavior.
- Adding a Bank/drop overflow policy, persistent recovery store, or generic recovery framework.
- Changing combat-stage duel settlement, unrelated script destruction callbacks, the equipment degradation timer, or other item-container transactions.

## Acceptance Criteria

- All production cancellation paths attempt one atomic refund to the original Inventory and MoneyPouch owners.
- A submitted GameFrame money-pouch withdrawal cannot consume Inventory slots while a trade is active.
- A Summoning Orb or Summoning Tab Take BoB action cannot consume Inventory slots while a trade is active.
- The familiar inventory screen's own overlay does not block normal withdrawals when no gameplay action is busy.
- An aura unequip during trade interrupts and refunds the active trade before the aura is moved into Inventory.
- Character detachment invokes script-specific detach callbacks before the final snapshot, while general script destruction callbacks remain at actual character destruction.
- A trade funded through the inventory click handler is refunded by the trade script's detach callback and appears in the inventory dehydration payload.
- A pre-combat duel's stakes are returned by the duel script's detach callback before the final snapshot.
- Cancellation marks the trade terminal only after the refund is staged and committed.
- Failed recipient delivery leaves the transaction rolled back, then the ordinary cancellation refund returns the untouched offers.
- No forced Rewards/Bank capacity selection remains in trade cancellation.

## Stop Conditions

- If another supported production action can consume Inventory capacity while a trade is active, identify and guard that path before relying on refund capacity.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `trading-completion`: cancellation refunds offers to their original owners without capacity-based recovery destinations.

## Impact

`ICharacterScript`, `CharacterScriptBase`, `Character`, trade and pre-combat duel scripts, logout lifecycle tests, and the character lifecycle and trading specifications. The script contract gains a narrow `OnDetach()` callback; item storage behavior is unchanged.

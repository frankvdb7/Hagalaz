# Proposal

## Why

Duel victory currently removes stake coins and adds both stake containers to the winner's inventory through separate, unchecked operations. A partial or repeated victory path can therefore leave stakes unconsumed or award items more than once. The existing item-container transaction already provides the atomic boundary needed to settle both stakes and the winner's destinations together.

## What Changes

- Route victory payout through `DuelStakeExchange`.
- Move both stake containers into the winner's MoneyPouch and inventory in one `ItemContainerTransaction`.
- Consume the source stake items as part of that transfer so a later settlement attempt cannot award them again.
- Leave both stakes and winner storage unchanged if any item cannot be transferred.
- Keep the victory stage and its stake-container references active when payout cannot complete; only show the victory screen and allow the trapdoor exit after settlement succeeds.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: define atomic duel victory settlement and its failure behavior.

## Impact

The affected runtime path is `DuelArenaCombatScript.StartVictoryStage` and the existing `DuelStakeExchange`. The transaction will enlist both stake containers and the winner's MoneyPouch, which contributes its inventory storage. No public API or dependency changes are required.

## Non-goals

- Changing duel staking or cancellation refunds.
- Adding a new recovery destination, payout queue, settlement coordinator, or generic transaction mechanism.
- Changing the existing MoneyPouch overflow rules.

## Acceptance Criteria

- A successful victory settlement moves all non-coin stakes to the winner's inventory and coin stakes through the winner's MoneyPouch in one committed transaction.
- A failed transfer leaves both stake containers, the winner's inventory, and the winner's pouch unchanged.
- If payout fails because inventory capacity changed during combat, the victory stage does not advance, the participant remains in the arena, and the stakes remain available for another settlement attempt.
- Repeating settlement after success does not award any stake item or coin a second time.
- Existing staking and cancellation-refund behavior remains unchanged.

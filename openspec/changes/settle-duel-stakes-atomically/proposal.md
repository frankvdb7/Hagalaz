# Proposal

## Why

Duel victory currently removes stake coins and adds both stake containers to the winner's inventory through separate, unchecked operations. A partial or repeated victory path can therefore leave stakes unconsumed or award items more than once. The existing item-container transaction already provides the atomic boundary needed to settle both stakes and the winner's destinations together. During pre-combat cancellation, the same transaction can commit both refunds and then throw from publication, leaving the duel session active even though its stakes have already returned.

## What Changes

- Route victory payout through `DuelStakeExchange`.
- Complete pre-combat session cleanup when a cancellation refund commits but post-commit publication fails.
- Move both stake containers into the winner's MoneyPouch and inventory in one `ItemContainerTransaction`.
- Consume the source stake items as part of that transfer so a later settlement attempt cannot award them again.
- Leave both stakes and winner storage unchanged if any item cannot be transferred.
- Keep the victory stage and its stake-container references active when payout cannot complete; only show the victory screen and allow the trapdoor exit after settlement succeeds.
- Check confirmation capacity against the actual destinations: Inventory must fit non-coin stakes, returned equipment, and only the coin amount that exceeds the remaining MoneyPouch capacity.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: define atomic duel victory settlement and its failure behavior.

## Impact

The affected runtime paths are `DuelRules.CheckRules` during duel confirmation, `DuelArenaCombatScript.StartVictoryStage` during settlement, and `DuelArenaScript.CancelDuelSession` during pre-combat cancellation and detach. Transactions use the existing `DuelStakeExchange` and enlist the required stake and destination storage. No public API or dependency changes are required.

## Non-goals

- Changing stake or refund economics, destinations, or all-or-nothing rejection behavior.
- Adding a new recovery destination, payout queue, settlement coordinator, or generic transaction mechanism.
- Changing the existing MoneyPouch overflow rules.

## Acceptance Criteria

- A successful victory settlement moves all non-coin stakes to the winner's inventory and coin stakes through the winner's MoneyPouch in one committed transaction.
- Duel confirmation accepts coin stakes that fit in each participant's MoneyPouch without requiring Inventory space for those coins, while still checking Inventory for coin overflow.
- A failed transfer leaves both stake containers, the winner's inventory, and the winner's pouch unchanged.
- If payout fails because inventory capacity changed during combat, the victory stage does not advance, the participant remains in the arena, and the stakes remain available for another settlement attempt.
- While a duel session is active, its character script reports the participant as busy so equipment access and familiar withdrawals cannot consume inventory capacity reserved for a stake refund or confirmed payout.
- A stage-flow regression funds both stakes through the offer handlers, confirms the duel, attempts capacity-consuming interactions during combat, then verifies victory settles both stakes.
- Repeating settlement after success does not award any stake item or coin a second time.
- Stake and refund economics and all-or-nothing rejection behavior remain unchanged.
- If refund storage commits but a post-commit callback throws during cancellation, the session owner logs the failure and completes session cleanup without retrying the refund.
- If a stake or return mutation commits but a post-commit completion or publication callback throws, the duel owner invalidates both first-stage acceptances before propagating the typed exception.

# Proposal

## Why

Duel confirmation currently checks that each participant can fit the full stake and, separately, any equipment removed by the duel rules. Both checks can pass even when the participant cannot fit the combined result. The winner's atomic payout then rejects the stakes after the duel has ended.

## What Changes

- Include rule-removed equipment in the inventory capacity check for the full duel stake.
- Reject confirmation when a participant cannot fit both the stake and their rule-removed equipment.
- Add a regression for capacity that is sufficient for each set separately but insufficient for their combination.

## Non-goals

- Redesigning duel settlement or its transaction.
- Adding an overflow destination, persistence mechanism, or recovery queue.
- Changing staking, refund, or equipment-removal semantics.

## Acceptance Criteria

- Duel confirmation is rejected unless each participant can fit the combined stake and equipment that will be returned to their inventory.
- Existing equipment-only and stake-only capacity messages remain unchanged.
- The existing atomic settlement behavior remains unchanged.

## Design Decisions

- Build the combined stake once in `DuelArenaScript.StartConfirmationStage()` and pass it to the existing `DuelRules.CheckRules()` validation.
- Keep capacity policy in `DuelRules`; it already checks capacity for equipment removed by duel rules.
- Keep the current atomic `DuelStakeExchange.TrySettleWinner()` as the final destination check.

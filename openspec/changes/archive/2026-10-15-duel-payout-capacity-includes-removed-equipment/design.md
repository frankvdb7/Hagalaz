# Design

`StartConfirmationStage()` will gather the combined stake before checking duel rules. `DuelRules.CheckRules()` will continue to check whether restricted equipment fits on its own, preserving the existing equipment-specific message, and will additionally check whether the participant can fit the stake together with that equipment. A combined-capacity failure uses the existing duel-stake capacity messages.

The rules check remains a preflight. `DuelStakeExchange.TrySettleWinner()` continues to perform the authoritative atomic transfer and reject any destination that no longer accepts the payout.

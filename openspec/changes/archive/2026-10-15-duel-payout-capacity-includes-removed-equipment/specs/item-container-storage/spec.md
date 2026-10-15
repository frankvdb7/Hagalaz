# Spec Delta

## MODIFIED Requirements

### Requirement: Duel victory stakes are credited atomically
Duel victory settlement MUST transfer all stake items to the winner as one atomic operation, consuming the source stakes exactly once. If any destination cannot accept the complete payout, all stakes and winner storage MUST remain unchanged. Before duel confirmation, each participant MUST have inventory capacity for both the full stake and any equipment that duel rules will return to their inventory before combat.

#### Scenario: Duel confirmation includes returned equipment in payout capacity
- **WHEN** rule-removed equipment and the full stake each fit in a participant's inventory separately but do not fit together
- **THEN** duel confirmation is rejected before combat starts

#### Scenario: Winner receives both stakes
- **WHEN** the winner's inventory and MoneyPouch can accept both participants' stakes
- **THEN** all non-coin items are transferred to inventory, all coin stacks are credited through the MoneyPouch, and both stake containers are empty

#### Scenario: Winner cannot accept the complete payout
- **WHEN** the winner's inventory or MoneyPouch cannot accept any part of the settlement
- **THEN** both stake containers, the winner's inventory, and the winner's MoneyPouch remain unchanged

#### Scenario: Victory settlement is repeated
- **WHEN** the same stakes are settled again after a successful settlement
- **THEN** no additional items or coins are credited to the winner

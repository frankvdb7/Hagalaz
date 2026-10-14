# Spec Delta

## ADDED Requirements

### Requirement: Duel victory stakes are credited atomically
Duel victory settlement MUST transfer all stake items to the winner as one atomic operation, consuming the source stakes exactly once. If any destination cannot accept the complete payout, all stakes and winner storage MUST remain unchanged.

#### Scenario: Winner receives both stakes
- **WHEN** the winner's inventory and MoneyPouch can accept both participants' stakes
- **THEN** all non-coin items are transferred to inventory, all coin stacks are credited through the MoneyPouch, and both stake containers are empty

#### Scenario: Winner cannot accept the complete payout
- **WHEN** the winner's inventory or MoneyPouch cannot accept any part of the settlement
- **THEN** both stake containers, the winner's inventory, and the winner's MoneyPouch remain unchanged

#### Scenario: Victory settlement is repeated
- **WHEN** the same stakes are settled again after a successful settlement
- **THEN** no additional items or coins are credited to the winner

# Spec Delta

## ADDED Requirements

### Requirement: Duel victory stakes are credited atomically
Duel victory settlement MUST transfer all stake items to the winner as one atomic operation, consuming the source stakes exactly once. If any destination cannot accept the complete payout, all stakes and winner storage MUST remain unchanged.

#### Scenario: Winner receives both stakes
- **WHEN** the winner's inventory and MoneyPouch can accept both participants' stakes
- **THEN** all non-coin items are transferred to inventory, all coin stacks are credited through the MoneyPouch, and both stake containers are empty

#### Scenario: Coin-only stakes fit in the MoneyPouch
- **WHEN** each participant's remaining MoneyPouch capacity can hold the combined coin stakes, even though their inventories are full
- **THEN** duel confirmation accepts the stakes and a winner receives the coins through the MoneyPouch without inventory space

#### Scenario: Coin overflow needs inventory capacity
- **WHEN** the combined coin stakes exceed a participant's remaining MoneyPouch capacity
- **THEN** duel confirmation requires inventory capacity for the overflow coins alongside non-coin stakes and returned equipment

#### Scenario: Active duel preserves confirmed payout capacity
- **WHEN** a participant attempts to open Equipment or withdraw familiar items after duel combat starts
- **THEN** the active combat script reports the character busy and those inventory-changing interactions are rejected
- **AND** the capacity accepted during confirmation remains available for stake settlement

#### Scenario: Winner cannot accept the complete payout
- **WHEN** the winner's inventory or MoneyPouch cannot accept any part of the settlement
- **THEN** both stake containers, the winner's inventory, and the winner's MoneyPouch remain unchanged
- **AND** the victory stage does not advance, the stake containers remain owned by the active combat script, and the trapdoor does not remove the winner from the arena

#### Scenario: Winner retries after making inventory space
- **WHEN** a previous payout attempt was rejected and the winner later makes enough inventory space
- **THEN** the victory stage can be started again and both stake containers are settled exactly once

#### Scenario: Victory settlement is repeated
- **WHEN** the same stakes are settled again after a successful settlement
- **THEN** no additional items or coins are credited to the winner

## ADDED Requirements

### Requirement: Paid shop purchases require exact payment

The shop SHALL grant a paid item only when the exact calculated cost has been removed from the player's available shop currency. If the player cannot pay the full cost, the purchase SHALL fail and the player's existing currency SHALL remain unchanged.

#### Scenario: Player has one coin for a ten-thousand-coin item

- **WHEN** the item costs 10,000 coins and the player has 1 coin
- **THEN** the purchase fails, the item is not granted, and the player's coin remains

#### Scenario: Player is short by one coin

- **WHEN** the item costs `cost` coins and the player has `cost - 1` coins
- **THEN** the purchase fails and all `cost - 1` coins remain

#### Scenario: Player has the exact coin cost

- **WHEN** the item costs `cost` coins and the player has exactly `cost` coins
- **THEN** the purchase succeeds and exactly `cost` coins are removed

#### Scenario: Player has more than the coin cost

- **WHEN** the item costs `cost` coins and the player has more than `cost` coins
- **THEN** the purchase succeeds and exactly `cost` coins are removed

#### Scenario: Coins are split between the pouch and inventory

- **WHEN** the player's money pouch and inventory together contain exactly the coin cost
- **THEN** the purchase succeeds and the exact cost is removed across those containers

### Requirement: Free and alternative-currency shop purchases keep their existing semantics

The shop SHALL not charge currency for sample stock, and a shop using a non-coin currency SHALL require and remove the exact cost from the player's inventory.

#### Scenario: Sample stock is free

- **WHEN** the player buys an item from sample stock
- **THEN** the purchase succeeds when the existing stock and inventory checks pass without removing any currency

#### Scenario: Non-coin currency is paid exactly

- **WHEN** a shop item costs `cost` units of a non-coin currency and the player's inventory contains exactly `cost`
- **THEN** the purchase succeeds and exactly `cost` units of that currency are removed

#### Scenario: Non-coin currency is underfunded

- **WHEN** a shop item costs `cost` units of a non-coin currency and the player's inventory contains less than `cost`
- **THEN** the purchase fails and the existing currency remains unchanged

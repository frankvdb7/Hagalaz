## Purpose

Provide a reliable containment query for coin currency when the player's funds are distributed between the money pouch and eligible inventory.

## ADDED Requirements

### Requirement: Coin containment uses the combined available balance

For coin currency, the containment query MUST compare the requested amount with the sum of the current money-pouch coin balance and the current eligible inventory coin balance. The combined calculation MUST remain correct for the largest supported individual balances and MUST NOT wrap an `int` intermediate into an incorrect result.

For non-coin items, the existing containment behavior MUST remain unchanged.

#### Scenario: Pouch coins satisfy the request
- **WHEN** the money pouch contains at least the requested coin amount
- **THEN** the coin containment query returns `true`

#### Scenario: Inventory coins satisfy the request with an empty pouch
- **WHEN** the money pouch is empty and eligible inventory contains at least the requested coin amount
- **THEN** the coin containment query returns `true`

#### Scenario: Pouch and inventory coins satisfy the request together
- **WHEN** the pouch balance alone is below the request but the pouch and eligible inventory balances together meet or exceed it
- **THEN** the coin containment query returns `true`

#### Scenario: Combined balance is insufficient
- **WHEN** the sum of the pouch and eligible inventory coin balances is below the requested amount
- **THEN** the coin containment query returns `false`

#### Scenario: Exact combined balance satisfies the request
- **WHEN** the requested amount equals the sum of the pouch and eligible inventory coin balances
- **THEN** the coin containment query returns `true`

#### Scenario: Large balances do not overflow
- **WHEN** the pouch and eligible inventory contain large coin balances whose sum exceeds the range of a signed 32-bit integer
- **THEN** the coin containment query returns the result implied by the mathematically correct combined balance

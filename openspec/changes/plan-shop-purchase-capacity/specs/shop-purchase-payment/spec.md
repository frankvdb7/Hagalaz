## ADDED Requirements

### Requirement: Purchase quantity accounts for payment-released inventory capacity

The shop SHALL determine one final purchase quantity, no greater than the request or the available stock, against the inventory capacity remaining after exact payment. That quantity SHALL determine the cost, the stock removed, the items added and the `ShopItemBoughtEvent` count.

#### Scenario: Exact payment empties the only slot

- **WHEN** the inventory is full, its only slot is a coin stack equal to the cost, and one non-stackable item is bought
- **THEN** the purchase succeeds using the released slot

#### Scenario: Partially consumed currency stack keeps its slot

- **WHEN** payment leaves part of the currency stack in the inventory and no other slot is free
- **THEN** the purchase is rejected and nothing changes

#### Scenario: Pouch-only payment releases nothing

- **WHEN** the money pouch covers the cost and the inventory is full
- **THEN** the purchase is rejected and nothing changes

#### Scenario: Split payment empties inventory coins

- **WHEN** pouch coins plus a whole inventory coin stack equal the cost and the inventory is full
- **THEN** the purchase succeeds using the released slot

#### Scenario: Alternative currency across slots

- **WHEN** payment fully consumes some currency slots and partly consumes another
- **THEN** only the fully emptied slots count as available capacity

#### Scenario: Capacity limits quantity

- **WHEN** a non-stackable request exceeds free plus released slots
- **THEN** the largest fitting quantity is bought, only that quantity is charged and the event reports it

#### Scenario: Sample stock releases nothing

- **WHEN** a sample item is bought with a full inventory
- **THEN** no payment-released capacity is assumed and the purchase is rejected

#### Scenario: Cost cannot overflow

- **WHEN** unit price times quantity exceeds `int.MaxValue`
- **THEN** the purchase is rejected without mutation

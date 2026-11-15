## ADDED Requirements

### Requirement: Degradation countdown follows the equipped lifetime of its item

An equipped degradable item's countdown MUST progress once per tick while that exact item instance is equipped, MUST be saved on that instance when it leaves equipment or when the character detaches, and MUST be cancelled exactly once.

#### Scenario: Initial equip publication

- **WHEN** an item is equipped and the equipment-change notification lists its slot while it is still equipped
- **THEN** the countdown keeps running and the item's saved ticks are unchanged

#### Scenario: Unrelated equipment change

- **WHEN** a different slot changes or a different item is equipped
- **THEN** the tracked countdown keeps running

#### Scenario: Unequip or replacement

- **WHEN** the tracked item instance is no longer equipped after an equipment change
- **THEN** its remaining ticks are stored on that item, the task is cancelled, and the item is not degraded afterwards

#### Scenario: Logout

- **WHEN** the character detaches for logout
- **THEN** remaining ticks are written to the equipped item before the final snapshot is dehydrated, so the persisted equipment carries the remainder

#### Scenario: Expiry

- **WHEN** the countdown reaches zero while the item is equipped
- **THEN** the item is removed once and the character is told it crumbled

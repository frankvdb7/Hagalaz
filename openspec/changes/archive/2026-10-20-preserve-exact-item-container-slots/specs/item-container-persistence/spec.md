## ADDED Requirements

### Requirement: Persisted item slots retain physical identity

Every persisted character item container SHALL dehydrate occupied items with their physical slot index and hydrate them back into that exact slot without gameplay insertion.

#### Scenario: Sparse character containers round trip
- **WHEN** inventory, bank, equipment, familiar inventory, or rewards contain gaps
- **THEN** every occupied item returns to its original slot and every gap remains empty

#### Scenario: Equipment retains semantic positions
- **WHEN** a character has items in separated hat, weapon, and shield slots
- **THEN** each item returns to the same equipment position

#### Scenario: Item extra data round trips
- **WHEN** a persisted item has extra data
- **THEN** hydration recreates the item with the same extra data

### Requirement: Exact restoration owns and validates storage

The container SHALL reject persisted entries with duplicate or out-of-bounds slots or invalid counts before replacing its state. The resulting storage SHALL have length `Capacity` and SHALL not alias a caller-owned array. Money pouch SHALL permit its zero-coin item, restore that item in slot 0 when persisted pouch state is empty, and reject non-coin entries.

#### Scenario: Empty persisted money pouch
- **WHEN** a money pouch hydrates from an empty persisted item list
- **THEN** slot 0 contains item 995 with count 0 and can be dehydrated as that item

#### Scenario: Non-coin persisted money pouch item
- **WHEN** a money pouch hydrates from an item other than coins
- **THEN** hydration rejects the item without changing the previous pouch state

#### Scenario: Corrupt persisted state
- **WHEN** entries have a negative slot, a slot at or above capacity, a duplicate slot, or an invalid count
- **THEN** restoration rejects the input without changing the container's previous slots

#### Scenario: Exact placement bypasses gameplay insertion
- **WHEN** identical stackable items are restored into separated slots
- **THEN** they remain separated in those exact slots

#### Scenario: Caller mutates the supplied array
- **WHEN** a caller changes an array after `SetItems` accepts it
- **THEN** container slots remain unchanged and its storage length equals `Capacity`

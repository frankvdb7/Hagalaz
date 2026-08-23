## Purpose

This capability defines reliable range insertion for item containers so callers can distinguish a complete successful addition from a rejected range without partial storage changes.

## ADDED Requirements

### Requirement: Range insertion is all-or-nothing

An item container MUST evaluate every non-null item in one range against the same simulated result of the preceding items. If any item cannot be inserted, the operation MUST return `false` and leave all existing slots and item counts unchanged. If every item fits, the operation MUST return `true` and preserve the container's established stacking and slot-allocation behavior.

#### Scenario: Non-stackable quantity needs multiple slots

- **WHEN** a range contains a non-stackable item with a count greater than one and fewer free slots than its count
- **THEN** the range is rejected and no slot or existing item count changes

#### Scenario: Non-stackable quantity has enough slots

- **WHEN** a range contains a non-stackable item with a count greater than one and at least that many free slots
- **THEN** the range succeeds with one count-one item in each consumed slot

#### Scenario: Items in one range share a newly created stack

- **WHEN** multiple incoming stackable items can stack together and no matching stack exists before the range
- **THEN** the range succeeds with the established single resulting stack and the combined quantity

#### Scenario: Combined stack overflows

- **WHEN** an incoming item would make an existing or earlier-created stack exceed `int.MaxValue`
- **THEN** the range is rejected and every existing slot and count remains unchanged

#### Scenario: A later item cannot fit

- **WHEN** an earlier item in the range fits but a later item does not
- **THEN** the complete range is rejected without retaining the earlier item

#### Scenario: Capacity query matches range insertion

- **WHEN** a range is checked for space and then inserted without another container mutation
- **THEN** the space result MUST agree with whether the complete range can be inserted

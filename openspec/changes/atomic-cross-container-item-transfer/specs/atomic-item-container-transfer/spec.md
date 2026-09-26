## Purpose

Defines how an exact item quantity moves between two containers so callers never observe a failed move that changed only one side.

## ADDED Requirements

### Requirement: Exact item transfers commit both containers or neither

An exact transfer SHALL move the complete requested positive quantity from its source into its destination, or leave both containers' stored slots, counts, and revisions unchanged. A transfer to the same container or with a non-positive quantity SHALL not mutate storage.

#### Scenario: Destination cannot accept the complete quantity
- **WHEN** the destination is full or its matching stack would overflow
- **THEN** the operation fails with both containers' slots and counts unchanged

#### Scenario: Source does not contain the complete quantity
- **WHEN** the source contains fewer matching items than requested
- **THEN** the operation fails with both containers' slots and counts unchanged

#### Scenario: Exact stack transfer succeeds
- **WHEN** a positive quantity fits into an existing or new destination stack
- **THEN** exactly that quantity is removed from the source and added to the destination

#### Scenario: Non-stackable instances move
- **WHEN** a complete non-stackable item instance moves into an available destination slot
- **THEN** the same instance and its item data are retained in the destination

#### Scenario: Preferred slots are supplied
- **WHEN** a matching preferred source slot or an explicit destination slot is supplied
- **THEN** the operation consumes the preferred source slot first and may use other matching source slots to complete the exact quantity, while an explicit destination slot must accept the complete insertion or neither container changes

#### Scenario: A drained source slot has a sentinel count
- **WHEN** an exact transfer drains a source item whose container retains a zero-count sentinel
- **THEN** the destination receives the requested positive quantity and the source sentinel remains in its slot

### Requirement: Transfers serialize and publish committed state

Concurrent transfers involving the same containers SHALL acquire their mutation boundaries in a deterministic order. A successful operation SHALL commit both containers and advance their revisions before invoking either container update callback. A failed operation SHALL emit no committed update. An observer exception SHALL not restore or otherwise undo committed storage.

#### Scenario: Transfers run in opposite directions
- **WHEN** two operations concurrently transfer items in opposite directions between the same containers
- **THEN** both operations complete without deadlock and each result preserves exact quantities

#### Scenario: Successful update observers inspect container state
- **WHEN** either container receives an update callback for a successful transfer
- **THEN** both source removal and destination insertion are already visible

#### Scenario: A transfer validation fails
- **WHEN** an exact transfer cannot complete
- **THEN** neither container receives an update callback

#### Scenario: An update observer throws after commit
- **WHEN** a container update observer throws after storage has committed
- **THEN** both committed storage states remain in place and the exception is handled using the existing observer exception semantics

### Requirement: Gameplay partial transfers remain explicit

Gameplay operations that intentionally move fewer than the originally requested quantity SHALL determine the exact quantity first and then request one exact transfer for that quantity. Equipment eligibility and gameplay callbacks SHALL remain owned by the equipment domain and SHALL run only around a successful storage commit.

#### Scenario: A caller supports moving as many non-stackable items as fit
- **WHEN** the destination can accept only part of the available non-stackable quantity
- **THEN** the caller calculates that quantity and transfers it exactly in one operation

#### Scenario: An equipment movement fails storage validation
- **WHEN** an equipment movement cannot complete its exact storage change
- **THEN** equipment callbacks and bonuses are not applied as though the item moved

#### Scenario: An equipment movement succeeds
- **WHEN** equipment storage movement commits successfully
- **THEN** the equipment domain applies its corresponding callback after the committed storage change

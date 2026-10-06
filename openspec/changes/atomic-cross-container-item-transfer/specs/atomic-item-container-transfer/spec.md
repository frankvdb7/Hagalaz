## Purpose

Defines how an exact item quantity moves between two containers so callers never observe a failed move that changed only one side.

## ADDED Requirements

### Requirement: Public exact transfer owns or participates in its transaction

`IItemContainer.TryTransferTo(...)` SHALL own and commit a short `ItemContainerTransaction` when neither storage participates in a current-thread transaction. When both storages already belong to the same active current-thread transaction, it SHALL participate without creating, committing, or disposing that transaction. Partial or conflicting participation SHALL be rejected before mutation. The internal `ItemContainerMutationBoundary.TryTransferTo(...)` SHALL continue to require both storages in the same active current-thread transaction.

#### Scenario: Standalone transfer owns a short transaction
- **WHEN** neither storage is bound and an exact transfer can succeed
- **THEN** the public method commits both storage changes and publishes each changed participant once without a separate caller transaction

#### Scenario: Existing transaction participates
- **WHEN** source and destination belong to the same active current-thread transaction
- **THEN** the transfer remains unpublished until caller commit and is restored if that transaction is disposed without commit

#### Scenario: Partial or conflicting participation rejects
- **WHEN** only one required storage is enlisted or the storages belong to different active transactions
- **THEN** the public operation throws before mutation and does not create a nested transaction

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

### Requirement: Transfers serialize storage and publish committed changes

Concurrent transfers involving the same containers SHALL acquire their mutation boundaries in a deterministic order. A successful standalone transfer SHALL commit both containers and advance their revisions before publishing either container's changes, and publication SHALL happen after the pair locks are released. A failed operation SHALL emit no committed update. If publication throws, the exception SHALL propagate and committed storage SHALL remain committed.

#### Scenario: Transfers run in opposite directions
- **WHEN** two operations concurrently transfer items in opposite directions between the same containers
- **THEN** both operations complete without deadlock and each result preserves exact quantities

#### Scenario: Successful publication observes both containers
- **WHEN** either container publishes changes for a successful standalone transfer
- **THEN** both source removal and destination insertion are already visible

#### Scenario: A transfer validation fails
- **WHEN** an exact transfer cannot complete
- **THEN** neither container publishes a change

#### Scenario: Change publication throws after commit
- **WHEN** a transfer has committed both container storage states and publication throws
- **THEN** committed storage is not rolled back
- **AND** the exception propagates normally

### Requirement: Composed operations publish only after storage is stable

Trade offer coin movement, trade completion, refund, escrow conservation, and paired Money Pouch and Inventory operations SHALL complete all related storage mutations before publishing container changes or Money Pouch messages. A checked storage failure SHALL restore every affected snapshot before publishing any restored-state changes. A publication exception SHALL propagate without undoing the final committed or restored storage.

#### Scenario: A trade offer moves coins between the pouch and offer
- **WHEN** coins are offered or removed from an offer
- **THEN** the pouch, Inventory, and offer storage reach their final state before any of their changes are published

#### Scenario: Trade settlement publishes only after both recipients and escrow are final
- **WHEN** trade settlement succeeds
- **THEN** both recipients' storage and both escrow containers are in their final state before any changes are published

#### Scenario: A later checked settlement step fails
- **WHEN** settlement restores earlier storage mutations after a checked storage failure
- **THEN** every snapshot is restored before any restored-state change is published

### Requirement: Equipment domain effects precede container change publication

For an equipment movement, storage SHALL commit before the required equipment domain effect runs, and container changes SHALL publish only after the domain effect completes. Unexpected domain or publication exceptions SHALL propagate without undoing committed storage.

#### Scenario: An item is equipped
- **WHEN** an item moves from Inventory into Equipment
- **THEN** `OnEquipped` runs after storage commit and before either container publishes its changes

#### Scenario: An item is unequipped
- **WHEN** an item moves from Equipment into Inventory
- **THEN** `OnUnequipped` runs after storage commit and before either container publishes its changes

#### Scenario: A huge non-stackable request cannot fit
- **WHEN** an exact transfer would expand a huge non-stackable quantity into more items than the destination can accept and no existing stack can receive them
- **THEN** the operation fails before creating per-unit incoming items and both containers remain unchanged

### Requirement: Price Checker selections do not own inventory items
Price Checker selections SHALL be non-owning clones. Inventory SHALL remain authoritative, and closing or disconnecting SHALL NOT lose items because no authoritative item leaves Inventory.

#### Scenario: A selection is added
- **WHEN** a player selects an item quantity for Price Checker
- **THEN** the selection contains a clone and Inventory remains unchanged

#### Scenario: A selection is removed
- **WHEN** a player removes quantity from Price Checker
- **THEN** only the selection changes and Inventory remains unchanged

#### Scenario: Inventory quantity falls below its selection
- **WHEN** Inventory changes and a selected item quantity is no longer owned
- **THEN** the selection is reduced to the matching quantity still in Inventory and the projection is refreshed

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

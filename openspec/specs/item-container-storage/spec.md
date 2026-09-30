# Item Container Storage

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## Requirements

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST implement `IItemContainer` and compose one `ItemContainerStorage` and one `ItemContainerMutationBoundary`, exposed as `Mutations`. Ordinary domain implementations and interfaces MUST own/expose concrete `ItemContainer Items`; they MUST NOT copy generic forwarding operations. Equipment and MoneyPouch MUST compose storage directly with private mutation boundaries and MUST NOT expose generic `Items` or their boundaries. Equipment MUST itself remain a read-only `IContainer<IItem?>`. TradeOffer, Duel, and Price Checker MUST compose the concrete `ItemContainer` where generic behavior is required. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST NOT be part of its contract. Two-container mutations MUST use instance methods on `ItemContainerMutationBoundary`; multi-container mutations MUST use a short-lived `ItemContainerTransaction`. No runtime cast may be required to recover storage infrastructure. `ItemContainerStorage` MUST own low-level slot and mutation mechanics and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. The mutation boundary MUST own deterministic two-container lock ordering and post-commit publication; the transaction MUST own deterministic multi-container lock ordering, rollback, and post-commit publication. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST be available through a neutral generic operation. MoneyPouch staging MUST participate through a narrow domain-owned path without exposing its boundary.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision

### Requirement: Cross-container transfers commit both stores atomically
An exact cross-container transfer MUST operate storage-to-storage. Its `ItemContainerMutationBoundary` MUST validate and plan source removal and destination insertion before changing either store, acquire distinct locks in stable order, call the single low-level `ItemContainerStorage` transfer algorithm, commit both stores together, advance each storage revision once, and publish changed slots only after success.

#### Scenario: Exact transfer succeeds
- **WHEN** the source has the requested quantity and the destination can accept the exact result
- **THEN** both stores commit the transfer before either domain container publishes an update

#### Scenario: Transfer fails validation
- **WHEN** the source quantity is insufficient or the destination cannot accept the result
- **THEN** neither store changes and neither container publishes a committed mutation

#### Scenario: Opposite transfers acquire locks consistently
- **WHEN** two operations transfer in opposite directions between the same stores
- **THEN** both operations acquire store locks in the same stable order and complete without lock-order deadlock

### Requirement: Storage and trade revisions have distinct purposes
Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerMutationBoundary` and `ItemContainerTransaction` MUST own lock ordering; TradeExchange MUST use a short-lived transaction rather than acquire storage locks directly. A trade offer's acceptance `Revision` MUST remain domain-owned and MUST advance according to its existing publication semantics, independently of storage revision.

#### Scenario: Storage mutation invalidates enumeration
- **WHEN** storage changes after an enumerator is created
- **THEN** the enumerator detects the storage revision mismatch

#### Scenario: Trade publication invalidates acceptance
- **WHEN** a trade offer publishes a content update
- **THEN** its acceptance revision advances independently of the storage enumeration revision

### Requirement: Multi-container mutations use an instance transaction
`ItemContainerTransaction` MUST coordinate explicit mutation boundaries as a concrete short-lived object. It MUST deduplicate participants, acquire locks in deterministic storage order, preserve snapshot and rollback semantics, release locks before publication, and publish only committed changes. It MUST NOT introduce interfaces, ambient state, asynchronous work, service lookup, or distributed transactions. Special-domain boundaries MUST remain private and participate only through a narrow domain-owned operation.

#### Scenario: Failed settlement restores every participant
- **WHEN** settlement fails after one or more staged mutations
- **THEN** every participant returns to its pre-transaction state and no partial settlement is published

#### Scenario: Successful settlement publishes after unlocking
- **WHEN** every staged mutation succeeds
- **THEN** all storage commits, locks are released, and changed domains receive post-commit publication

### Requirement: Domain-specific empty-count semantics remain explicit
Storage MUST support removing depleted slots or retaining the item at a configured reset count. Money pouch and shop containers MUST retain their own domain behavior when using a zero reset count.

#### Scenario: Money pouch retains its coin sentinel
- **WHEN** pouch coins reach zero or an empty persisted pouch is restored
- **THEN** slot zero contains coin item 995 with count zero

#### Scenario: Shop stock normalizes depleted entries
- **WHEN** shop normalization processes depleted non-original stock
- **THEN** storage retains the configured zero-count entry and the shop applies its existing normalization and sorting behavior

### Requirement: Domain callbacks follow committed storage state
Equipment callbacks, trade settlement publication, inventory/bank/reward events, money-pouch messages, and UI updates MUST remain owned by their domain operations and MUST observe committed storage state.

#### Scenario: Equipment transfer runs callbacks before publication
- **WHEN** an equip or unequip transfer commits successfully
- **THEN** the required equipment callbacks run after both stores commit and before equipment and inventory updates publish

#### Scenario: Trade consumes generic container operations
- **WHEN** TradeExchange stages item mutations for settlement
- **THEN** it uses generic storage operations under its deterministic storage locks and publishes through domain owners only after commit

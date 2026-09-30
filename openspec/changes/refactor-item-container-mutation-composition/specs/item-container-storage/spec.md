# Spec Delta

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## MODIFIED Requirements

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST be the single implementation of `IItemContainer` and MUST compose one `ItemContainerStorage` and one `ItemContainerMutationBoundary`, exposed as `Mutations`. Ordinary domain implementations and interfaces MUST own/expose concrete `ItemContainer Items`; they MUST NOT implement `IItemContainer` or `IContainer` by forwarding generic operations. Equipment and MoneyPouch MUST compose `ItemContainerStorage` directly with a private mutation boundary and MUST NOT expose generic `Items` or their boundary. Equipment MUST itself remain a read-only `IContainer<IItem?>`. TradeOffer, Duel, Price Checker, and test fixtures MUST compose the real `ItemContainer` when they need normal generic behavior. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST NOT be part of that contract. Two-container mutations MUST be coordinated through instance methods on `ItemContainerMutationBoundary`; multi-container mutations MUST compose a short-lived `ItemContainerTransaction`. Item-container infrastructure MUST NOT require runtime casts to recover storage or a mutation boundary. `ItemContainerStorage` MUST own low-level slot and mutation algorithms and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. The mutation boundary MUST own deterministic two-container lock ordering and publication after a committed transfer; the transaction MUST own deterministic multi-container lock ordering, snapshots, rollback, and post-commit publication. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST be available through a neutral generic operation. MoneyPouch staging MUST participate through a narrow domain-owned path without exposing its private boundary.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts

#### Scenario: Generic exact removal is available to domain consumers
- **WHEN** a caller needs to remove an exact item quantity for payment or settlement
- **THEN** it uses the generic exact-removal contract and receives success or failure without a trade-specific container capability

### Requirement: Cross-container transfers commit both stores atomically
An exact cross-container transfer MUST be coordinated through instance methods on `ItemContainerMutationBoundary`, use deterministic lock ordering, call the single low-level `ItemContainerStorage` transfer algorithm, commit both stores together, and publish changed slots only after success. The staged transfer MUST return exact changed slots without publication for domain-owned callback ordering. No static gameplay transfer coordinator or storage-owner capability interface may be used.

#### Scenario: Exact transfer succeeds
- **WHEN** the source boundary has the requested quantity and the destination can accept the exact result
- **THEN** both stores commit before either domain container publishes an update

#### Scenario: Transfer fails validation
- **WHEN** the source quantity is insufficient or the destination cannot accept the result
- **THEN** neither store changes and neither boundary publishes a committed mutation

#### Scenario: Opposite transfers acquire locks consistently
- **WHEN** two operations transfer in opposite directions between the same stores
- **THEN** both operations acquire store locks in the same stable order and complete without lock-order deadlock

### Requirement: Multi-container mutations use an instance transaction
`ItemContainerTransaction` MUST be a concrete short-lived coordinator over explicit mutation boundaries. It MUST deduplicate participants, acquire locks in deterministic storage order, preserve existing snapshot and rollback semantics, release locks before publication, and publish only committed changes. It MUST NOT introduce interfaces, ambient state, asynchronous work, service lookup, or distributed transaction behavior. Special-domain boundaries MUST remain private and participate only through a narrow domain-owned operation.

#### Scenario: Failed settlement restores every participant
- **WHEN** a multi-container settlement fails after one or more staged mutations
- **THEN** every participant returns to its pre-transaction contents and no partial settlement is published

#### Scenario: Successful settlement publishes after unlocking
- **WHEN** every staged mutation succeeds
- **THEN** all participant storage is committed, locks are released, and each changed domain receives its post-commit publication

## ADDED Requirements

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision

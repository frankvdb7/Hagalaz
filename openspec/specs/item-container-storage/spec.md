# Item Container Storage

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## Requirements

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST implement `IItemContainer` and privately compose one `ItemContainerStorage` and one `ItemContainerMutationBoundary`, exposed as `IItemContainerMutationBoundary Mutations`. Ordinary domain implementations privately own concrete `ItemContainer`; ordinary domain interfaces MUST expose `IItemContainer Items` and MUST NOT copy generic forwarding operations. Equipment and MoneyPouch MUST compose storage directly with private mutation boundaries and MUST NOT expose generic `Items` or their boundaries. Equipment MUST itself remain a read-only `IContainer<IItem?>`. TradeOffer, Duel, and Price Checker MUST compose the concrete `ItemContainer` where generic behavior is required. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST NOT be part of its contract. Exact and multi-container mutations MUST use `ItemContainerTransaction` through `IItemContainerMutationBoundary` participants. No runtime cast may be required to recover storage infrastructure. `ItemContainerStorage` MUST own low-level slot and mutation mechanics and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `ItemContainerTransaction` MUST own deterministic lock ordering, snapshots, rollback, changed-slot tracking, and post-commit publication. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST be available through a neutral generic operation. MoneyPouch MUST stage pouch and inventory mutations through the same transaction without exposing its boundary or keeping a separate rollback path. Concrete storage and mutation-boundary implementations SHOULD remain assembly-internal implementation details where friend-assembly access is sufficient.

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
An exact cross-container transfer MUST enter through `IItemContainerMutationBoundary` and use `ItemContainerTransaction` to validate and plan source removal and destination insertion before changing either store, acquire distinct locks in stable order, call the single low-level `ItemContainerStorage` transfer algorithm, commit both stores together, advance each storage revision once, and publish changed slots only after success.

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
Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerTransaction` MUST own lock ordering; `ItemContainerMutationBoundary.TryTransferTo` MUST delegate exact transfers to a short-lived transaction. TradeExchange MUST use a short-lived transaction rather than acquire storage locks directly. A trade offer's acceptance `Revision` MUST remain domain-owned and MUST advance according to its existing publication semantics, independently of storage revision.

#### Scenario: Storage mutation invalidates enumeration
- **WHEN** storage changes after an enumerator is created
- **THEN** the enumerator detects the storage revision mismatch

#### Scenario: Trade publication invalidates acceptance
- **WHEN** a trade offer publishes a content update
- **THEN** its acceptance revision advances independently of the storage enumeration revision

### Requirement: Multi-container mutations use an instance transaction
`ItemContainerTransaction` MUST coordinate explicit mutation boundaries as a concrete short-lived object implementing `IItemContainerTransaction`. It MUST own transaction execution, deduplicate participants, acquire locks in deterministic storage order, capture snapshots, own rollback and changed-slot tracking, release locks before publication, and publish only committed changes. `IItemContainerTransaction` MUST expose only active staging behavior and MUST NOT expose lifecycle execution or changed-slot bookkeeping. Domain participants MUST stage all enlisted storage changes through the active transaction instead of mutating storage independently and reporting changed slots afterward. It MUST NOT introduce ambient state, asynchronous work, service lookup, or distributed transactions. Special-domain boundaries MUST remain private and participate only through a narrow domain-owned operation.

#### Scenario: A transaction participant receives a staging context
- **WHEN** a domain participant receives `IItemContainerTransaction` inside an active operation
- **THEN** it can stage item changes and register post-commit effects but cannot execute the transaction lifecycle

### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch exact additions and removals MUST stage both pouch and inventory storage through the same active transaction. Coin, slot-zero sentinel, balance, message, and event rules remain owned by MoneyPouch. Its committed domain effects MUST use `OnCommitted`, and MoneyPouch MUST NOT snapshot and restore pouch storage as a separate rollback mechanism.

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory mutations are staged but a later participant rejects its operation
- **THEN** transaction rollback restores both stores and no pouch message or event is published

### Requirement: Familiar inventory owns partial withdrawal policy
Generic mutation infrastructure MUST NOT expose partial bulk movement through `TransferAll` or `AddAndRemoveFrom`. `IFamiliarInventoryContainer` MUST expose an operation that attempts every familiar item in one transaction, commits fitting transfers together, and leaves items that do not fit in familiar storage.

#### Scenario: Familiar withdrawal has fitting and non-fitting items
- **WHEN** only some familiar items fit in the owner's inventory
- **THEN** fitting items move, non-fitting items remain, and each changed container publishes once

#### Scenario: No familiar item fits
- **WHEN** the owner's inventory cannot accept any familiar item
- **THEN** both containers remain unchanged and publish no mutation

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

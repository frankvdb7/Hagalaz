# Spec Delta

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## MODIFIED Requirements

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST be the single implementation of `IItemContainer` and MUST privately compose one `ItemContainerStorage` and one concrete `ItemContainerMutationBoundary`, exposed as `IItemContainerMutationBoundary Mutations`. Ordinary domain interfaces MUST expose `IItemContainer Items`; implementations may privately own concrete `ItemContainer`; they MUST NOT implement `IItemContainer` or `IContainer` by forwarding generic operations. Equipment and MoneyPouch MUST compose `ItemContainerStorage` directly with a private mutation boundary and MUST NOT expose generic `Items` or their boundary. Equipment MUST itself remain a read-only `IContainer<IItem?>` and MUST NOT expose publication-only methods. TradeOffer, Duel, Price Checker, and test fixtures MUST compose the real `ItemContainer` when they need normal generic behavior. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST NOT be part of that contract. Exact and multi-container mutations MUST be coordinated through `ItemContainerTransaction` implementing `IItemContainerTransaction` over boundary interfaces. Item-container infrastructure MUST NOT require runtime casts to recover storage or a mutation boundary. `ItemContainerStorage` MUST own low-level slot and mutation algorithms and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. The transaction MUST own deterministic locking, snapshots, rollback, changed-slot tracking, and publication after unlocking. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST be available through a neutral generic operation. MoneyPouch MUST stage pouch and inventory mutations through one transaction without exposing its private boundary or implementing a second rollback path. Concrete storage and mutation-boundary implementations SHOULD remain assembly-internal implementation details when friend-assembly access is sufficient.

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
An exact cross-container transfer MUST enter through `IItemContainerMutationBoundary` and use `ItemContainerTransaction` for deterministic lock ordering, snapshots, rollback, and publication. It MUST call the single low-level `ItemContainerStorage` transfer algorithm, commit both stores together, and publish changed slots only after success. Domain-owned callbacks MUST run after unlocking and before publication where the established operation order requires it. No static gameplay transfer coordinator or storage-owner capability interface may be used.

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
`ItemContainerTransaction` MUST be a concrete short-lived coordinator implementing `IItemContainerTransaction` over explicit `IItemContainerMutationBoundary` participants. It MUST own execution, participant coordination, deterministic locking, snapshot capture, rollback during the locked reversible phase, changed-slot tracking, and post-commit publication. `IItemContainerTransaction.Include` MUST support participant enlistment before execution. Its storage-staging methods and `OnCommitted` MUST be used only inside the operation callback passed to concrete `ItemContainerTransaction.TryExecute`. The interface MUST NOT expose transaction execution or changed-slot bookkeeping. Domain containers MUST NOT mutate enlisted storage independently and report changed slots afterward; every staged storage mutation MUST pass through the active transaction. It MUST deduplicate participants, preserve existing snapshot and rollback semantics, and release locks before post-commit work. Once a successful operation callback returns and participant locks are released, storage MUST be considered committed and MUST NOT be rolled back or reported as a false result because of later callback or publication failure. The transaction MUST attempt every registered pre-publication callback, every changed participant publisher in participant-registration order, and every `OnCommitted` callback exactly once and in that order. One post-commit exception MUST be rethrown with its original stack after all actions are attempted; multiple exceptions MUST be reported together in an `AggregateException`. It MUST NOT introduce ambient state, asynchronous work, service lookup, or distributed transaction behavior. Special-domain boundaries MUST remain private and participate only through a narrow domain-owned operation.

#### Scenario: A transaction participant receives a staging context
- **WHEN** a domain participant receives `IItemContainerTransaction` inside the `TryExecute` operation callback
- **THEN** it can stage item changes and register post-commit effects, while participant enlistment through `Include` remains available only before execution

#### Scenario: Failed transaction restores storage revision
- **GIVEN** an enumerator exists before a multi-container transaction begins
- **WHEN** the transaction stages one or more storage changes but later rolls back
- **THEN** all participant storage contents, item identities, counts, and mutation revisions are restored
- **AND** the pre-existing enumerator remains valid
- **AND** no participant publishes a committed mutation or runs a committed callback

#### Scenario: Empty bulk addition is not a mutation
- **GIVEN** an item container and an enumerator created before the operation
- **WHEN** `TryAddRange` receives no effective items, including an empty range or a range containing only null entries
- **THEN** the operation succeeds with no changed slots
- **AND** storage contents and mutation revision remain unchanged
- **AND** the existing enumerator remains valid
- **AND** no participant mutation publication occurs

### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch additions, removals, inventory transfers, and transfers to/from bank, shop, or duel stake MUST coordinate every affected pouch and inventory storage through one `ItemContainerTransaction`. Exact staged additions and removals MUST remain the single implementation of pouch overflow/underflow rules. MoneyPouch MUST keep coin, sentinel, balance, message, and event semantics in its domain implementation, register committed effects through `OnCommitted`, and MUST NOT mutate either store independently, compensate with a second mutation, or snapshot/restore its storage as another rollback mechanism. Existing partial-count behavior for `Remove`, `AddFromInventory`, and `MoveToInventory` MUST remain.

#### Scenario: Pouch overflow cannot partially commit
- **WHEN** an addition must overflow into inventory but inventory cannot accept the overflow
- **THEN** neither pouch nor inventory changes and no committed pouch message or event is emitted

#### Scenario: Pouch removal spans both stores atomically
- **WHEN** a requested partial removal is available across pouch and inventory
- **THEN** the actual amount is removed from both through one transaction, or neither store changes

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory mutations are staged but a later participant rejects its operation
- **THEN** transaction rollback restores both stores and no pouch message or event is published

### Requirement: Economic ownership changes use one transaction owner
Bank deposits from MoneyPouch, shop purchases and sales, and duel stake, return, and cancellation refund MUST stage all affected containers through `ItemContainerTransaction` or a two-container mutation boundary. They MUST NOT use independent remove/add operations with compensating mutation. Existing domain policy for capacity, amount clamping, prices, messages, and stock normalization MUST remain in its owning domain method.

#### Scenario: Shop payment and item delivery are atomic
- **WHEN** either payment or item delivery fails during a shop purchase or sale
- **THEN** every participating inventory, pouch, and shop store retains its pre-operation state and no purchase event is emitted

#### Scenario: Bank deposit from pouch is atomic
- **WHEN** bank storage cannot accept a staged pouch deposit
- **THEN** bank and pouch remain unchanged and no committed bank or pouch publication occurs

#### Scenario: Duel cancellation refund is atomic
- **WHEN** either player's stake cannot be refunded
- **THEN** both stake containers and both players' destination stores retain their pre-refund state

### Requirement: Duel stake mutations use a composed domain collaborator
`DuelArenaScript` MUST compose one concrete `DuelStakeExchange` for inventory/pouch stake, return, and cancellation-refund mutations. `DuelStakeExchange` MUST use mutation boundaries for simple exact two-container transfers and `ItemContainerTransaction` when MoneyPouch or both players participate. It MUST NOT own duel UI or session state and MUST NOT have a new interface or service registration.

#### Scenario: Duel cancellation cannot discard escrow
- **WHEN** a combined refund of both stake containers fails
- **THEN** neither stake container is cleared and duel scripts/session teardown do not proceed

#### Scenario: Duel stake transfer fails
- **WHEN** a stake transfer cannot accept the exact requested item quantity
- **THEN** neither source nor destination storage changes or publishes a committed mutation

### Requirement: Equipment replacement preflights unequip permission
Before the first mutation for an equipment replacement, `EquipmentContainer` MUST identify every conflicting equipped item and require `CanUnEquipItem` to succeed for each. On rejection it MUST leave inventory and equipment unchanged and invoke no equip/unequip callbacks. For occupied replacement slots other than Weapon and Shield, successful preflight MUST preserve existing `IEquipmentScript.UnEquipItem` command behavior, including custom and interactive behavior.

#### Scenario: A later conflicting item rejects replacement
- **WHEN** one conflicting equipped item permits unequipping and a later conflict rejects it
- **THEN** the incoming item and all existing equipment remain unchanged and no mutation publication or equipment callback occurs

### Requirement: Weapon and shield replacement uses one exact storage transaction
After weapon/shield conflict preflight succeeds, `EquipmentContainer` MUST stage exact removal of the incoming inventory item, transfers of the conflicting weapon and shield to inventory, and exact-slot insertion of the same incoming instance into equipment through one `ItemContainerTransaction`. Exact-slot insertion MUST be an internal concrete transaction operation and MUST NOT widen `IItemContainerTransaction`. Any staging failure MUST roll back all storage changes without callbacks or publication. This path MUST NOT invoke `IEquipmentScript.UnEquipItem` commands. On commit, it MUST run the weapon `OnUnequipped`, shield `OnUnequipped`, weapon profile/special-attack logic when applicable, and incoming `OnEquipped` callbacks in that order after unlocking and before participant publication.

#### Scenario: A later weapon or shield transfer cannot fit
- **WHEN** conflict preflight succeeds but a later conflicting item cannot fit in inventory
- **THEN** inventory and equipment remain unchanged and no lifecycle callback or publication occurs

#### Scenario: Weapon and shield replacement succeeds
- **WHEN** all required storage operations can be staged
- **THEN** the final storage contains both outgoing instances in inventory and the original incoming instance in equipment before callbacks and publication

#### Scenario: Publication fails after a successful replacement
- **WHEN** a participant publisher throws after commit
- **THEN** storage stays committed and all lifecycle callbacks and remaining publishers are attempted

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory changes have been staged but a later participant rejects its operation
- **THEN** the transaction restores both pouch and inventory, and no pouch message or event is published

### Requirement: Familiar withdrawal owns partial bulk movement
Generic mutation infrastructure MUST NOT expose partial bulk movement with `TransferAll` or `AddAndRemoveFrom` semantics. `IFamiliarInventoryContainer` MUST expose a domain operation that attempts every familiar item in one transaction, commits fitting transfers together, and leaves items that cannot fit in familiar storage.

#### Scenario: Some familiar items fit
- **WHEN** familiar withdrawal has both fitting and non-fitting items
- **THEN** fitting items move, non-fitting items remain, and each affected container publishes once after the transaction commits

#### Scenario: No familiar items fit
- **WHEN** the owner's inventory cannot accept any familiar item
- **THEN** neither container changes and neither publishes a mutation

#### Scenario: Failed settlement restores every participant
- **WHEN** a multi-container settlement fails after one or more staged mutations
- **THEN** every participant returns to its pre-transaction contents and no partial settlement is published

#### Scenario: Successful settlement publishes after unlocking
- **WHEN** every staged mutation succeeds
- **THEN** all participant storage is committed, locks are released, and each changed domain receives its post-commit publication

## ADDED Requirements

### Requirement: Trade orchestration is explicitly composed
Trade settlement, refund, escrow recovery, and money-offer coordination MUST be performed by a composed `TradeExchange` instance rather than static trade orchestration. `TradingCharacterScript` MUST own or otherwise explicitly compose its `TradeExchange` collaborator. `TradeExchange` MUST own its `IItemBuilder` dependency and MUST depend on generic container abstractions, including `IItemContainer`, `IItemContainerMutationBoundary`, and `IItemContainerTransaction` as appropriate. It MUST NOT require concrete `ItemContainer`, `ItemContainerStorage`, or manual container or MoneyPouch publication. `TradeExchange` MUST NOT own trade-session state.

#### Scenario: Trading script settles through its collaborator
- **WHEN** a trading script completes or refunds an accepted trade
- **THEN** it delegates the operation to its owned `TradeExchange` instance, which coordinates abstract container participants and commits through the transaction contract

#### Scenario: Trade orchestration does not recover infrastructure
- **WHEN** trade settlement or escrow recovery runs
- **THEN** it does not access raw storage, acquire storage locks, restore snapshots, or manually publish container or MoneyPouch changes

### Requirement: Public item-container contracts remain implementation-independent
Ordinary domain interfaces MUST expose `IItemContainer Items`, never concrete `ItemContainer`. `IItemContainer` MUST expose `IItemContainerMutationBoundary`. Generic mutation code MUST NOT cast an `IItemContainer` to `ItemContainer`, and mutation code MUST NOT cast an `IItemContainerMutationBoundary` to `ItemContainerMutationBoundary`. `ItemContainerTransaction` MUST implement `IItemContainerTransaction`, accept and coordinate boundary interfaces, and MUST NOT cast participants to concrete boundaries. Cross-domain transaction APIs MUST use `IItemContainerTransaction`. Raw `ItemContainerStorage` MUST NOT appear in domain-facing interfaces. Interfaces MUST remain declaration-only; no default implementation, behavior-sharing base class, or generic forwarding layer may be introduced. Equipment and MoneyPouch MUST keep their concrete boundaries private. Cross-container mutation MUST remain instance-based and MUST NOT use a static transfer coordinator. The internal boundary enlistment bridge MAY reference concrete transaction infrastructure only to register privately owned storage and publication; it MUST NOT expose storage or locks.

#### Scenario: Ordinary callers use abstract containers end to end
- **WHEN** a consumer receives inventory and bank interfaces
- **THEN** it can access `IItemContainer` items and perform transfers through `IItemContainerMutationBoundary` without implementation casts

#### Scenario: Cross-domain staging uses the transaction contract
- **WHEN** MoneyPouch participates in a composed mutation
- **THEN** it uses semantic exact staging operations and `IItemContainerTransaction.OnCommitted` without exposing storage methods or manual publication

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision

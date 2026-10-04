# Spec Delta

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## MODIFIED Requirements

### Requirement: MoneyPouch separates gameplay and mutation capabilities
`IMoneyPouchContainer` MUST expose normal coin-domain operations separately from transaction staging. Transaction participation MUST be available through `IMoneyPouchMutationBoundary` exposed by `MoneyPouch.Mutations`. Public exact methods MUST always own a new transaction; mutation-boundary exact methods MUST require the caller-owned active transaction to include pouch storage and every inventory contribution. The primary MoneyPouch API MUST NOT expose generic item-ID `Contains` operations or staging methods directly.

#### Scenario: MoneyPouch coin availability is domain-specific
- **WHEN** a caller checks whether a character has a positive coin amount
- **THEN** `HasCoins` compares the request against pouch coins and inventory coin item `995` using overflow-safe addition

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST be the single implementation of `IItemContainer` and MUST privately compose one `ItemContainerStorage` and one concrete `ItemContainerMutationBoundary`, exposed through `IItemContainerTransactionParticipant Mutations`. Ordinary domain interfaces MUST expose `IItemContainer Items`; implementations may privately own concrete `ItemContainer`; they MUST NOT implement `IItemContainer` or `IContainer` by forwarding generic operations. Equipment and MoneyPouch MUST compose `ItemContainerStorage` directly with a private mutation boundary and MUST NOT expose generic `Items` or their concrete boundary. Equipment MUST expose only its read-only `IReadOnlyItemContainer Items` view and domain operations. TradeOffer, Duel, Price Checker, and test fixtures MUST compose the real `ItemContainer` when they need normal generic behavior. `IItemContainer` MUST expose semantic container operations, including exact transfer; transaction participation MUST be represented only by the opaque `IItemContainerTransactionParticipant` returned from `.Mutations`. `ItemContainerTransaction.Begin` MUST receive all public mutation participants up front, acquire unique storage locks in deterministic order, capture snapshots, and bind participants only after successful construction. Ordinary `IItemContainer` mutations MUST use an already enlisted storage scope and MUST remain standalone when storage is not bound. The transaction MUST own rollback-on-dispose, point-of-no-return, deterministic unlocking, changed-slot tracking, and publication after unlocking. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST be available through the ordinary container API. MoneyPouch MUST contribute pouch and inventory storage through `IMoneyPouchMutationBoundary`; it MUST NOT expose a concrete storage boundary or maintain a separate rollback path. Concrete storage and mutation-boundary implementations MUST remain internal; Scripts MUST use public domain capabilities and MUST NOT receive friend-assembly access.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts

#### Scenario: Generic exact removal is available to domain consumers
- **WHEN** a caller needs to remove an exact item quantity for payment or settlement
- **THEN** it uses the generic exact-removal contract and receives success or failure without a trade-specific container capability


### Requirement: Equipment exposes semantic mutation operations
`IEquipmentContainer` MUST expose domain-specific mutation operations rather than generic `Add`, `Replace`, `Remove`, and `Clear` commands. Direct restoration, in-place transformation, removal, and clearing MUST use explicitly named equipment operations. Equipment replacement MUST require the caller's expected current item instance so a stale command cannot replace a different equipped item. Standalone semantic Equipment operations MUST reject transaction-bound equipment storage and MUST NOT implicitly participate in another caller's transaction. Explicit transaction-owning Equipment workflows may use the private mutation boundary and typed deferred completion facts.

#### Scenario: Stale equipment replacement is rejected
- **WHEN** the expected item instance no longer occupies the requested slot
- **THEN** replacement returns false without changing storage, publishing an equipment update, or running lifecycle callbacks

#### Scenario: Equipment storage mutation participates in an active scope
- **WHEN** restoration, replacement, removal, or clearing is called while Equipment storage belongs to an active transaction
- **THEN** the storage mutation participates in that transaction, with lifecycle completion and publication deferred until commit

### Requirement: Equipment publishes only after lifecycle effects
Direct equipment restoration, replacement, full removal, and clearing MUST publish committed equipment state only after all required equipment lifecycle effects have been attempted. Storage MUST commit before lifecycle callbacks run. Clearing MUST remove all equipped items before callbacks and attempt `OnUnequipped` for every previously equipped item. Post-commit lifecycle or publication failures MUST NOT roll back committed storage. Every post-commit action MUST be attempted; one failure MUST preserve and rethrow the original exception, while multiple failures MUST be aggregated.

#### Scenario: Equipment replacement callbacks precede publication
- **WHEN** an expected equipped item is replaced
- **THEN** storage contains the replacement before `OnUnequipped` and `OnEquipped`, and publication follows both callback attempts

#### Scenario: Full equipment removal callback precedes publication
- **WHEN** an equipped item is fully removed
- **THEN** storage no longer contains it before `OnUnequipped`, and publication follows the callback attempt

#### Scenario: Equipment clear exhausts callbacks after storage clear
- **WHEN** equipment containing one or more items is cleared
- **THEN** storage is empty before callbacks, every prior item's `OnUnequipped` is attempted, and publication follows all callback attempts

### Requirement: ItemContainer storage stays encapsulated
Ordinary domain owners that compose `ItemContainer` MUST NOT recover or access its `ItemContainerStorage` instance. Persistence and specialized state maintenance MUST use narrow internal `ItemContainer` operations. Direct `ItemContainerStorage` ownership is reserved for domain containers whose invariants require bypassing the generic facade, currently `EquipmentContainer` and `MoneyPouchContainer`.

#### Scenario: Ordinary container persistence uses the facade
- **WHEN** an ordinary domain owner hydrates, dehydrates, or normalizes item state
- **THEN** it uses narrow `ItemContainer` operations and does not obtain raw storage

### Requirement: Cross-container transfers commit both stores atomically
An exact cross-container transfer MUST enter through `IItemContainer.TryTransferTo(...)` and use the one caller-owned `ItemContainerTransaction` to validate and plan source removal and destination insertion before changing either store, acquire distinct locks in stable order, call the single low-level `ItemContainerStorage` transfer algorithm, advance each storage revision once, and publish changed slots only after successful commit. The transfer method MUST NOT create or commit a transaction or dynamically enlist the destination. A standalone domain owner MUST supply both participants before mutation; a composable operation MUST use the existing transaction only when both storages belong to it.

#### Scenario: Exact transfer succeeds
- **WHEN** the source boundary has the requested quantity and the destination can accept the exact result
- **THEN** both stores commit before either domain container publishes an update

#### Scenario: Transfer fails validation
- **WHEN** the source quantity is insufficient or the destination cannot accept the result
- **THEN** neither store changes and neither boundary publishes a committed mutation

#### Scenario: Storage transfer requires one active transaction
- **WHEN** the transfer boundary is called without both boundaries enlisted in one active transaction, or the low-level source storage transfer primitive is called without a transaction shared by source and destination or from a thread other than the transaction owner
- **THEN** it throws before either store changes

#### Scenario: Opposite transfers acquire locks consistently
- **WHEN** two operations transfer in opposite directions between the same stores
- **THEN** their owner scopes acquire store locks in the same stable order, and an independent scope waits until the current scope releases its locks

#### Scenario: Transfer is exposed by the container
- **WHEN** a consumer has source and destination values typed as `IItemContainer`
- **THEN** it performs transfer through `IItemContainer.TryTransferTo(...)` while `.Mutations` remains only an opaque transaction participant

### Requirement: Multi-container mutations use an instance transaction
`ItemContainerTransaction` MUST expose only `Begin`, `Commit`, and `Dispose` as its public lifecycle. It MUST accept all participants before locking, deduplicate storage aliases, lock deterministically, capture rollback snapshots, restore on uncommitted disposal, make storage irreversible on commit, release bindings and locks, then publish once. It MUST NOT expose item mutations, dynamic enlistment, transaction state, publication methods, callback registration, ambient joining, or a separate unit-of-work abstraction. Domain operations MUST own item semantics. Transaction membership is declared through `.Mutations` participants passed to `Begin`. Ordinary `IItemContainer` operations automatically participate when their backing storage is enlisted and remain standalone otherwise. `IItemContainer.TryTransferTo` requires source and destination storage in the same active current-thread transaction and MUST NOT create or join another scope. MoneyPouch exact methods own a transaction when all required storage is unbound, participate when all required storage belongs to the same active transaction, and reject partial or conflicting enlistment before mutation. Publication and hooks MUST run only after all transaction locks are released. Existing rollback, publication ordering, failure propagation, and completion ownership MUST be preserved. If Commit cannot fully release transaction resources, observable completion and publication MUST NOT start, pending domain completion facts MUST still be discarded as non-observable cleanup, and committed storage MUST remain irreversible.

#### Scenario: A transaction owns all explicit participants
- **WHEN** a caller begins a transaction with every required participant, stages changes, and commits
- **THEN** all changed participants publish once after bindings and locks are released

#### Scenario: A transaction participant uses explicit mutation capabilities
- **WHEN** a caller begins one transaction with every required participant and performs domain mutations
- **THEN** `.Mutations` is used only to declare scope membership and container mutations use their normal domain APIs

#### Scenario: Ordinary mutations use enlisted storage automatically
- **WHEN** a caller begins one transaction with a container participant and uses ordinary container mutation methods
- **THEN** those methods participate in the scope and use its locks without acquiring additional storage

#### Scenario: A cross-storage mutation has no owner scope
- **WHEN** a caller attempts a cross-storage mutation without an active transaction containing both boundaries
- **THEN** the operation throws before changing either storage

#### Scenario: A helper requires missing storage
- **WHEN** a scope includes A but a composable operation requires A and B
- **THEN** the operation throws without acquiring or mutating B, and A remains bound to its existing scope

#### Scenario: Storage belongs to different scopes
- **WHEN** an operation requires storage bound to different active transactions
- **THEN** it throws without mutation or nested transaction creation

#### Scenario: MoneyPouch exact operation selects the existing complete scope
- **WHEN** a public MoneyPouch exact operation is called with all required storage unbound
- **THEN** it owns one transaction over the pouch and inventory
- **WHEN** it is called with all required storage enlisted in the same active current-thread transaction
- **THEN** it participates in that transaction without starting another scope
- **WHEN** only some required storage is enlisted or required storage belongs to different transactions
- **THEN** it throws before mutation

#### Scenario: Ordinary item-container mutation participates in an enlisted store
- **WHEN** an ordinary `IItemContainer` mutation is called for storage enlisted in an active transaction
- **THEN** it mutates within that scope and defers publication until commit

#### Scenario: A composite participant aliases storage
- **WHEN** ordinary and composite participants contribute the same storage
- **THEN** storage is locked and snapshotted once and participant publication order remains first-seen order

#### Scenario: Failed transaction restores storage revision
- **GIVEN** an enumerator exists before a multi-container transaction begins
- **WHEN** the transaction stages one or more storage changes but later rolls back
- **THEN** all participant storage contents, item identities, counts, and mutation revisions are restored
- **AND** the pre-existing enumerator remains valid
- **AND** no participant publishes a committed mutation or runs a committed callback

#### Scenario: Empty bulk addition is not a mutation
- **GIVEN** an item container and an enumerator created before the operation
- **WHEN** `AddRange` receives no effective items, including an empty range or a range containing only null entries
- **THEN** the operation succeeds with no changed slots
- **AND** storage contents and mutation revision remain unchanged
- **AND** the existing enumerator remains valid
- **AND** no participant mutation publication occurs

### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch additions, removals, inventory transfers, and transfers to/from bank, shop, or duel stake MUST coordinate every affected pouch and inventory storage through one `ItemContainerTransaction`. Public exact operations MUST always own their transaction; `IMoneyPouchMutationBoundary` exact operations MUST participate in an existing transaction and validate complete pouch plus inventory enlistment before mutation. Exact pouch core rules MUST remain single-sourced. MoneyPouch MUST keep coin, sentinel, balance, message, and event semantics in its domain implementation, capture immutable previous/new/change facts per mutation, and MUST NOT mutate either store independently, compensate with a second mutation, or snapshot/restore its storage as another rollback mechanism. Existing partial-count behavior for `Remove`, `AddFromInventory`, and `MoveToInventory` MUST remain.

#### Scenario: Pouch overflow cannot partially commit
- **WHEN** an addition must overflow into inventory but inventory cannot accept the overflow
- **THEN** neither pouch nor inventory changes and no committed pouch message or event is emitted

#### Scenario: Pouch removal spans both stores atomically
- **WHEN** a requested partial removal is available across pouch and inventory
- **THEN** the actual amount is removed from both through one transaction, or neither store changes

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory mutations are staged but a later participant rejects its operation
- **THEN** transaction rollback restores both stores and no pouch message or event is published

### Requirement: Duel stake mutations use a composed domain collaborator
`DuelArenaScript` MUST compose one concrete `DuelStakeExchange` for inventory/pouch stake, return, and cancellation-refund mutations. `DuelStakeExchange` MUST use mutation boundaries for simple exact two-container transfers and `ItemContainerTransaction` when MoneyPouch or both players participate. It MUST NOT own duel UI or session state and MUST NOT have a new interface or service registration.

#### Scenario: Duel cancellation cannot discard escrow
- **WHEN** a combined refund of both stake containers fails
- **THEN** neither stake container is cleared and duel scripts/session teardown do not proceed

#### Scenario: Duel stake transfer fails
- **WHEN** a stake transfer cannot accept the exact requested item quantity
- **THEN** neither source nor destination storage changes or publishes a committed mutation

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision

## ADDED Requirements

### Requirement: Economic ownership changes use one transaction owner
Bank deposits from MoneyPouch, shop purchases and sales, and duel stake, return, and cancellation refund MUST stage all affected containers through `ItemContainerTransaction` and use normal domain/container operations. They MUST NOT use independent remove/add operations with compensating mutation. Existing domain policy for capacity, amount clamping, prices, messages, and stock normalization MUST remain in its owning domain method.

#### Scenario: Shop payment and item delivery are atomic
- **WHEN** either payment or item delivery fails during a shop purchase or sale
- **THEN** every participating inventory, pouch, and shop store retains its pre-operation state and no purchase event is emitted

#### Scenario: Bank deposit from pouch is atomic
- **WHEN** bank storage cannot accept a staged pouch deposit
- **THEN** bank and pouch remain unchanged and no committed bank or pouch publication occurs

#### Scenario: Duel cancellation refund is atomic
- **WHEN** either player's stake cannot be refunded
- **THEN** both stake containers and both players' destination stores retain their pre-refund state

### Requirement: Equipment replacement preflights unequip permission
Before the first mutation for an equipment replacement, `EquipmentContainer` MUST identify every conflicting equipped item and require `CanUnEquipItem` to succeed for each. On rejection it MUST leave inventory and equipment unchanged and invoke no equip/unequip callbacks. For occupied replacement slots other than Weapon and Shield, successful preflight MUST preserve existing `IEquipmentScript.UnEquipItem` command behavior, including custom and interactive behavior.

#### Scenario: A later conflicting item rejects replacement
- **WHEN** one conflicting equipped item permits unequipping and a later conflict rejects it
- **THEN** the incoming item and all existing equipment remain unchanged and no mutation publication or equipment callback occurs

### Requirement: Weapon and shield replacement uses one exact storage transaction
After weapon/shield conflict preflight succeeds, `EquipmentContainer` MUST stage exact removal of the incoming inventory item, transfers of the conflicting weapon and shield to inventory, and exact-slot insertion of the same incoming instance into equipment through one `ItemContainerTransaction`. Exact-slot insertion MUST remain a private Equipment storage operation and MUST NOT widen any public mutation capability or expose transaction internals. Any staging failure MUST roll back all storage changes without callbacks or publication. This path MUST NOT invoke `IEquipmentScript.UnEquipItem` commands. On commit, it MUST run the weapon `OnUnequipped`, shield `OnUnequipped`, weapon profile/special-attack logic when applicable, and incoming `OnEquipped` callbacks in that order after unlocking and before participant publication.

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
Trade settlement, refund, escrow recovery, and money-offer coordination MUST be performed by a composed `TradeExchange` instance rather than static trade orchestration. `TradingCharacterScript` MUST own or otherwise explicitly compose its `TradeExchange` collaborator. `TradeExchange` MUST own its `IItemBuilder` dependency and MUST depend on generic container abstractions, including `IItemContainer`, plus `IMoneyPouchMutationBoundary` when a pouch participates. It MUST begin one transaction with every required `.Mutations` participant before performing composed changes and perform container transfers through `IItemContainer.TryTransferTo(...)`. Standalone operations MUST NOT inspect active storage bindings or implicitly join. It MUST NOT require concrete `ItemContainer`, `ItemContainerStorage`, transaction internals, or manual container or MoneyPouch publication. `TradeExchange` MUST NOT own trade-session state.

#### Scenario: Trading script settles through its collaborator
- **WHEN** a trading script completes or refunds an accepted trade
- **THEN** it delegates the operation to its owned `TradeExchange` instance, which begins one transaction with all required mutation participants and commits through `ItemContainerTransaction`

#### Scenario: Trade orchestration does not recover infrastructure
- **WHEN** trade settlement or escrow recovery runs
- **THEN** it does not access raw storage, acquire storage locks, restore snapshots, or manually publish container or MoneyPouch changes

### Requirement: Public item-container contracts remain implementation-independent
Ordinary domain interfaces MUST expose `IItemContainer Items`, never concrete `ItemContainer`. `IItemContainer.Mutations` MUST expose only the opaque `IItemContainerTransactionParticipant`. `IItemContainer` MUST expose `TryTransferTo(...)`; it MUST NOT expose storage or transaction internals. Generic mutation code MUST NOT cast an `IItemContainer` to `ItemContainer` except inside its concrete implementation when validating supported destinations. `ItemContainerTransaction.Begin` MUST accept the public participant marker and resolve only supported mutation boundaries; there MUST NOT be an `IItemContainerTransaction` API. Cross-domain transaction callers MUST pass `.Mutations` participants to `Begin` and perform operations through normal container/domain operations. Raw `ItemContainerStorage` MUST NOT appear in domain-facing interfaces. Interfaces MUST remain declaration-only; no default implementation, behavior-sharing base class, or generic forwarding layer may be introduced. Equipment and MoneyPouch MUST keep concrete boundaries private, with only the typed pouch mutation capability public where cross-assembly composition requires it. Cross-container mutation MUST remain instance-based and MUST NOT use a static transfer coordinator. The internal boundary enlistment bridge MAY register privately owned storage and publication, but MUST NOT expose storage or locks.

#### Scenario: Ordinary callers use abstract containers end to end
- **WHEN** a consumer receives inventory and bank interfaces
- **THEN** it can access `IItemContainer` items and perform transfers through `IItemContainer.TryTransferTo(...)` without implementation casts

#### Scenario: Cross-domain mutation uses the public pouch capability
- **WHEN** MoneyPouch participates in a composed mutation
- **THEN** the caller enlists `MoneyPouch.Mutations` and invokes its exact operation without exposing storage methods or manual publication

# Item Container Storage

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## Requirements

### Requirement: Equipment exposes semantic mutation operations
`IEquipmentContainer` MUST expose domain-specific mutation operations rather than generic `Add`, `Replace`, `Remove`, and `Clear` commands. Direct restoration, in-place transformation, removal, and clearing MUST use explicitly named equipment operations. Equipment replacement MUST require the caller's expected current item instance so a stale command cannot replace a different equipped item. Simple Equipment storage operations MUST participate when Equipment storage is enlisted; typed lifecycle completion MUST be deferred through the owning transaction. When unbound, lifecycle completion and publication occur immediately. `TryMoveTo(...)` MUST use direct two-boundary ownership when standalone, participate without committing or disposing when both belong to the same active current-thread transaction, and throw before mutation for partial or conflicting participation. Full removal MUST defer lifecycle completion until the owning transaction commits or run the standalone lifecycle effect before publication. `EquipItem()` and `UnEquipItem()` MUST remain standalone gameplay commands and MUST NOT participate in a caller-owned transaction over their required storage.

#### Scenario: Stale equipment replacement is rejected
- **WHEN** the expected item instance no longer occupies the requested slot
- **THEN** replacement returns false without changing storage, publishing an equipment update, or running lifecycle callbacks

#### Scenario: Equipment storage mutation participates in an active scope
- **WHEN** restoration, replacement, removal, or clearing is called while Equipment storage belongs to an active transaction
- **THEN** the storage mutation participates in that transaction, with lifecycle completion and publication deferred until commit

#### Scenario: Equipment transfer uses direct standalone ownership
- **WHEN** `TryMoveTo(...)` moves an equipped item while neither Equipment nor destination belongs to a current-thread transaction
- **THEN** it atomically mutates both storages without creating an `ItemContainerTransaction`, runs `OnUnequipped` before publication for full removal, and retains both boundaries' ownership through publication

#### Scenario: Equipment transfer participates in a caller-owned transaction
- **WHEN** `TryMoveTo(...)` moves an equipped item while Equipment and destination belong to the same active current-thread transaction
- **THEN** it stages the move and defers lifecycle completion and publication until the caller commits

#### Scenario: Equipment transfer rejects partial or conflicting participation
- **WHEN** only one required storage is enlisted or required storages belong to different active current-thread transactions
- **THEN** `TryMoveTo(...)` throws before changing either storage

#### Scenario: Equipment gameplay commands remain standalone
- **WHEN** a caller invokes `EquipItem()` or `UnEquipItem()` for an operation requiring inventory and Equipment
- **THEN** the command owns its workflow and does not join a caller-owned transaction over those storages

### Requirement: Equipment publishes only after lifecycle effects
Direct equipment restoration, replacement, full removal, and clearing MUST publish committed equipment state only after all required equipment lifecycle effects have been attempted. Storage MUST commit before lifecycle callbacks run. Clearing MUST remove all equipped items before callbacks and attempt `OnUnequipped` for every previously equipped item. An occupied replacement MUST obtain any required approval before mutation and replace inventory and Equipment storage atomically. Post-commit lifecycle or publication failures MUST NOT roll back committed storage. Every post-commit action MUST be attempted; one failure MUST preserve and rethrow the original exception, while multiple failures MUST be aggregated.

#### Scenario: Equipment replacement callbacks precede publication
- **WHEN** an expected equipped item is replaced
- **THEN** storage contains the replacement before `OnUnequipped` and `OnEquipped`, and publication follows both callback attempts

#### Scenario: Approved non-weapon replacement is atomic
- **WHEN** an incoming item replaces an occupied non-weapon/non-shield equipment slot after approval
- **THEN** the incoming and displaced item instances are swapped atomically, lifecycle callbacks run after storage commit and unlock, and publication follows both callback attempts

#### Scenario: Deferred equipment approval leaves storage unchanged
- **WHEN** an occupied replacement requests UI approval
- **THEN** inventory and Equipment remain unchanged until acceptance, and the accepted continuation revalidates item identity and eligibility before mutation

#### Scenario: Full equipment removal callback precedes publication
- **WHEN** an equipped item is fully removed
- **THEN** storage no longer contains it before `OnUnequipped`, and publication follows the callback attempt

#### Scenario: Equipment clear exhausts callbacks after storage clear
- **WHEN** equipment containing one or more items is cleared
- **THEN** storage is empty before callbacks, every prior item's `OnUnequipped` is attempted, and publication follows all callback attempts

### Requirement: MoneyPouch separates gameplay operations from transaction participation
`IMoneyPouchContainer` MUST expose normal coin-domain operations and implement the empty public `IItemTransactional` capability marker. `Add` MUST add the entire requested amount or leave storage unchanged, using inventory capacity for pouch overflow. It MUST NOT expose a `Mutations` property or storage boundary. Its concrete domain implementation MUST provide exactly its pouch storage and one inventory item-storage boundary through the internal `IItemTransactionSource`. `Add` and `TryRemoveExact` MUST own a transaction when the current thread owns neither required boundary, participate in the same active current-thread transaction when it owns both, and throw before mutation when the inventory boundary is already owned by a current-thread transaction that does not include the pouch boundary. A foreign transaction MUST be handled by the standalone transaction's normal contention behavior. `AddFromInventory` and `MoveToInventory` MUST reject invocation when the current thread already owns a transaction or standalone publication involving either the pouch or inventory boundary, before validation, messaging, or mutation. Foreign contention for these standalone commands MUST retain normal standalone waiting behavior. The primary MoneyPouch API MUST NOT expose generic item-ID `Contains`, a domain-specific mutation boundary, or caller-managed publication receipts.

#### Scenario: MoneyPouch coin availability is domain-specific
- **WHEN** a caller checks whether a character has a positive coin amount
- **THEN** `HasCoins` compares the request against pouch coins and inventory coin item `995` using overflow-safe addition

#### Scenario: Fully enlisted MoneyPouch inventory command is rejected before side effects
- **WHEN** `AddFromInventory` or `MoveToInventory` is called while the current thread owns a transaction containing the pouch and inventory
- **THEN** it throws before validation, messaging, or storage mutation

#### Scenario: Inventory-only MoneyPouch command participation is rejected
- **WHEN** `AddFromInventory` or `MoveToInventory` is called while the current thread owns a transaction containing only the inventory boundary
- **THEN** it throws before validation, messaging, or storage mutation and does not create a nested transaction

### Requirement: ItemContainer storage stays encapsulated
Ordinary domain owners that compose `ItemContainer` MUST NOT recover or access its `ItemContainerStorage` instance. Persistence and specialized state maintenance MUST use narrow internal `ItemContainer` operations. Direct `ItemContainerStorage` ownership is reserved for domain containers whose invariants require bypassing the generic facade, currently `EquipmentContainer` and `MoneyPouchContainer`.

#### Scenario: Ordinary container persistence uses the facade
- **WHEN** an ordinary domain owner hydrates, dehydrates, or normalizes item state
- **THEN** it uses narrow `ItemContainer` operations and does not obtain raw storage

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST implement `IItemContainer` and the empty public `IItemTransactional` marker, privately composing one `ItemContainerStorage` and one `ItemContainerMutationBoundary`; its internal `IItemTransactionSource` implementation MUST contribute that boundary. Ordinary domain implementations privately own concrete `ItemContainer`; ordinary domain interfaces MUST expose `IItemContainer Items` and MUST NOT copy generic forwarding operations. Equipment and MoneyPouch MUST compose storage directly with private mutation boundaries and MUST NOT expose generic `Items` or raw storage boundaries. `IEquipmentContainer` MUST expose `IReadOnlyItemContainer Items` and MUST NOT expose generic mutation; its private `ItemContainerStorage` MAY implement `IReadOnlyItemContainer` directly without a forwarding wrapper. TradeOffer, Duel, and Price Checker MUST compose the concrete `ItemContainer` where generic behavior is required. `IItemContainer` MUST NOT reference concrete `ItemContainer`. Generic policy-heavy partial/bulk movement APIs such as `TransferAll`, `AddAndRemoveFrom`, or domain-specific transfer orchestration MUST NOT be added to `IItemContainer`. The exact atomic `TryTransferTo(...)` primitive is intentionally part of `IItemContainer`. Transaction membership MUST be explicit by passing all participating domain objects directly to `ItemContainerTransaction.Begin(...)`; `IItemContainer` and `IMoneyPouchContainer` MUST implement `IItemTransactional` and MUST NOT expose a public `Mutations` property. The internal `IItemTransactionSource` MUST resolve storage contributions. Ordinary mutations on enlisted storage MUST participate automatically and defer publication through the active transaction; ordinary mutations on unenlisted storage MUST remain standalone. Ordinary mutation methods MUST NOT create, commit, or dynamically enlist a transaction. Domain callers MUST NOT cast containers to recover storage infrastructure. `ItemContainerStorage` MUST own low-level slot and mutation mechanics and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `ItemContainerTransaction` MUST own deterministic lock ordering, snapshots, rollback, changed-slot tracking, and post-commit publication. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST remain available through the ordinary container API. MoneyPouch MUST perform pouch and inventory mutations through the same scope, with its internal source contributing both storage boundaries; it MUST NOT expose the concrete storage boundary or keep a separate rollback path. Concrete storage and mutation-boundary implementations MUST remain internal; Scripts MUST use public domain capabilities without friend-assembly access.

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
`IItemContainer.TryTransferTo(...)` MUST provide one atomic exact cross-container operation using the single low-level `ItemContainerStorage` transfer algorithm. When neither storage participates in a current-thread transaction, the method MUST acquire both mutation boundaries in deterministic lock order, mutate both storages once, and retain standalone publication ownership for both until publication completes; it MUST NOT create an `ItemContainerTransaction`. When both storages already belong to the same active current-thread transaction, the method MUST participate without creating, committing, or disposing that transaction. Partial or conflicting participation MUST be rejected before mutation. A valid transfer rejection MUST return false without mutating either storage. Success MUST advance each storage revision once. Standalone publication MUST occur after both locks are released in source-then-destination order. Every supplied before-publication action and both publishers MUST be attempted once even if an earlier stage throws. Failures MUST leave storage committed, MUST NOT be retried, and MUST release both ownership claims. A single failure MUST propagate as its original exception; multiple failures MUST be aggregated. `ItemContainerMutationBoundary.TryTransferTo(...)` MUST require both storages to already belong to the same active current-thread transaction.

#### Scenario: Standalone transfer uses direct two-boundary ownership
- **WHEN** neither storage participates in a current-thread transaction and the source has the requested quantity while the destination can accept the exact result
- **THEN** `TryTransferTo(...)` mutates both stores atomically without creating an `ItemContainerTransaction`, releases both physical locks, and publishes source before destination while retaining both logical ownership claims

#### Scenario: Standalone transfer publication failure leaves storage committed
- **WHEN** the source publisher throws after a successful standalone transfer
- **THEN** the destination publisher is still attempted once, both stores remain changed, both ownership claims are released, and the original publication exception propagates

#### Scenario: Standalone transfer attempts publishers after a before-publication failure
- **WHEN** a supplied before-publication action throws after a successful standalone transfer
- **THEN** source and destination publishers are both attempted once in order, ownership is released, and all failures propagate according to the single-or-multiple failure rule

#### Scenario: Standalone transfer aggregates independent publisher failures
- **WHEN** more than one before-publication action or publisher throws
- **THEN** all independent stages are attempted once and an `AggregateException` retains the failures

#### Scenario: Transfer participates in an existing transaction
- **WHEN** both storages belong to the same active current-thread transaction
- **THEN** `TryTransferTo(...)` stages the transfer without creating, committing, or disposing that transaction, and the caller controls publication or rollback

#### Scenario: Caller rollback restores a transfer
- **WHEN** a transfer succeeds inside an existing transaction that is disposed without commit
- **THEN** both stores are restored and neither container publishes a committed mutation

#### Scenario: Partial or conflicting enlistment rejects
- **WHEN** only one storage is enlisted or source and destination belong to different active transactions
- **THEN** `TryTransferTo(...)` throws before either store changes and does not create a nested transaction

#### Scenario: Transfer fails validation
- **WHEN** the source quantity is insufficient or the destination cannot accept the result
- **THEN** neither store changes and neither container publishes a committed mutation

#### Scenario: Internal transfer boundaries require one active transaction
- **WHEN** `ItemContainerMutationBoundary.TryTransferTo(...)` is called without a transaction shared by source and destination, or from a thread other than the transaction owner
- **THEN** it throws before either store changes

#### Scenario: Opposite transfers acquire locks consistently
- **WHEN** two operations transfer in opposite directions between the same stores
- **THEN** their owner scopes acquire store locks in the same stable order, and an independent scope waits until the current scope releases its locks

#### Scenario: Transfer is exposed by the container
- **WHEN** a caller inspects the public `IItemContainer` contract
- **THEN** it finds `TryTransferTo(...)` on the interface; transaction membership is declared by passing the aggregate object directly to `Begin(...)`

### Requirement: Storage and trade revisions have distinct purposes
Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerLockSet` MUST own deterministic mutation-boundary lock ordering; `ItemContainerTransaction` MUST use that lock order for scope acquisition and finalization. `ItemContainerMutationBoundary.TryTransferTo` MUST require an existing caller-owned scope containing both boundaries. `TradingCharacterScript` MUST own terminal completion, refund, and forced recovery scopes, establish them before TradeExchange stages mutations, and set the terminal `TradeState` before `Commit()`; TradeExchange MUST NOT begin or commit those scopes. Terminal TradeExchange movement MUST use exact `IItemContainer.TryTransferTo(...)` for offered non-coin items and selected escrow recovery destinations inside the existing caller-owned transaction, and `IMoneyPouchContainer.TryTransferCoinsFrom(...)` for offered coins. It MUST NOT use standalone-capable destination `AddRange` or terminal offer `Clear` for authoritative escrow movement. Trade session owners MUST NOT acquire mutation-boundary locks directly. A trade offer's acceptance `Revision` MUST remain domain-owned and MUST advance according to its existing publication semantics, independently of storage revision.

#### Scenario: Storage revisions are independent from deterministic lock ordering
- **WHEN** storage changes during a transaction involving multiple boundaries
- **THEN** storage advances its own mutation revision while `ItemContainerLockSet` orders synchronization by boundary lock order

### Requirement: Storage is synchronization-agnostic
`ItemContainerStorage` MUST own item state, mutation revision, mutation algorithms, snapshots, and restoration only. It MUST NOT own synchronization or transaction state. Its owning `ItemContainerMutationBoundary` provides synchronous authorization and protection. Transactions MUST coordinate boundaries and access storage only for the item-state data those boundaries protect.

#### Scenario: Storage mutation algorithms do not acquire synchronization
- **WHEN** an internal storage algorithm mutates item state
- **THEN** it performs item-state validation and mutation while its owning boundary provides synchronization and authorization

#### Scenario: Storage mutation invalidates enumeration
- **WHEN** storage changes after an enumerator is created
- **THEN** the enumerator detects the storage revision mismatch

#### Scenario: Empty bulk addition is not a mutation
- **GIVEN** an item container and an enumerator created before the operation
- **WHEN** `TryAddRange` receives no effective items, including an empty range or a range containing only null entries
- **THEN** the operation succeeds with no changed slots
- **AND** storage contents and mutation revision remain unchanged
- **AND** the existing enumerator remains valid
- **AND** no participant mutation publication occurs

### Requirement: Stacking checks do not orchestrate gameplay
`IItemScript.CanStackItem` calls made during storage mutation MUST make a deterministic decision from the supplied item data. They MUST NOT mutate unrelated containers, begin transactions, send gameplay events, or block on unrelated resources.

#### Scenario: A stacking check attempts contended storage access
- **WHEN** a stacking check tries to acquire another boundary while its caller owns storage
- **THEN** the additional operation fails without waiting, deadlocking, or partially changing either container

#### Scenario: Failed transaction restores storage revision
- **GIVEN** enumerators exist before a multi-container transaction begins
- **WHEN** the transaction stages one or more storage changes but later rolls back
- **THEN** every participant's slots, item identities, counts, and mutation revision are restored
- **AND** the pre-existing enumerators remain valid
- **AND** no participant publishes a committed mutation or runs a committed domain effect

#### Scenario: Trade publication invalidates acceptance
- **WHEN** a trade offer publishes a content update
- **THEN** its acceptance revision advances independently of the storage enumeration revision

### Requirement: Multi-container mutations use one explicit transaction
`ItemContainerTransaction` MUST provide one synchronous atomic scope around existing domain operations. Callers MUST supply every participant needed by a multi-storage operation before Begin; operations MUST NOT dynamically enlist storage. Domain operations retain item semantics, while the transaction owns atomicity and lifecycle. Ordinary container operations MUST participate automatically when their boundary is enlisted and remain standalone otherwise. A helper with missing or differently bound required storage MUST fail before mutation. Transaction misuse, including wrong-thread use, MUST fail before mutation. The public lifecycle is Begin, Commit, and Dispose; callers MUST NOT need a separate unit of work, publication call, or committed-state query. Async, savepoint, and distributed transaction support MUST NOT be introduced.

#### Scenario: Ordinary mutations use enlisted storage automatically
- **WHEN** a caller begins one transaction with a container participant and uses ordinary container mutation methods
- **THEN** those methods participate in the scope and use its locks without acquiring additional storage

#### Scenario: A helper requires missing storage
- **WHEN** a scope includes A but a composable operation requires A and B
- **THEN** the operation throws without acquiring or mutating B, and A remains bound to its existing scope

#### Scenario: Storage belongs to different scopes
- **WHEN** an operation requires storage bound to different active transactions
- **THEN** it throws without mutation or nested transaction creation

#### Scenario: A composite participant aliases storage
- **WHEN** ordinary and composite participants contribute the same storage
- **THEN** its owning boundary is locked and the storage state is snapshotted once while participant publication order remains first-seen order

#### Scenario: An unenlisted item container remains standalone
- **WHEN** a transaction includes A but not independent container B, and both containers are mutated
- **THEN** disposal restores A without publication while B retains its mutation and publishes normally

### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch MUST contribute exactly its pouch storage and its inventory item storage to a transaction. Any pouch or inventory mutations made by `Add` and `TryRemoveExact` MUST occur through the same transaction. When neither boundary is owned by a current-thread transaction, these operations own a transaction; they participate only when both boundaries belong to the same active transaction and throw before mutation on partial current-thread participation. A foreign overlap uses normal transaction contention. `TryTransferCoinsFrom(...)` MUST require its source, pouch, and inventory boundaries in one caller-owned transaction and MUST NOT create a transaction. Coin, slot-zero sentinel, and balance rules remain owned by MoneyPouch; it MUST NOT keep a separate rollback mechanism.

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory mutations are staged but a later participant rejects its operation
- **THEN** transaction rollback restores both stores and no pouch message or event is published

#### Scenario: MoneyPouch additions and exact removals own or participate in one scope
- **WHEN** `Add` or `TryRemoveExact` runs with neither pouch nor inventory boundary owned by a current-thread transaction
- **THEN** MoneyPouch owns a transaction over both boundaries
- **WHEN** both boundaries already belong to the same active current-thread transaction
- **THEN** the operation participates without committing that transaction
- **WHEN** only the inventory boundary is owned by a current-thread transaction
- **THEN** the operation throws before changing pouch or inventory storage

#### Scenario: Coin transfer requires a complete caller-owned scope
- **WHEN** `TryTransferCoinsFrom(...)` is called without one transaction containing its source, pouch, and inventory boundaries
- **THEN** it throws before mutation and creates no independent transaction

#### Scenario: MoneyPouch contributes its two storage boundaries
- **WHEN** a transaction begins with the MoneyPouch aggregate
- **THEN** exactly the pouch storage and its inventory item storage are enlisted

#### Scenario: An exact MoneyPouch operation waits for a foreign owner
- **WHEN** another thread owns either required boundary and the current thread calls `Add` or `TryRemoveExact`
- **THEN** the operation waits through normal Begin contention and proceeds after the foreign scope completes

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

### Requirement: Clearing empty item storage is a no-op
Clearing storage with no non-null items MUST leave its slots and mutation revision unchanged and MUST produce no container publication. A non-null zero-count item sentinel MUST count as occupied and follow the existing clear behavior.

#### Scenario: Empty storage clear does not change storage or publish
- **WHEN** an empty or zero-capacity item container is cleared, including inside a committed transaction
- **THEN** storage revision and contents remain unchanged and no container change is published

#### Scenario: Non-null zero-count sentinel is cleared
- **WHEN** storage containing a non-null zero-count sentinel is cleared
- **THEN** the sentinel is removed and the storage revision advances as for other nonempty storage

### Requirement: Domain-specific empty-count semantics remain explicit
Storage MUST support removing depleted slots or retaining the item at a configured reset count. Money pouch and shop containers MUST retain their own domain behavior when using a zero reset count.

#### Scenario: Money pouch retains its coin sentinel
- **WHEN** pouch coins reach zero or an empty persisted pouch is restored
- **THEN** slot zero contains coin item 995 with count zero

#### Scenario: Shop stock normalizes depleted entries
- **WHEN** shop normalization processes depleted non-original stock
- **THEN** storage retains the configured zero-count entry and the shop applies its existing normalization and sorting behavior

### Requirement: Partial removal respects the requested total
`Remove(...)` MUST remove no more than the requested item quantity across all matching stacks and return the actual quantity removed. Non-null zero-count sentinels MUST NOT count as available quantity. `TryRemoveExact(...)` and `TryTransferTo(...)` MUST remain all-or-nothing. A ground drop MUST create a ground item only after exact inventory removal succeeds, and its quantity MUST match the selected item quantity.

#### Scenario: Removal spans matching stacks
- **WHEN** matching stacks together contain more than the requested quantity
- **THEN** removal consumes only the requested total and leaves the remaining quantity intact

#### Scenario: Removal has insufficient positive quantity
- **WHEN** matching stacks contain less than the requested quantity, including zero-count sentinels
- **THEN** partial removal returns only the available positive quantity and removes no additional items

#### Scenario: Exact removal fails without changing storage
- **WHEN** the selected exact quantity is unavailable
- **THEN** exact removal returns false and leaves every matching stack unchanged

#### Scenario: Failed exact drop does not create a ground item
- **WHEN** the selected item leaves inventory before the drop's exact removal
- **THEN** the drop returns false and creates no ground item

### Requirement: No-op sorting does not mutate storage
Sorting an already compact container MUST preserve order and item references without advancing its revision or publishing. Sorting that moves items MUST advance revision and publish the changed container state once.

#### Scenario: Sort has no items to move
- **WHEN** every item already occupies the compact prefix
- **THEN** sorting leaves revision and publication unchanged

#### Scenario: Sort compacts sparse slots
- **WHEN** sorting moves an item across an empty slot
- **THEN** it preserves item order, advances revision once, and publishes the change

### Requirement: Shop normalization uses immutable original quantities
`ShopStockContainer` MUST retain original quantities keyed by item ID, aggregate duplicate entries with overflow validation, and plan normalization without mutating live item references. A successful change MUST apply as one coherent storage mutation, preserve identity for unchanged entries, and publish only when state changes. A failed plan MUST leave storage, item counts, and revision unchanged.

#### Scenario: Original stock moves or player stock extends storage
- **WHEN** original items move slots or player-added stock occupies an arbitrary slot
- **THEN** normalization uses item-ID baselines without indexing current slots into original-stock metadata

#### Scenario: Normalization changes only planned item instances
- **WHEN** normalization changes stock quantities
- **THEN** only changed items are replaced by planned copies and unchanged entries retain their original identity

#### Scenario: Normalization has no effect
- **WHEN** current stock already matches the normalization result
- **THEN** revision and publication remain unchanged

#### Scenario: Normalization planning fails
- **WHEN** cloning or planning fails before state replacement
- **THEN** live slots, item counts, references, and revision remain unchanged

### Requirement: Domain callbacks follow committed storage state
Equipment callbacks, trade settlement publication, inventory/bank/reward events, money-pouch messages, and UI updates MUST remain owned by their domain operations and MUST observe committed storage state.

#### Scenario: Equipment transfer runs callbacks before publication
- **WHEN** an equip or unequip transfer commits successfully
- **THEN** the required equipment callbacks run after both stores commit and before equipment and inventory updates publish

#### Scenario: Trade consumes generic container operations
- **WHEN** TradeExchange stages item mutations for settlement
- **THEN** it uses existing container operations under the session-owned scope's deterministically ordered boundary locks and publishes through domain owners only after commit

### Requirement: MoneyPouch and economic flows use one transaction owner
MoneyPouch additions, removals, inventory transfers, bank deposits, shop purchases and sales, and duel stake, return, and cancellation refunds MUST stage all affected storage through one `ItemContainerTransaction` or exact two-container mutation boundary. A failed operation MUST leave every participant unchanged. Existing amount clamping, prices, messages, callbacks, and stock normalization MUST remain owned by their domain operations. MoneyPouch MUST retain its slot-zero coin sentinel and partial `Remove`, `AddFromInventory`, and `MoveToInventory` behavior.

#### Scenario: Pouch overflow cannot partially commit
- **WHEN** a pouch addition overflows but inventory cannot accept the overflow
- **THEN** pouch and inventory remain unchanged and no committed pouch message or event is emitted

#### Scenario: Bank deposit from pouch is rejected
- **WHEN** bank storage cannot accept a staged deposit
- **THEN** bank and pouch remain unchanged

#### Scenario: Shop payment or delivery fails
- **WHEN** a shop purchase or sale cannot stage every payment, item, or payout change
- **THEN** inventory, pouch, and shop storage remain unchanged and no purchase event is emitted

#### Scenario: Duel cancellation refund fails
- **WHEN** either player's stake cannot be returned
- **THEN** both stake containers and both players' destination stores remain unchanged and the duel session remains active

### Requirement: Duel stake mutations use a composed domain collaborator
`DuelArenaScript` MUST compose one concrete `DuelStakeExchange` for item and pouch staking, return, and cancellation refund. The collaborator MUST use exact mutation-boundary transfers for simple items and one `ItemContainerTransaction` for pouch or two-player refund operations. It MUST NOT own UI or session state or require an interface or service registration.

#### Scenario: Duel stake transfer fails
- **WHEN** the stake or destination cannot accept the exact transfer
- **THEN** source and destination remain unchanged and the script does not publish a stake change

### Requirement: Decision-critical reads are revalidated under ownership
A preflight read that determines whether an item mutation is valid MUST be checked again after all required mutation boundaries are acquired. Informational reads MAY remain lightweight, and the system MUST NOT introduce a global read lock.

#### Scenario: Source changes after preflight
- **WHEN** the selected source instance or quantity changes between preflight and mutation ownership
- **THEN** the operation rejects without consuming a different or stale item

#### Scenario: Destination changes after preflight
- **WHEN** capacity or the target slot changes before mutation ownership is acquired
- **THEN** the operation rechecks the destination and either performs the complete operation or leaves every participant unchanged

### Requirement: Equipment replacement is authorized and atomic
Before mutation, `EquipmentContainer` MUST require `CanEquipItem` and `CanUnEquipItem` to succeed for the relevant items. Rejection MUST leave equipment and inventory unchanged and invoke no lifecycle callback. For an active aura replacement, `AuraEquipmentScript.EquipItem` MUST prompt before delegating to `EquipmentScript.EquipItem`; it MUST NOT hold storage locks or a transaction while waiting. Acceptance MUST continue only if the same aura instance remains equipped. The resulting `EquipmentContainer` operation MUST revalidate the exact incoming inventory instance, equipped instance, and target slot under one transaction. Repeated or concurrent delivery MUST NOT duplicate an item. The transaction MUST remove the exact incoming instance, place the displaced item in the vacated inventory slot, and equip the incoming instance. A rejected storage step MUST roll back both containers without callbacks or publication. This path MUST NOT invoke the old `UnEquipItem` command or recover items through Rewards or Bank.

#### Scenario: A later conflict rejects replacement
- **WHEN** an earlier conflicting item allows unequipping but a later conflict rejects it
- **THEN** equipment and inventory remain unchanged and no mutation publication or callback occurs

#### Scenario: An occupied slot swaps atomically
- **WHEN** the incoming item is equipped over an item in a non-weapon/non-shield slot
- **THEN** the exact incoming item replaces it in one transaction and both lifecycle callbacks run after commit and before publication

#### Scenario: Active aura confirmation leaves storage unchanged until accepted
- **WHEN** an active aura's `EquipItem` requests UI confirmation before replacement
- **THEN** inventory and equipment remain unchanged while the dialogue is open, and acceptance delegates to the normal atomic Equipment operation

#### Scenario: A stale aura confirmation does not replace another item
- **WHEN** the equipped aura changes before confirmation is accepted
- **THEN** the continuation does nothing and does not start an Equipment operation

#### Scenario: Repeated equip requests do not duplicate items
- **WHEN** the same incoming item is submitted for equipment more than once, sequentially or concurrently
- **THEN** the exact incoming item is moved at most once and later requests leave storage and publication unchanged

#### Scenario: A failed replacement stage restores both containers
- **WHEN** a storage operation fails after the incoming item has been removed inside the transaction
- **THEN** rollback restores both original item instances and no lifecycle callback or publication occurs

#### Scenario: Weapon and shield replacement rejects stale captured instances
- **WHEN** the incoming inventory item, captured weapon, or captured shield changes during preflight
- **THEN** the transaction rejects before mutation, leaves the current item instances in place, and performs no additional lifecycle callback, publication, or inventory-full message

#### Scenario: Two-handed replacement validates both captured equipment slots
- **WHEN** a two-handed weapon replacement captured both weapon and shield and either slot changes before transaction ownership
- **THEN** it rejects rather than recomputing which items to displace

### Requirement: Unequip validates the expected instance and destination under ownership
`UnEquipItem` MUST check that the exact item instance captured before `CanUnEquipItem` still occupies its equipment slot after it acquires inventory and equipment ownership. It MUST choose an available preferred inventory slot under that same ownership and use normal destination selection if the preferred slot is unavailable.

#### Scenario: Unequip rejects an item replaced during preflight
- **WHEN** the equipped item instance changes during `CanUnEquipItem`
- **THEN** the command returns false without transferring the replacement, running an additional lifecycle callback, publishing the rejected operation, or reporting inventory full

#### Scenario: Unequip rechecks its preferred inventory slot
- **WHEN** a requested in-range inventory slot is empty at request time
- **THEN** it uses that slot if still empty after acquiring ownership, and otherwise uses normal destination selection

### Requirement: Weapon and shield replacement commits conflicting items atomically
After all required `CanUnEquipItem` checks succeed, Weapon and Shield replacement MUST acquire one `ItemContainerTransaction` and verify that the captured incoming item still occupies its in-range inventory slot, the captured weapon and shield instances still occupy their respective slots (including captured null values), the incoming count is positive, and its equipment slot is unchanged. It MUST reject a stale capture before mutation without treating it as an inventory-capacity failure. If validation succeeds, it MUST remove the incoming inventory item, move each conflicting equipped item into inventory, and place the same incoming item instance in its equipment slot within that transaction. If any staged storage operation fails, inventory and equipment MUST be restored unchanged and no lifecycle callback or mutation publication may occur. A successful replacement MUST NOT invoke conflicting items' `UnEquipItem` commands. After commit and unlock, it MUST run `OnUnequipped` for a conflicting weapon, then a conflicting shield, then weapon profile/special-attack logic when a weapon is removed, then the incoming item's `OnEquipped`, all before participant publication.

#### Scenario: A second conflicting item cannot fit
- **WHEN** Weapon and Shield are both displaced but inventory can accept only one of them
- **THEN** the incoming item and both equipped items remain in their original slots and no lifecycle callback or mutation publication occurs

#### Scenario: A weapon and shield are displaced successfully
- **WHEN** both conflicting items fit after the incoming item leaves inventory
- **THEN** both original item instances are in inventory, the same incoming instance occupies its equipment slot, and lifecycle callbacks run in the specified order after commit and before publication

#### Scenario: A stale weapon or shield capture is rejected
- **WHEN** a captured conflicting weapon or shield instance changes during preflight
- **THEN** replacement leaves the incoming item and current equipment instances unchanged and performs no additional callback or publication

#### Scenario: Empty weapon and shield slots are revalidated after ownership
- **WHEN** both slots are captured empty before the equipment transaction begins and either slot becomes occupied while the operation waits to acquire ownership
- **THEN** the operation returns false without changing inventory or equipment, running lifecycle effects, or publishing changes

#### Scenario: A participant publisher throws after replacement commit
- **WHEN** replacement storage commits and a participant publisher throws
- **THEN** committed storage remains in place, all required lifecycle effects have been attempted, every independent changed-container publisher is attempted once in the existing order, and the original exception or aggregated failures propagate after completion

### Requirement: Transaction construction and rollback preserve storage state
`ItemContainerTransaction` MUST expose Begin, Commit, and Dispose as one synchronous atomic lifecycle. Begin MUST resolve and validate participant contributions in encounter order before locking, deduplicate repeated boundary contributions without changing resolved publication order, and reject one storage exposed through different boundaries. It MUST acquire boundary locks in deterministic order, capture snapshots, and establish bindings only after construction succeeds. A construction failure MUST release acquired locks in reverse order without changing storage or publishing. Mutations affect live storage while the scope is active. Dispose without Commit MUST attempt every snapshot restoration while retaining the transaction bindings and locks, discard pending completion facts, then clear bindings and release locks without publishing or pulsing waiters. Snapshots restore slots, item references, counts, and storage revision, but do not deep-copy mutable `IItem` state such as `ExtraData`.

Commit MUST make the current storage state irreversible before discarding snapshots. It MUST release every mutation lock before invoking domain hooks or publishers, while retaining boundary bindings until completion and publication finish. Once the scope is fully completed, bindings MUST be cleared under ordered boundary locks and waiters awakened. An overlapping Begin from a thread that owns no other item-container storage waits until then. A thread already owning storage MUST acquire additional boundaries without waiting and throw if an additional lock is contended or logically foreign-owned. Same-thread overlapping Begin and ordinary mutation against a committed binding are rejected. Dispose after commit is inert, and another Commit is invalid.

#### Scenario: Construction fails during snapshot capture
- **WHEN** snapshot capture throws after locks have been acquired
- **THEN** all acquired locks are released, no binding survives, and storage and publication remain unchanged

#### Scenario: Scope exits without commit
- **WHEN** an early return, exception, or cancellation exits an active scope
- **THEN** every participant is restored while its lock and binding are held, pending completion is discarded, and no effect is published

#### Scenario: Commit crosses the irreversible point before callbacks
- **WHEN** all mutations succeed and Commit begins completion
- **THEN** storage is irreversible and all mutation locks are released before hooks or publication run

#### Scenario: Independent overlapping scopes wait
- **WHEN** another thread owns a required boundary and the requesting thread owns no other item-container storage
- **THEN** Begin waits until the foreign scope completes, then acquires locks in deterministic order

#### Scenario: A storage-owning thread rejects a contended additional transaction
- **WHEN** a thread already owns an item-container transaction or standalone publication and begins another scope that needs a contended or foreign-owned boundary
- **THEN** Begin throws without waiting and releases any additional lock prefix

#### Scenario: Nested disjoint transaction remains available
- **WHEN** a thread already owns one scope and begins a transaction over disjoint, immediately available boundaries
- **THEN** the nested scope acquires them without waiting and can complete independently

#### Scenario: Nested Begin on an enlisted boundary is rejected
- **WHEN** the current thread begins a transaction for storage already enlisted in its active transaction
- **THEN** Begin throws and leaves the existing scope unchanged

### Requirement: Commit completes domain effects and publication in order
Commit MUST run pre-publication completion, changed container publication, then post-publication completion. Completion owners run once per owner-bearing resolved boundary in participant encounter and contribution order before lock sorting; each owner processes its facts in FIFO order, with no cross-owner fact-interleaving guarantee. Container publication uses its existing observable order independently of lock order. Every independent completion owner and changed boundary in each phase MUST be attempted once even if an earlier operation throws. A failure within one owner's own fact processing MAY stop that owner's remaining facts, but MUST NOT prevent later owners from being attempted. Pending facts are discarded before bindings are released.

An exception after the irreversible point leaves storage committed and is never retried. A single failure propagates as its original exception; multiple failures propagate as a flattened AggregateException in encounter order. Validation needed to determine whether a mutation is allowed MUST happen before Commit. The transaction MUST NOT store executable domain callbacks or expose callback registration.

#### Scenario: Container publication fails
- **WHEN** a container publisher throws
- **THEN** later changed containers and every post-publication completion owner are attempted once, storage remains committed, and the original exception propagates after cleanup

#### Scenario: Post-publication completion fails
- **WHEN** one completion owner throws after container publication succeeds
- **THEN** later independent owners are attempted once, the failed owner's remaining facts are discarded, and storage remains committed

#### Scenario: Pre-publication completion failure does not suppress later phases
- **WHEN** one pre-publication completion owner throws
- **THEN** later owners, changed container publishers, and post-publication completion owners are attempted once

#### Scenario: Multiple committed stages fail
- **WHEN** more than one independent hook, publisher, or completion owner throws
- **THEN** each independent stage is attempted once and a flattened AggregateException retains all original failures in encounter order

#### Scenario: Equipment hooks and publication both fail
- **WHEN** an Equipment hook batch and normal publication both throw
- **THEN** all required Equipment hooks are attempted and an AggregateException retains both original failures

#### Scenario: Rollback discards owner completion
- **WHEN** Equipment and pouch mutations record pending effects but the scope exits without Commit
- **THEN** storage is restored, pending facts are discarded, and a later unrelated scope runs none of those effects

#### Scenario: Completion owners follow resolved boundary order
- **WHEN** participants are supplied in an order different from lock acquisition and mutation order
- **THEN** owner-bearing resolved boundaries complete in participant encounter and contribution order

#### Scenario: One owner preserves its pending fact order
- **WHEN** one completion owner records multiple facts in a transaction
- **THEN** that owner completes its facts in the order recorded

#### Scenario: Completion starts a disjoint scope
- **WHEN** post-unlock completion synchronously starts a scope over storage not owned by the committing transaction
- **THEN** the disjoint scope proceeds normally

#### Scenario: Completion cannot re-enter an overlapping scope
- **WHEN** committed completion attempts same-thread Begin or mutation against storage still bound to the committing transaction
- **THEN** it throws `InvalidOperationException` without altering the committed scope

### Requirement: Mutation authorization and standalone publication belong to the boundary
Each mutation boundary MUST authorize synchronous operations against its storage. A per-operation scope MUST acquire the boundary lock for standalone work or borrow the lock held by its active transaction, and release only a lock it acquired. A manually held lock without a transaction binding MUST NOT authorize mutation. Change attribution MUST occur under the same lock and transaction binding, and MUST reject a boundary outside the active transaction or an unlocked boundary. A scope with no changes MUST publish nothing. Transaction membership is determined from the boundary binding; ambient current-transaction state is prohibited.

When a standalone mutation records changes, the boundary MUST claim publication ownership before releasing its lock. It retains that ownership through lifecycle effects and publication while callbacks run without the lock. Same-thread overlapping use MUST be rejected. When the current thread owns no item-container scope, ordinary mutations and overlapping Begin operations MUST wait for foreign transaction or publication ownership to clear. When the current thread already owns any item-container transaction or standalone publication, additional boundary acquisition MUST be nonblocking: it MAY proceed when the lock and logical ownership are immediately available, but MUST throw rather than wait when either is contended or foreign-owned. A multi-boundary Begin MUST preserve deterministic lock ordering and release any acquired lock prefix before waiting or failing. Standalone cleanup MUST clear ownership and pulse waiters under the boundary lock, including after publisher failure.

#### Scenario: Standalone mutation publishes after unlocking
- **WHEN** an ordinary mutation succeeds on unbound storage
- **THEN** it acquires the boundary lock once, records changes and claims publication ownership under that lock, releases the lock, and publishes before an overlapping foreign transaction can bind

#### Scenario: Transaction-owned mutation borrows its boundary lock
- **WHEN** an ordinary mutation targets a boundary enlisted in the current thread's active transaction
- **THEN** it neither reacquires nor releases the transaction-owned lock and attributes changes before returning

#### Scenario: A manually held lock does not authorize mutation
- **WHEN** the current thread holds a boundary lock without a transaction binding and starts an ordinary mutation
- **THEN** the operation throws without borrowing or releasing the external lock

#### Scenario: Change attribution requires the enlisted boundary and lock
- **WHEN** changes are attributed for a boundary outside the active transaction or without its mutation lock
- **THEN** attribution throws without adding change data

#### Scenario: An unsuccessful standalone mutation publishes nothing
- **WHEN** an ordinary operation returns without recording changes
- **THEN** its lock is released without publication ownership or notification

#### Scenario: Ordinary mutation cannot change state during standalone publication
- **WHEN** a standalone publisher runs while logical ownership remains on its boundary and same-thread or foreign ordinary mutation starts
- **THEN** same-thread mutation throws before changing storage, while foreign mutation waits when it owns no other storage and proceeds after standalone ownership is released

#### Scenario: Foreign ordinary mutation waits through standalone publication
- **WHEN** a standalone publisher runs while logical ownership remains on its boundary and a foreign thread with no other storage ownership starts an ordinary mutation
- **THEN** the mutation waits without changing storage, then proceeds and publishes after standalone ownership is released

#### Scenario: Foreign ordinary mutation waits through transaction publication
- **WHEN** a committed transaction publisher runs while its boundary remains bound and a foreign thread with no other storage ownership starts an ordinary mutation
- **THEN** the mutation waits without changing storage, then proceeds and publishes after the transaction binding is released

#### Scenario: A storage-owning thread rejects a contended mutation boundary
- **WHEN** a thread already owns an item-container scope and starts an ordinary mutation on another boundary whose lock is unavailable or whose logical owner is foreign
- **THEN** the mutation throws without waiting or changing that boundary

#### Scenario: A storage-owning thread can mutate a disjoint boundary
- **WHEN** a thread already owns one item-container scope and mutates another boundary that is immediately available
- **THEN** the mutation proceeds without waiting and retains normal publication behavior

#### Scenario: Waiting mutation proceeds after publisher failure
- **WHEN** a standalone publisher throws while a foreign ordinary mutation with no other storage ownership waits on its boundary
- **THEN** standalone ownership is cleared and waiters are pulsed before the original publication exception propagates, allowing the waiting mutation to proceed

#### Scenario: Equipment lifecycle retains standalone ownership
- **WHEN** standalone Equipment lifecycle effects run after unlock and before publication
- **THEN** an overlapping foreign transaction waits through both phases and same-thread reentrant Begin is rejected

#### Scenario: Standalone publication cleanup releases ownership after failure
- **WHEN** a standalone publisher throws after ownership is claimed
- **THEN** cleanup clears ownership and pulses waiters under the boundary lock before propagating the publication exception

#### Scenario: Mutation participation uses no ambient scope
- **WHEN** an ordinary item-container mutation runs
- **THEN** participation is determined from its boundary binding without ambient state or transaction identity passed through the operation

### Requirement: Automatic economic publication preserves domain behavior
MoneyPouch MUST internally record immutable notification facts containing the previous count, new count, and domain-visible change amount for automatic publication after container publishers. Callers MUST NOT manage notification receipts. Messages MUST use the captured change amount, then events MUST use the captured previous and new counts; publication MUST NOT reconstruct events from live pouch state. An inventory-only pouch addition MUST NOT publish a pouch effect. Equipment MUST own its ordered lifecycle/profile hook batch, retain attempt-all behavior after storage is irreversible and locks are released, and execute typed completion facts directly. For standalone equipment changes, lifecycle effects and normal publication MUST be attempted explicitly before retained failures are surfaced. Shop purchases MUST sort stock and send the purchase event only after Commit finishes publication.

#### Scenario: Multiple pouch mutations retain their own facts
- **WHEN** a transaction changes pouch count from 10 to 13 and then from 13 to 12
- **THEN** the first publication sends the +3 message and event 10 to 13, and the second sends the -1 message and event 13 to 12

#### Scenario: Equipment lifecycle fails after commit
- **WHEN** an equipment lifecycle effect throws after storage commits
- **THEN** later required lifecycle effects and transaction publication are still attempted under the equipment owner's existing cleanup policy and storage is not rolled back

#### Scenario: Shop publication fails after purchase commit
- **WHEN** container or pouch publication throws after a purchase commits
- **THEN** later stock sorting and the purchase event are skipped, storage remains committed, and the original publication exception propagates directly

### Requirement: Read-only item access is an explicit capability
`IReadOnlyItemContainer` MUST expose indexed/enumerable item reads and item-specific queries without mutation or transaction participation. `IItemContainer` MUST inherit this capability and retain mutation members. `IEquipmentContainer` MUST implement the public `IItemTransactional` marker, compose an `IReadOnlyItemContainer Items` view, and retain its equipment-slot indexer and semantic operations.

#### Scenario: Equipment exposes a read-only view and transaction participation
- **WHEN** a caller accesses equipment through `IEquipmentContainer`
- **THEN** generic item reads and queries are available through `Items`, equipment-slot reads remain available through the `EquipmentSlot` indexer, generic mutation is unavailable, and the equipment domain object can participate in an item-container transaction

#### Scenario: Equipment read view reflects its storage
- **WHEN** equipment contains an item
- **THEN** the read-only view reports the same capacity, slots, enumeration, ID lookup, counts, and containment as the equipment storage

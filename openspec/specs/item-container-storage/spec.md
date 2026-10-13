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
Ordinary domain owners that compose `ItemContainer` MUST NOT recover or access its `ItemContainerStorage` instance. Persistence and specialized state maintenance MUST use narrow internal `ItemContainer` operations. Direct `ItemContainerStorage` ownership is reserved for domain containers whose invariants require bypassing the generic facade, currently `EquipmentContainer` and `MoneyPouchContainer`. Storage, mutation boundaries, and transaction-source mechanics MUST remain internal. Ordinary domain interfaces MUST expose read-only item views and domain mutation operations rather than duplicating generic mutation APIs; Equipment and MoneyPouch MUST keep their storage composition private.

#### Scenario: Ordinary container persistence uses the facade
- **WHEN** an ordinary domain owner hydrates, dehydrates, or normalizes item state
- **THEN** it uses narrow `ItemContainer` operations and does not obtain raw storage

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision

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

### Requirement: Domain-owned containers control mutation publication
Ordinary domain containers MUST expose item reads through `IReadOnlyItemContainer`; Equipment MUST expose a read-only item view and transaction participation without generic mutation. Writes MUST use the owning domain's existing or narrowly required mutation commands. A standalone command MUST publish its committed tracker batch after releasing physical locks and before releasing its logical transaction binding. A caller-owned transaction MUST publish each affected domain through that owner after Commit and before disposing the transaction. Raw `ItemContainer` remains directly mutable and publishes no domain events; its owner consumes `ChangeTracker` explicitly.

#### Scenario: Domain item views cannot bypass their owner
- **WHEN** a consumer accesses Inventory, Bank, Reward, Familiar Inventory, or Shop Stock items through its public interface
- **THEN** it can query the items but cannot invoke generic storage mutations

#### Scenario: Equipment exposes a read-only item view
- **WHEN** a caller accesses equipment through `IEquipmentContainer`
- **THEN** it can query its items and participate in a transaction, but cannot invoke generic storage mutations through the item view

#### Scenario: Standalone domain command publishes committed changes
- **WHEN** a domain mutation succeeds without an enclosing transaction
- **THEN** its owner publishes the committed changed slots once after the storage mutation lock is released

#### Scenario: Multi-owner transaction publishes before releasing ownership
- **WHEN** a caller commits mutations to multiple domain containers
- **THEN** it publishes each affected owner in the operation's established order before disposing the scope, and foreign mutations wait until disposal

#### Scenario: Rollback does not publish domain changes
- **WHEN** a domain mutation participates in a transaction that is disposed without Commit
- **THEN** storage and tracker state are restored or discarded without a domain notification

### Requirement: Cross-container transfer records both sides without publishing
`IItemContainer.TryTransferTo(...)` MUST use one atomic storage transfer under deterministic boundary locking. It MUST participate in a shared active transaction when both boundaries are enlisted and otherwise perform an atomic standalone transfer without creating a transaction. Success MUST record committed changes for both trackers once; rejection MUST leave both stores and trackers unchanged. The generic operation MUST NOT invoke publishers or external callbacks. Domain owners MUST coordinate publication for their own storage after a surrounding transaction commits.

#### Scenario: Standalone transfer records source and destination batches
- **WHEN** an exact transfer succeeds between raw item containers outside a transaction
- **THEN** both storages change atomically and each tracker records its own changed slots without invoking external code

#### Scenario: Transactional transfer rolls back without publication
- **WHEN** an exact transfer succeeds in a shared transaction that is disposed without Commit
- **THEN** both storages and provisional tracker changes are restored

#### Scenario: Failed transfer leaves both trackers unchanged
- **WHEN** the source lacks the requested amount or the destination cannot accept it
- **THEN** the operation returns false without changing either storage or tracker

### Requirement: Transaction construction and commit preserve storage state
`ItemContainerTransaction` MUST resolve and validate all participants before locking, deduplicate repeated boundaries without changing participant encounter order, acquire boundaries in deterministic lock order, capture snapshots, and establish bindings only after construction succeeds. Construction failure MUST release acquired locks without changing storage or publishing. Mutations affect live storage while the scope is active. Dispose without Commit MUST restore snapshots and discard provisional tracker changes. Commit MUST make storage irreversible, promote tracker changes, release physical locks, and retain transaction bindings until Dispose so owners can publish without an intervening mutation. Publication failures MUST NOT roll back committed storage or replay consumed changes. Snapshots restore slot topology, item references, counts, and storage revision, but do not deep-copy mutable item metadata such as `ExtraData`.

#### Scenario: Construction fails during snapshot capture
- **WHEN** snapshot capture throws after locks have been acquired
- **THEN** all acquired locks are released, no binding survives, and storage and publication remain unchanged

#### Scenario: Scope exits without commit
- **WHEN** an early return, exception, or cancellation exits an active scope
- **THEN** every participant is restored while its lock and binding are held, provisional changes are discarded, and no domain effect is published

#### Scenario: Commit makes storage irreversible before owner publication
- **WHEN** all mutations succeed and Commit returns
- **THEN** tracker changes are committed, physical locks are released, and owner publication can consume changes while the transaction binding remains until Dispose

#### Scenario: Foreign operations wait through committed owner publication
- **WHEN** an owner publishes changes after Commit but before transaction disposal
- **THEN** foreign mutations and tracker reads wait until the scope releases its bindings

### Requirement: Mutation boundaries own synchronization without publishing
Mutation boundaries MUST authorize synchronous mutations, transaction participation, deterministic locking, and tracker access. They MUST NOT store or invoke generic publication delegates. Standalone raw-container operations MUST record committed changes directly in the tracker; transaction mutations MUST remain provisional until Commit. Tracker reads MUST obey boundary ownership and MUST NOT expose provisional changes.

#### Scenario: Raw container change batch is consumed once
- **WHEN** a raw container mutation commits and its owner calls `ChangeTracker.GetSlots()`
- **THEN** the returned batch contains the changes and a later retrieval is empty unless another mutation commits

#### Scenario: Transaction owner consumes after commit only
- **WHEN** the owning thread calls `GetSlots()` during an active transaction
- **THEN** the call is rejected until Commit; after Commit it can consume the committed batch before Dispose

#### Scenario: Foreign tracker reader waits through owner publication
- **WHEN** a committed transaction retains a domain binding while the owner publishes
- **THEN** a foreign tracker reader waits until the scope is disposed and then sees only unconsumed committed changes

### Requirement: Equipment and MoneyPouch own specialized publication
Equipment MUST run its lifecycle effects before consuming and publishing committed Equipment changes. MoneyPouch MUST retain FIFO immutable change facts and publish its messages/events after other domain containers affected by the same operation. Both owners MUST discard pending facts on rollback. Post-commit failures MUST leave storage irreversible and consume each attempted batch once.

#### Scenario: Equipment effects precede its event
- **WHEN** a committed Equipment mutation has lifecycle effects
- **THEN** all required effects are attempted before the Equipment event is sent

#### Scenario: MoneyPouch publishes captured facts after container events
- **WHEN** a committed operation changes both a pouch and an ordinary item container
- **THEN** the ordinary container event precedes the MoneyPouch message/event, which uses its captured counts and change amount

#### Scenario: Equipment hook failure does not roll back storage
- **WHEN** an Equipment lifecycle hook throws after Commit
- **THEN** required hooks and the Equipment event follow Equipment's attempt-all policy, storage remains committed, and the failure propagates

### Requirement: Committed item-container changes are consumed by domain owners
Each mutation boundary MUST own one change tracker. Concrete `ItemContainer.ChangeTracker` MUST expose that same tracker. The tracker MUST expose committed changes only, report whether unconsumed changes exist, atomically consume each retrieved batch, and preserve full-container changes separately from empty batches. A full-container change is represented by `null`; no changes are represented by an empty `HashSet<int>`. Domain owners MUST consume committed batches explicitly; generic storage MUST NOT publish domain events.

#### Scenario: Standalone changes are accumulated and consumed
- **WHEN** standalone mutations change one or more slots and the owner retrieves the tracker batch
- **THEN** the tracker returns the union of changed slots, and a later retrieval returns an empty set unless another committed mutation occurred

#### Scenario: Full-container change dominates slot changes
- **WHEN** a full-container change is recorded with partial changes before or after it
- **THEN** the next retrieval returns `null` for the full container and subsequent retrieval returns an empty set

#### Scenario: Transaction changes are committed or discarded
- **WHEN** a transaction mutates a boundary and commits or rolls back
- **THEN** commit makes its changes consumable while rollback discards only provisional changes and preserves earlier unconsumed committed changes

#### Scenario: Explicitly suppressed mutation is not tracked
- **WHEN** an operation is invoked with `publishChanges: false`
- **THEN** it changes storage without adding changed slots to a later tracker batch

#### Scenario: Domain publication consumes tracked changes once
- **WHEN** a domain owner publishes a committed batch successfully or its publisher throws
- **THEN** the batch is consumed once and a later retrieval does not replay it
### Requirement: PriceChecker selection updates use tracked slots
PriceChecker MUST send a full selection on open, then consume tracked selection changes to refresh only affected slots and their price configurations. Projected inventory MUST remain a separate full view.

#### Scenario: Incremental selection refresh updates changed slots
- **WHEN** a selection mutation commits after the interface opens
- **THEN** component 90 and per-slot price configurations contain only the changed slots while the total price is refreshed

#### Scenario: Inventory reconciliation refreshes selection once
- **WHEN** an inventory change reduces one or more selected stacks
- **THEN** reconciliation records those slot changes and sends one incremental selection refresh after the loop

#### Scenario: Inventory changes without selection changes
- **WHEN** inventory changes but reconciliation does not alter the selection
- **THEN** PriceChecker does not send a component 90 update

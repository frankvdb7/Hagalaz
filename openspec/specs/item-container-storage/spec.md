# Item Container Storage

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## Requirements

### Requirement: Equipment exposes semantic mutation operations
`IEquipmentContainer` MUST expose domain-specific mutation operations rather than generic `Add`, `Replace`, `Remove`, and `Clear` commands. Direct restoration, in-place transformation, removal, and clearing MUST use explicitly named equipment operations. Equipment replacement MUST require the caller's expected current item instance so a stale command cannot replace a different equipped item. Simple Equipment storage operations MUST participate when Equipment storage is enlisted; typed lifecycle completion MUST be deferred through the owning transaction. When unbound, lifecycle completion and publication occur immediately. Explicit transaction-owning Equipment workflows may use the private mutation boundary and typed deferred completion facts.

#### Scenario: Stale equipment replacement is rejected
- **WHEN** the expected item instance no longer occupies the requested slot
- **THEN** replacement returns false without changing storage, publishing an equipment update, or running lifecycle callbacks

#### Scenario: Equipment storage mutation participates in an active scope
- **WHEN** restoration, replacement, removal, or clearing is called while Equipment storage belongs to an active transaction
- **THEN** the storage mutation participates in that transaction, with lifecycle completion and publication deferred until commit

### Requirement: Equipment publishes only after lifecycle effects
Direct equipment restoration, replacement, full removal, and clearing MUST publish committed equipment state only after all required equipment lifecycle effects have been attempted. Storage MUST commit before lifecycle callbacks run. Clearing MUST remove all equipped items before callbacks and attempt `OnUnequipped` for every previously equipped item. The interactive occupied non-weapon/non-shield replacement path MUST verify that both inventory and Equipment are available for standalone operation before removing the incoming item or invoking its custom unequip command. Post-commit lifecycle or publication failures MUST NOT roll back committed storage. Every post-commit action MUST be attempted; one failure MUST preserve and rethrow the original exception, while multiple failures MUST be aggregated.

#### Scenario: Equipment replacement callbacks precede publication
- **WHEN** an expected equipped item is replaced
- **THEN** storage contains the replacement before `OnUnequipped` and `OnEquipped`, and publication follows both callback attempts

#### Scenario: Interactive non-weapon equipment replacement callbacks precede publication
- **WHEN** an incoming item replaces an occupied non-weapon/non-shield equipment slot through the standalone custom unequip path
- **THEN** the incoming `OnEquipped` callback runs after storage mutation and lock release but before Equipment publication

#### Scenario: Interactive equipment replacement rejects existing ownership before mutation
- **WHEN** inventory or Equipment already belongs to a transaction or standalone publication scope before interactive non-weapon replacement
- **THEN** the operation throws before removing the incoming inventory item or invoking the custom unequip command

#### Scenario: Full equipment removal callback precedes publication
- **WHEN** an equipped item is fully removed
- **THEN** storage no longer contains it before `OnUnequipped`, and publication follows the callback attempt

#### Scenario: Equipment clear exhausts callbacks after storage clear
- **WHEN** equipment containing one or more items is cleared
- **THEN** storage is empty before callbacks, every prior item's `OnUnequipped` is attempted, and publication follows all callback attempts

### Requirement: MoneyPouch separates gameplay operations from transaction participation
`IMoneyPouchContainer` MUST expose normal coin-domain operations and implement the empty public `IItemTransactional` capability marker. It MUST NOT expose a `Mutations` property or storage boundary. Its concrete domain implementation MUST provide exactly its pouch storage and one inventory item-storage boundary through the internal `IItemTransactionSource`. `TryAddExact` and `TryRemoveExact` MUST own a transaction when the current thread owns neither required boundary, participate in the same active current-thread transaction when it owns both, and throw before mutation when the inventory boundary is already owned by a current-thread transaction that does not include the pouch boundary. A foreign transaction MUST be handled by the standalone transaction's normal contention behavior. The primary MoneyPouch API MUST NOT expose generic item-ID `Contains`, a domain-specific mutation boundary, or caller-managed publication receipts.

#### Scenario: MoneyPouch coin availability is domain-specific
- **WHEN** a caller checks whether a character has a positive coin amount
- **THEN** `HasCoins` compares the request against pouch coins and inventory coin item `995` using overflow-safe addition

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
`IItemContainer.TryTransferTo(...)` MUST provide one atomic exact cross-container operation using the single low-level `ItemContainerStorage` transfer algorithm. When neither storage participates in a current-thread transaction, the method MUST own a short `ItemContainerTransaction` over source and destination and commit it only after the exact transfer succeeds. When both storages already belong to the same active current-thread transaction, the method MUST participate without creating, committing, or disposing that transaction. Partial or conflicting participation MUST be rejected before mutation. A valid transfer rejection MUST return false without mutating either storage. Success MUST advance each storage revision once and publish changed slots only after commit. `ItemContainerMutationBoundary.TryTransferTo(...)` MUST require both storages to already belong to the same active current-thread transaction.

#### Scenario: Standalone transfer owns a short transaction
- **WHEN** neither storage participates in a current-thread transaction and the source has the requested quantity while the destination can accept the exact result
- **THEN** `TryTransferTo(...)` owns and commits a short transaction over both stores before either container publishes an update

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
Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerTransaction` MUST own deterministic mutation-boundary lock ordering; `ItemContainerMutationBoundary.TryTransferTo` MUST require an existing caller-owned scope containing both boundaries. `TradingCharacterScript` MUST own terminal completion, refund, and forced recovery scopes, establish them before TradeExchange stages mutations, and set the terminal `TradeState` before `Commit()`; TradeExchange MUST NOT begin or commit those scopes. Terminal TradeExchange movement MUST use exact `IItemContainer.TryTransferTo(...)` for offered non-coin items and selected escrow recovery destinations inside the existing caller-owned transaction, and `IMoneyPouchContainer.TryTransferCoinsFrom(...)` for offered coins. It MUST NOT use standalone-capable destination `AddRange` or terminal offer `Clear` for authoritative escrow movement. Trade session owners MUST NOT acquire mutation-boundary locks directly. A trade offer's acceptance `Revision` MUST remain domain-owned and MUST advance according to its existing publication semantics, independently of storage revision.

#### Scenario: Storage revisions are independent from deterministic lock ordering
- **WHEN** storage changes during a transaction involving multiple boundaries
- **THEN** storage advances its own mutation revision while the transaction orders synchronization by boundary mutation order

### Requirement: Storage is synchronization-agnostic
`ItemContainerStorage` MUST own item state, revision, mutation algorithms, snapshot data, and rollback restoration only. It MUST NOT own a lock, deterministic mutation order, transaction binding, standalone publication ownership, synchronization authorization, or use `Monitor`. Its single owning `ItemContainerMutationBoundary` MUST own those concerns and protect that storage. Storage algorithms MUST assume the boundary has authorized the synchronous operation. `ItemContainerTransaction` MUST coordinate and order boundaries, and MUST key bindings, snapshots, rollback, and changed-slot tracking by boundary; it may access storage only for item-state data through the boundary.

#### Scenario: Storage mutation algorithms do not acquire synchronization
- **WHEN** an internal storage algorithm mutates item state
- **THEN** it performs item-state validation and mutation while its owning boundary provides synchronization and authorization

#### Scenario: Transaction bookkeeping identifies boundaries
- **WHEN** a transaction locks, binds, records changes, snapshots, or rolls back participants
- **THEN** it uses mutation boundaries as synchronization identity and accesses storage only for the item-state data being protected

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

#### Scenario: Failed transaction restores storage revision
- **GIVEN** enumerators exist before a multi-container transaction begins
- **WHEN** the transaction stages one or more storage changes but later rolls back
- **THEN** every participant's slots, item identities, counts, and mutation revision are restored
- **AND** the pre-existing enumerators remain valid
- **AND** no participant publishes a committed mutation or runs a committed domain effect

#### Scenario: Trade publication invalidates acceptance
- **WHEN** a trade offer publishes a content update
- **THEN** its acceptance revision advances independently of the storage enumeration revision

#### Scenario: Post-commit failures do not change transaction outcome
- **WHEN** mutations succeed and Commit reaches publication after releasing transaction locks
- **THEN** the transaction remains committed and runs changed participant publishers at most once in registration order, stopping at the first exception and propagating it directly

### Requirement: Multi-container mutations use an instance transaction
`ItemContainerTransaction` MUST provide one disposable atomic scope around existing synchronous domain operations. Every participant MUST be supplied before Begin, and storage aliases MUST be deduplicated without changing first-seen publication order. It MUST own ordered locks, snapshots, rollback and deferred publication. Domain operations MUST retain item semantics; the transaction MUST NOT own item operations. Transaction membership MUST be explicit at Begin. Ordinary mutations MUST operate through normal domain/container APIs and automatically participate only when their backing storage was enlisted; they MUST NOT create, commit, or dynamically join transactions. Cross-storage transfer MUST use `source.TryTransferTo(destination, ...)` and that public operation owns and commits a short transaction when neither storage is bound, participates without committing when both belong to one active current-thread transaction, and rejects partial/conflicting enlistment before mutation. MoneyPouch exact methods MUST select a standalone scope only when every required store is unbound, participate when every required store belongs to the same active current-thread scope, and reject partial/conflicting enlistment before mutation. Internal validation MUST reject wrong-thread use before mutation. If Commit cannot fully release transaction resources, observable completion and publication MUST NOT start, pending domain completion facts MUST still be discarded as non-observable cleanup, and committed storage MUST remain irreversible. No dynamic enlistment, separate unit of work, committed-state query, publication call, or ambient transaction API may be required. Async, nested and distributed transaction support MUST NOT be introduced.

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
- **THEN** its owning boundary is locked and the storage state is snapshotted once while participant publication order remains first-seen order

### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch exact additions and removals MUST mutate both pouch and inventory storage through the same active scope. Coin, slot-zero sentinel, balance, message, and event rules remain owned by MoneyPouch. Its captured notification facts MUST be owned by MoneyPouch and associated with the scope and published automatically after commit and unlock, and MoneyPouch MUST NOT snapshot and restore pouch storage as a separate rollback mechanism.

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

### Requirement: Equipment replacement preserves command behavior outside weapon and shield conflicts
Before its first mutation, `EquipmentContainer` MUST identify all conflicting equipped items and require `CanUnEquipItem` to succeed for each. Rejection MUST leave equipment and inventory unchanged and invoke no equip or unequip callback. For occupied replacement slots other than Weapon and Shield, a successful preflight MUST preserve the existing `UnEquipItem` command behavior, including custom and interactive behavior.

#### Scenario: A later conflict rejects replacement
- **WHEN** an earlier conflicting item allows unequipping but a later conflict rejects it
- **THEN** equipment and inventory remain unchanged and no mutation publication or callback occurs

### Requirement: Weapon and shield replacement commits conflicting items atomically
After all required `CanUnEquipItem` checks succeed, Weapon and Shield replacement MUST remove the incoming inventory item, move each conflicting equipped item into inventory, and place the same incoming item instance in its equipment slot within one `ItemContainerTransaction`. If any staged storage operation fails, inventory and equipment MUST be restored unchanged and no lifecycle callback or mutation publication may occur. A successful replacement MUST NOT invoke conflicting items' `UnEquipItem` commands. After commit and unlock, it MUST run `OnUnequipped` for a conflicting weapon, then a conflicting shield, then weapon profile/special-attack logic when a weapon is removed, then the incoming item's `OnEquipped`, all before participant publication.

#### Scenario: A second conflicting item cannot fit
- **WHEN** Weapon and Shield are both displaced but inventory can accept only one of them
- **THEN** the incoming item and both equipped items remain in their original slots and no lifecycle callback or mutation publication occurs

#### Scenario: A weapon and shield are displaced successfully
- **WHEN** both conflicting items fit after the incoming item leaves inventory
- **THEN** both original item instances are in inventory, the same incoming instance occupies its equipment slot, and lifecycle callbacks run in the specified order after commit and before publication

#### Scenario: A participant publisher throws after replacement commit
- **WHEN** replacement storage commits and a participant publisher throws
- **THEN** committed storage remains in place, all required lifecycle effects have been attempted, and later participant publishers are skipped

### Requirement: Transaction commit includes publication
A transaction MUST expose Begin, Commit, and Dispose as its public lifecycle. `Begin(...)` MUST create one synchronous scope over every resolved boundary contribution. Boundary mutation locks MUST be held while the scope is Active. Dispose without commit MUST restore all enlisted storage and revisions before clearing boundary bindings and releasing locks. Commit MUST declare current storage irreversible, release boundary mutation locks before domain hooks and automatic publication, and retain boundary bindings to the committing scope until committed completion/publication and pending-fact cleanup finish. A foreign overlapping `Begin(...)` MUST wait for the scope to finish; same-thread overlapping `Begin(...)` MUST be rejected. Ordinary mutation against a boundary bound to a Committed scope MUST be rejected. After completion/publication, bindings MUST be cleared under the ordered boundary locks, waiters awakened, and the transaction made Completed. Releasing a mutation lock alone MUST NOT make storage available while its committed state is still being observed. Callers MUST NOT require a separate publication step or committed-state query. Each changed container MUST publish once in existing observable order, independent of lock order. Completion failures MUST leave committed storage irreversible, release scope ownership, and MUST NOT be retried by Commit or Dispose. Transaction rollback restores slot topology, item references, item counts, and storage revision. It does not deep-snapshot arbitrary mutable `IItem` state, including `ExtraData`; callers requiring rollback of metadata MUST replace the item reference transactionally with a clone carrying the intended metadata rather than mutate rollback-sensitive metadata in place before commit.

#### Scenario: Storage commits before publication
- **WHEN** all staged mutations succeed
- **THEN** storage becomes irreversible before Commit releases locks and invokes deferred domain effects and publication

#### Scenario: Staging fails
- **WHEN** a scope exits without Commit after domain rejection or a mutation exception
- **THEN** disposal restores every enlisted store and revision, and emits no effects

#### Scenario: A participant publisher fails
- **WHEN** one participant throws during publication
- **THEN** later participants are skipped, storage remains committed, the original exception propagates directly, and another Commit is invalid and Dispose emits nothing

#### Scenario: Standalone publication retains mutation-time attribution
- **WHEN** a standalone mutation records changes and releases its mutation lock before publication while another thread begins an overlapping transaction
- **THEN** the standalone publication owner prevents the transaction from binding until publication finishes, and the standalone change is published before the later transaction begins

### Requirement: Standalone publication retains boundary ownership
A standalone mutation that records changes MUST claim boundary-owned publication ownership while holding the boundary mutation lock before releasing it. That ownership MUST remain through standalone domain lifecycle effects and publication, while observable callbacks run without the boundary lock. Ordinary mutations MUST reject a boundary with standalone publication ownership. Explicit transaction `Begin(...)` MUST wait for a foreign standalone publication owner and reject same-thread overlap; a multi-boundary Begin MUST release any acquired lock prefix before waiting and retry deterministic acquisition after ownership clears. Standalone completion MUST clear ownership and pulse waiters under the boundary lock after publication. A publisher failure MUST still trigger the normal ownership-release cleanup before propagating. Synchronization invariant failures MUST propagate directly and MUST NOT be aggregated with publication failures.

#### Scenario: Ordinary mutation cannot change state during standalone publication
- **WHEN** a standalone publisher is running with logical publication ownership retained on its boundary
- **THEN** same-thread and foreign ordinary mutations throw before changing storage

#### Scenario: Equipment lifecycle retains standalone ownership
- **WHEN** standalone Equipment lifecycle effects run after releasing the mutation lock and before publication
- **THEN** an overlapping foreign transaction waits through both lifecycle effects and publication, while same-thread reentrant Begin is rejected

#### Scenario: Standalone publication cleanup releases ownership after failure
- **WHEN** the standalone publisher throws after publication ownership was claimed
- **THEN** cleanup clears ownership and pulses waiters under the boundary lock, and the publication exception propagates

### Requirement: Transaction change attribution validates boundary ownership
`ItemContainerTransaction.RecordChanges(...)` MUST accept changes only when the given boundary is bound to that exact active transaction and the calling thread holds that boundary's mutation lock. It MUST reject unbound, differently bound, or unlocked boundaries before adding change data.

#### Scenario: Change attribution rejects a boundary outside the transaction
- **WHEN** a transaction records changes for a boundary that it did not enlist
- **THEN** it throws without binding or mutating the other storage

#### Scenario: Change attribution requires the enlisted boundary lock
- **WHEN** an active transaction records changes for its enlisted boundary without owning its mutation lock
- **THEN** it throws and leaves transaction change attribution unchanged

### Requirement: Automatic economic publication preserves domain behavior
MoneyPouch MUST internally record immutable notification facts containing the previous count, new count, and domain-visible change amount for automatic publication after container publishers. Callers MUST NOT manage notification receipts. Messages MUST use the captured change amount, then events MUST use the captured previous and new counts; publication MUST NOT reconstruct events from live pouch state. An inventory-only pouch addition MUST NOT publish a pouch effect. Equipment MUST own its ordered lifecycle/profile hook batch and retain attempt-all behavior after storage becomes irreversible and locks are released. Equipment MUST execute typed completion facts directly without converting them to executable delegate arrays. For standalone equipment changes, lifecycle effects and normal publication MUST be attempted explicitly before retained failures are surfaced. Normal container publication MUST stop at its first failure, skipping later containers and all pouch publication; pouch publication MUST stop at its first failure. A single failure MUST preserve the original exception; independent hook and publication failures MUST preserve both original exceptions in AggregateException. Shop purchases MUST sort stock and send the purchase event only after Commit finishes publication.

#### Scenario: Multiple pouch mutations retain their own facts
- **WHEN** a transaction changes pouch count from 10 to 13 and then from 13 to 12
- **THEN** the first publication sends the +3 message and event 10 to 13, and the second sends the -1 message and event 13 to 12

#### Scenario: Overlapping publication reentrancy is rejected
- **WHEN** committed pouch or container publication synchronously attempts to begin or mutate an overlapping storage scope on the same thread
- **THEN** the operation throws `InvalidOperationException` while the original committed scope remains bound, and original publication and cleanup continue according to their failure policy

#### Scenario: Disjoint publication work remains independent
- **WHEN** committed completion or publication starts a transaction over storage that does not overlap the committing scope
- **THEN** the disjoint scope proceeds normally and pending completion facts remain associated with their originating transaction

#### Scenario: Equipment lifecycle fails after commit
- **WHEN** an equipment lifecycle effect throws after storage commits
- **THEN** later required lifecycle effects and transaction publication are still attempted under the equipment owner's existing cleanup policy and storage is not rolled back

#### Scenario: Shop publication fails after purchase commit
- **WHEN** container or pouch publication throws after a purchase commits
- **THEN** later stock sorting and the purchase event are skipped, storage remains committed, and the original publication exception propagates directly

### Requirement: Disposable atomic mutation scope
An item transaction MUST expose Begin, Commit, and Dispose, with every participant resolved and validated before locking. It MUST capture all snapshots before establishing originating-thread boundary bindings. Construction failure MUST release acquired locks, leave storage unchanged, and publish nothing because no bindings have yet been established. Dispose without commit MUST attempt every snapshot restore while retaining participant locks and bindings, discard pending completion facts, clear bindings, and release locks; rollback MUST NOT pulse waiters because contenders cannot acquire a participant lock before bindings are cleared. Commit MUST declare already-mutated storage irreversible before dropping snapshots and releasing mutation-boundary locks; it MUST retain boundary bindings while executing callbacks, then clear bindings under the ordered boundary locks and pulse waiters. Domain completion and publication MUST NOT run unless all transaction mutation locks were released. Synchronization invariant failures MUST propagate normally and MUST NOT be aggregated with domain failures. Repeated owner-thread disposal MUST be inert and repeated commit MUST NOT retry publication.

#### Scenario: Construction fails during snapshot capture
- **WHEN** snapshot capture throws after locks have been acquired
- **THEN** all acquired locks are released, no binding survives, and storage and publication remain unchanged

#### Scenario: Mutation scope exits without commit
- **WHEN** an early return, exception, or cancellation exits an active scope
- **THEN** disposal restores every participant and emits no deferred effect

#### Scenario: Completion throws after commit
- **WHEN** a hook or publisher throws after the irreversible transition
- **THEN** committed storage remains permanent, all transaction locks have been released, disposal is inert, and commit cannot retry completion

### Requirement: Complete same-scope participation
Every transaction MUST have one explicit owner. A same-thread Begin on a boundary already bound to a transaction MUST throw, while a foreign overlapping Begin MUST wait until the existing scope ends. All transaction participants MUST be supplied to Begin; ordinary operations MUST NOT dynamically enlist storage. Ordinary `IItemContainer` mutations MUST use the active scope automatically when their boundary is enlisted and MUST remain standalone when it is not. MoneyPouch exact operations MUST own a scope when the current thread owns neither its pouch nor its single inventory boundary, participate when both are owned by the same active current-thread transaction, and throw before mutation when the inventory boundary is already owned by a current-thread transaction that does not include the pouch boundary. A foreign overlap MUST be handled by the standalone `Begin` path's normal contention. `IItemContainer.TryTransferTo` MUST own and commit a short transaction when neither required boundary is bound, participate without committing when both boundaries belong to the same active current-thread transaction, and reject partial or conflicting enlistment before mutation. `IMoneyPouchContainer.TryTransferCoinsFrom` MUST require its source boundary, pouch boundary, and single inventory boundary to belong to the same active caller-owned transaction; it MUST NOT create a transaction. Partial enlistment, conflicting active scopes, nested Begin, and wrong-thread use MUST throw `InvalidOperationException` before mutation. Participant aliases MUST be deduplicated independently from first-seen publication order; two different mutation boundaries for one storage MUST be rejected before locking, while repeated contributions of the same boundary instance are valid.

#### Scenario: Independent overlapping scopes contend
- **WHEN** another thread owns any required boundary and the current thread owns none of it
- **THEN** Begin waits on deterministic boundary locks and proceeds after the other scope completes

#### Scenario: Nested Begin is rejected
- **WHEN** the current thread begins a transaction for storage already enlisted in its active transaction
- **THEN** Begin throws and leaves the existing scope unchanged

#### Scenario: A helper requires missing storage
- **WHEN** a composable operation requires A and B but the caller's scope includes only A
- **THEN** it throws without locking or mutating B, and A remains bound to the original scope

#### Scenario: A MoneyPouch method rejects partial participation
- **WHEN** public `TryAddExact` or `TryRemoveExact` is called while the current thread owns the inventory boundary through a transaction that does not include the pouch
- **THEN** it fails before changing either scope or storage

#### Scenario: An exact MoneyPouch operation waits for a foreign owner
- **WHEN** another thread owns either required boundary and the current thread calls `TryAddExact` or `TryRemoveExact`
- **THEN** the operation uses normal Begin contention and proceeds after the foreign scope completes

#### Scenario: Coin transfer requires a complete caller-owned scope
- **WHEN** `TryTransferCoinsFrom(...)` is called without an active transaction containing its source, pouch, and inventory boundary
- **THEN** it throws before mutation and does not create an independent transaction

#### Scenario: MoneyPouch contributes its two storage boundaries
- **WHEN** a transaction begins with the MoneyPouch aggregate
- **THEN** exactly the pouch storage and its inventory item storage are enlisted

#### Scenario: Ordinary item-container methods participate when enlisted
- **WHEN** an ordinary item-container mutation is called for storage already enlisted in a transaction
- **THEN** it changes that storage and defers publication until the transaction commits

#### Scenario: An unenlisted item-container remains standalone
- **WHEN** a transaction includes A but not independent container B, and both containers are mutated
- **THEN** disposal restores A without publication while B retains its mutation and publishes normally

#### Scenario: Storage belongs to different scopes
- **WHEN** required storage belongs to different active transactions
- **THEN** the operation throws without mutation or nested transaction creation

#### Scenario: A composite participant aliases storage
- **WHEN** two participants contribute the same underlying storage
- **THEN** that storage's owning boundary is locked and its state snapshotted once without changing participant publication order

### Requirement: Automatic ordered transaction completion
Commit MUST perform automatic completion after unlock without a separate caller publication call. Fixed pre-publication domain completion MUST precede normal container publication and domain-owned batches MUST retain their existing failure policy. Container publishers MUST retain existing observable order independently of lock order; the first failure MUST skip later container publishers and all post-publication domain completion. Completion owners MUST be visited for each resolved boundary that has an owner, in participant encounter and contribution order before lock sorting. Owners MUST process their own pending facts in FIFO order; no cross-owner mutation-interleaving guarantee is provided. Fixed post-publication completion MUST run only after normal container publication, with storage irreversible and locks released; its first failure MUST stop later boundaries with owners. Completion facts MUST be discarded before boundary ownership is released. The transaction MUST NOT store executable domain callbacks or offer callback registration. Repository-owned mutation boundaries MUST invoke fixed owner completion stages. A single failure MUST preserve its original exception; multiple hook failures and an independent publication failure MUST survive as original leaf exceptions in one flat AggregateException. Validation determining mutation eligibility MUST remain before commit.

#### Scenario: Container publication fails
- **WHEN** a container publisher throws
- **THEN** later containers and all post-publication domain completion are skipped and storage stays committed

#### Scenario: Post-publication domain completion fails
- **WHEN** post-publication completion for one owner throws after all container publication
- **THEN** earlier completion remains observable, later owners are skipped, remaining facts are discarded, and storage stays committed

#### Scenario: Equipment hooks and publication both fail
- **WHEN** an equipment-owned hook batch and normal publication both throw
- **THEN** required equipment hooks have been attempted and AggregateException retains both original failures

#### Scenario: Rollback discards owner completion
- **WHEN** equipment and pouch mutations record pending effects but the scope is disposed without commit
- **THEN** storage is restored, pending owner facts are discarded without observable effects, and a later unrelated scope executes none of those effects

#### Scenario: Completion owners follow resolved boundary order
- **WHEN** participants are supplied in an order different from lock acquisition and mutation order
- **THEN** the completion owners attached to resolved boundaries are visited in boundary order, independent of lock and mutation order

#### Scenario: One owner preserves its pending fact order
- **WHEN** one completion owner records multiple facts in a transaction
- **THEN** that owner completes the facts in the order they were recorded

#### Scenario: Completion starts a disjoint scope
- **WHEN** post-unlock domain completion synchronously starts a new scope over storage not owned by the committing scope
- **THEN** the disjoint scope proceeds normally and pending completion facts remain associated with their originating transaction

#### Scenario: Completion cannot re-enter an overlapping scope
- **WHEN** committed completion attempts same-thread Begin or mutation against storage still owned by the committing scope
- **THEN** it throws `InvalidOperationException` and cannot consume or alter the committing scope's storage

### Requirement: Mutation authorization and attribution are boundary-owned
Every ordinary mutation MUST be authorized by its owning boundary while holding that boundary's mutation lock. Storage mutation algorithms MUST NOT acquire locks or validate synchronization themselves. One per-operation mutation scope MUST acquire the boundary lock for standalone mutations or borrow the lock already held by the active owning transaction, and MUST release only a lock it acquired. If the current thread already owns the boundary lock without an active transaction binding, starting an ordinary mutation scope MUST fail. The scope MUST validate the boundary's transaction before allowing a borrowed mutation. Successful change attribution MUST occur while the same boundary lock is still held. Transaction-owned changes MUST be recorded by boundary into that active transaction; standalone changes MUST be published only after the scope releases its owned lock. A scope that records no changes MUST publish nothing. Ordinary callers MUST NOT branch on deferred/immediate publication state or re-read transaction ownership after attribution. Transaction membership remains boundary-bound; ambient transaction accessors such as `AsyncLocal`, `ThreadLocal`, `ThreadStatic` current-transaction state, and implicit current-scope APIs are prohibited.

#### Scenario: Standalone mutation uses one boundary lock and publishes after unlock
- **WHEN** an ordinary mutation runs on unbound storage and succeeds
- **THEN** its operation scope acquires the owning boundary lock once, attributes the change while holding it, releases it, and only then publishes

#### Scenario: Transaction-owned mutation borrows its boundary lock
- **WHEN** an ordinary mutation runs on storage whose boundary is enlisted in the current thread's active transaction
- **THEN** its operation scope does not reacquire or release the transaction-owned lock and records changes into that transaction before returning

#### Scenario: Storage algorithms do not enforce boundary ownership
- **WHEN** an internal storage mutation algorithm is called directly
- **THEN** it applies only item-state rules and performs no lock or transaction checks

#### Scenario: Manually held boundary lock cannot imply transaction ownership
- **WHEN** the current thread holds a boundary mutation lock that has no active transaction binding and starts an ordinary mutation scope
- **THEN** the scope throws without borrowing or releasing the externally owned lock

#### Scenario: Unsuccessful standalone mutation publishes nothing
- **WHEN** an ordinary operation returns without recording changes
- **THEN** its owned lock is released and no publication occurs

#### Scenario: Transaction attribution uses no ambient scope
- **WHEN** an ordinary item-container mutation runs
- **THEN** participation is determined from the boundary binding under its lock without ambient state or transaction identity passed through the operation

### Requirement: Transaction rollback restores through the owning boundary
Rollback authorization MUST require the transaction's owning boundary lock and matching boundary binding, and MUST NOT reacquire the same lock. Storage snapshot restoration itself MUST contain no synchronization checks. It is not an ordinary active mutation and MUST NOT use normal active-transaction mutation authorization. The transaction MUST retain participant boundary locks and bindings until all snapshot restores have been attempted, then clear bindings and release locks.

#### Scenario: Rollback restores while transaction owns locks
- **WHEN** a transaction is disposed without commit
- **THEN** it restores each snapshot while holding the participant locks, attempts every restore, and publishes nothing

### Requirement: Read-only item access is an explicit capability
`IReadOnlyItemContainer` MUST expose indexed/enumerable item reads and item-specific queries without mutation or transaction participation. `IItemContainer` MUST inherit this capability and retain mutation members. `IEquipmentContainer` MUST compose an `IReadOnlyItemContainer Items` view and retain its equipment-slot indexer and semantic operations.

#### Scenario: Equipment exposes only a read-only composed view
- **WHEN** a caller accesses equipment through `IEquipmentContainer`
- **THEN** generic item reads and queries are available through `Items`, equipment-slot reads remain available through the `EquipmentSlot` indexer, and no generic mutation or transaction capability is exposed

#### Scenario: Equipment read view reflects its storage
- **WHEN** equipment contains an item
- **THEN** the read-only view reports the same capacity, slots, enumeration, ID lookup, counts, and containment as the equipment storage

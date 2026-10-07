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
MoneyPouch MUST contribute exactly its pouch storage and its inventory item storage to a transaction. Exact additions and removals MUST mutate both through the same transaction. When neither boundary is owned by a current-thread transaction, an exact operation owns a transaction; it participates only when both boundaries belong to the same active transaction and throws before mutation on partial current-thread participation. A foreign overlap uses normal transaction contention. `TryTransferCoinsFrom(...)` MUST require its source, pouch, and inventory boundaries in one caller-owned transaction and MUST NOT create a transaction. Coin, slot-zero sentinel, and balance rules remain owned by MoneyPouch; it MUST NOT keep a separate rollback mechanism.

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory mutations are staged but a later participant rejects its operation
- **THEN** transaction rollback restores both stores and no pouch message or event is published

#### Scenario: MoneyPouch exact operations own or participate in one scope
- **WHEN** an exact add or remove runs with neither pouch nor inventory boundary owned by a current-thread transaction
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
- **WHEN** another thread owns either required boundary and the current thread calls `TryAddExact` or `TryRemoveExact`
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

### Requirement: Transaction construction and rollback preserve storage state
`ItemContainerTransaction` MUST expose Begin, Commit, and Dispose as one synchronous atomic lifecycle. Begin MUST resolve and validate participant contributions in encounter order before locking, deduplicate repeated boundary contributions without changing resolved publication order, and reject one storage exposed through different boundaries. It MUST acquire boundary locks in deterministic order, capture snapshots, and establish bindings only after construction succeeds. A construction failure MUST release acquired locks in reverse order without changing storage or publishing. Mutations affect live storage while the scope is active. Dispose without Commit MUST attempt every snapshot restoration while retaining the transaction bindings and locks, discard pending completion facts, then clear bindings and release locks without publishing or pulsing waiters. Snapshots restore slots, item references, counts, and storage revision, but do not deep-copy mutable `IItem` state such as `ExtraData`.

Commit MUST make the current storage state irreversible before discarding snapshots. It MUST release every mutation lock before invoking domain hooks or publishers, while retaining boundary bindings until completion and publication finish. Once the scope is fully completed, bindings MUST be cleared under ordered boundary locks and waiters awakened. A foreign overlapping Begin waits until then; same-thread overlapping Begin and ordinary mutation against a committed binding are rejected. Dispose after commit is inert, and another Commit is invalid.

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
- **WHEN** another thread owns a required boundary and the current thread owns none of the requested boundaries
- **THEN** Begin waits until the foreign scope completes, then acquires locks in deterministic order

#### Scenario: Nested Begin on an enlisted boundary is rejected
- **WHEN** the current thread begins a transaction for storage already enlisted in its active transaction
- **THEN** Begin throws and leaves the existing scope unchanged

### Requirement: Commit completes domain effects and publication in order
Commit MUST run pre-publication completion, changed container publication, then post-publication completion. Completion owners run once per owner-bearing resolved boundary in participant encounter and contribution order before lock sorting; each owner processes its facts in FIFO order, with no cross-owner fact-interleaving guarantee. Container publication uses its existing observable order independently of lock order; its first failure skips later container publishers and all post-publication completion. Post-publication completion runs only if container publication finishes, and its first failure skips later owners. Pending facts are discarded before bindings are released.

An exception after the irreversible point leaves storage committed. A single failure propagates as its original exception; independent Equipment hook and publication failures both survive in one AggregateException. Validation needed to determine whether a mutation is allowed MUST happen before Commit. The transaction MUST NOT store executable domain callbacks or expose callback registration.

#### Scenario: Container publication fails
- **WHEN** a container publisher throws
- **THEN** later containers and all post-publication completion are skipped, storage remains committed, and the original exception propagates

#### Scenario: Post-publication completion fails
- **WHEN** one completion owner throws after container publication succeeds
- **THEN** earlier completion remains observable, later owners are skipped, remaining facts are discarded, and storage remains committed

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

When a standalone mutation records changes, the boundary MUST claim publication ownership before releasing its lock. It retains that ownership through lifecycle effects and publication while callbacks run without the lock. Ordinary mutations MUST reject that boundary during publication; same-thread overlapping Begin is rejected and foreign overlapping Begin waits. A multi-boundary Begin MUST release any acquired lock prefix before waiting and retry deterministic acquisition after ownership clears. Standalone cleanup MUST clear ownership and pulse waiters under the boundary lock, including after publisher failure.

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
- **WHEN** a standalone publisher runs while logical ownership remains on its boundary
- **THEN** same-thread and foreign ordinary mutations throw before changing storage

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
`IReadOnlyItemContainer` MUST expose indexed/enumerable item reads and item-specific queries without mutation or transaction participation. `IItemContainer` MUST inherit this capability and retain mutation members. `IEquipmentContainer` MUST compose an `IReadOnlyItemContainer Items` view and retain its equipment-slot indexer and semantic operations.

#### Scenario: Equipment exposes only a read-only composed view
- **WHEN** a caller accesses equipment through `IEquipmentContainer`
- **THEN** generic item reads and queries are available through `Items`, equipment-slot reads remain available through the `EquipmentSlot` indexer, and no generic mutation or transaction capability is exposed

#### Scenario: Equipment read view reflects its storage
- **WHEN** equipment contains an item
- **THEN** the read-only view reports the same capacity, slots, enumeration, ID lookup, counts, and containment as the equipment storage

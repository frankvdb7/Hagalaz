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

### Requirement: MoneyPouch separates gameplay operations from transaction participation
`IMoneyPouchContainer` MUST expose normal coin-domain operations and implement the empty public `IItemTransactional` capability marker. It MUST NOT expose a `Mutations` property or storage boundary. Its concrete domain implementation MUST provide pouch and every required inventory storage through the internal `IItemTransactionSource`. `TryAddExact` and `TryRemoveExact` MUST own a transaction when none of their required storage is bound, participate in the same active current-thread transaction when every required storage is bound to it, and throw before mutation for partial or conflicting enlistment. The primary MoneyPouch API MUST NOT expose generic item-ID `Contains`, a domain-specific mutation boundary, or caller-managed publication receipts.

#### Scenario: MoneyPouch coin availability is domain-specific
- **WHEN** a caller checks whether a character has a positive coin amount
- **THEN** `HasCoins` compares the request against pouch coins and inventory coin item `995` using overflow-safe addition

### Requirement: ItemContainer storage stays encapsulated
Ordinary domain owners that compose `ItemContainer` MUST NOT recover or access its `ItemContainerStorage` instance. Persistence and specialized state maintenance MUST use narrow internal `ItemContainer` operations. Direct `ItemContainerStorage` ownership is reserved for domain containers whose invariants require bypassing the generic facade, currently `EquipmentContainer` and `MoneyPouchContainer`.

#### Scenario: Ordinary container persistence uses the facade
- **WHEN** an ordinary domain owner hydrates, dehydrates, or normalizes item state
- **THEN** it uses narrow `ItemContainer` operations and does not obtain raw storage

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST implement `IItemContainer` and the empty public `IItemTransactional` marker, privately composing one `ItemContainerStorage` and one `ItemContainerMutationBoundary`; its internal `IItemTransactionSource` implementation MUST contribute that boundary. Ordinary domain implementations privately own concrete `ItemContainer`; ordinary domain interfaces MUST expose `IItemContainer Items` and MUST NOT copy generic forwarding operations. Equipment and MoneyPouch MUST compose storage directly with private mutation boundaries and MUST NOT expose generic `Items` or raw storage boundaries. `IEquipmentContainer` MUST expose `IReadOnlyItemContainer Items` and MUST NOT expose generic mutation; its private `ItemContainerStorage` MAY implement `IReadOnlyItemContainer` directly without a forwarding wrapper. TradeOffer, Duel, and Price Checker MUST compose the concrete `ItemContainer` where generic behavior is required. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST NOT be part of its contract. Transaction membership MUST be explicit by passing all participating domain objects directly to `ItemContainerTransaction.Begin(...)`; `IItemContainer` and `IMoneyPouchContainer` MUST implement `IItemTransactional` and MUST NOT expose a public `Mutations` property. The internal `IItemTransactionSource` MUST resolve storage contributions. `IItemContainer.TryTransferTo` MUST expose atomic cross-container transfer. Ordinary mutations on enlisted storage MUST participate automatically and defer publication through the active transaction; ordinary mutations on unenlisted storage MUST remain standalone. Ordinary mutation methods MUST NOT create, commit, or dynamically enlist a transaction. Domain callers MUST NOT cast containers to recover storage infrastructure. `ItemContainerStorage` MUST own low-level slot and mutation mechanics and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `ItemContainerTransaction` MUST own deterministic lock ordering, snapshots, rollback, changed-slot tracking, and post-commit publication. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST remain available through the ordinary container API. MoneyPouch MUST perform pouch and inventory mutations through the same scope, with its internal source contributing both storage boundaries; it MUST NOT expose the concrete storage boundary or keep a separate rollback path. Concrete storage and mutation-boundary implementations MUST remain internal; Scripts MUST use public domain capabilities without friend-assembly access.

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
An exact cross-container transfer MUST enter through `IItemContainer.TryTransferTo(...)` and use the one caller-owned `ItemContainerTransaction` to validate and plan source removal and destination insertion before changing either store, acquire distinct locks in stable order, call the single low-level `ItemContainerStorage` transfer algorithm, advance each storage revision once, and publish changed slots only after successful commit. The transfer method MUST NOT create or commit a transaction or dynamically enlist the destination. A standalone domain owner MUST supply both participants before mutation; a composable operation MUST use the existing transaction only when both storages belong to it.

#### Scenario: Exact transfer succeeds
- **WHEN** the caller enlists source and destination, the source has the requested quantity, the destination can accept the exact result, and the caller commits
- **THEN** both stores commit the transfer before either domain container publishes an update

#### Scenario: Transfer fails validation
- **WHEN** the source quantity is insufficient or the destination cannot accept the result
- **THEN** neither store changes and neither container publishes a committed mutation

#### Scenario: Storage transfer requires one active transaction
- **WHEN** the transfer boundary is called without both boundaries enlisted in one active transaction, or the low-level source storage transfer primitive is called without a transaction shared by source and destination or from a thread other than the transaction owner
- **THEN** it throws before either store changes

#### Scenario: Opposite transfers acquire locks consistently
- **WHEN** two operations transfer in opposite directions between the same stores
- **THEN** their owner scopes acquire store locks in the same stable order, and an independent scope waits until the current scope releases its locks

#### Scenario: Transfer is exposed by the container
- **WHEN** a caller inspects the public `IItemContainer` contract
- **THEN** it finds `TryTransferTo(...)` on the interface; transaction membership is declared by passing the aggregate object directly to `Begin(...)`

### Requirement: Storage and trade revisions have distinct purposes
Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerTransaction` MUST own lock ordering; `ItemContainerMutationBoundary.TryTransferTo` MUST require an existing caller-owned scope containing both boundaries. `TradingCharacterScript` MUST own terminal completion, refund, and forced recovery scopes, establish them before TradeExchange stages mutations, and set the terminal `TradeState` before `Commit()`; TradeExchange MUST NOT begin or commit those scopes. Trade session owners MUST NOT acquire storage locks directly. A trade offer's acceptance `Revision` MUST remain domain-owned and MUST advance according to its existing publication semantics, independently of storage revision.

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
`ItemContainerTransaction` MUST provide one disposable atomic scope around existing synchronous domain operations. Every participant MUST be supplied before Begin, and storage aliases MUST be deduplicated without changing first-seen publication order. It MUST own ordered locks, snapshots, rollback and deferred publication. Domain operations MUST retain item semantics; the transaction MUST NOT own item operations. Transaction membership MUST be explicit at Begin. Ordinary mutations MUST operate through normal domain/container APIs and automatically participate only when their backing storage was enlisted; they MUST NOT create, commit, or dynamically join transactions. Cross-storage transfer MUST use `source.TryTransferTo(destination, ...)` and require both storages in the one caller-owned transaction. MoneyPouch exact methods MUST select a standalone scope only when every required store is unbound, participate when every required store belongs to the same active current-thread scope, and reject partial/conflicting enlistment before mutation. Internal validation MUST reject wrong-thread use before mutation. If Commit cannot fully release transaction resources, observable completion and publication MUST NOT start, pending domain completion facts MUST still be discarded as non-observable cleanup, and committed storage MUST remain irreversible. No dynamic enlistment, separate unit of work, committed-state query, publication call, or ambient transaction API may be required. Async, nested and distributed transaction support MUST NOT be introduced.

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
- **THEN** it uses existing container operations under the session-owned scope's deterministic storage locks and publishes through domain owners only after commit

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
A transaction MUST expose Begin, Commit, and Dispose as its public lifecycle. `Begin(...)` MUST create one synchronous scope over every resolved storage contribution. Mutation locks MUST be held while the scope is Active. Dispose without commit MUST restore all enlisted storage and revisions before clearing bindings and releasing locks. Commit MUST declare current storage irreversible, release mutation locks before domain hooks and automatic publication, and retain storage bindings to the committing scope until committed completion/publication and pending-fact cleanup finish. A foreign overlapping `Begin(...)` MUST wait for the scope to finish; same-thread overlapping `Begin(...)` MUST be rejected. Ordinary mutation against storage bound to a Committed scope MUST be rejected. After completion/publication, bindings MUST be cleared under the ordered storage locks, waiters awakened, and the transaction made Completed. Callers MUST NOT require a separate publication step or committed-state query. Each changed container MUST publish once in existing observable order, independent of lock order. Completion failures MUST leave committed storage irreversible, release scope ownership, and MUST NOT be retried by Commit or Dispose.

#### Scenario: Storage commits before publication
- **WHEN** all staged mutations succeed
- **THEN** storage becomes irreversible before Commit releases locks and invokes deferred domain effects and publication

#### Scenario: Staging fails
- **WHEN** a scope exits without Commit after domain rejection or a mutation exception
- **THEN** disposal restores every enlisted store and revision, and emits no effects

#### Scenario: A participant publisher fails
- **WHEN** one participant throws during publication
- **THEN** later participants are skipped, storage remains committed, the original exception propagates directly, and another Commit is invalid and Dispose emits nothing

### Requirement: Automatic economic publication preserves domain behavior
MoneyPouch MUST internally record immutable notification facts containing the previous count, new count, and domain-visible change amount for automatic publication after container publishers. Callers MUST NOT manage notification receipts. Messages MUST use the captured change amount, then events MUST use the captured previous and new counts; publication MUST NOT reconstruct events from live pouch state. An inventory-only pouch addition MUST NOT publish a pouch effect. Equipment MUST own its ordered lifecycle/profile hook batch and retain attempt-all behavior after storage becomes irreversible and locks are released. Equipment MUST execute typed completion facts directly without converting them to executable delegate arrays. For standalone equipment changes, lifecycle effects and normal publication MUST be attempted explicitly before retained failures are surfaced. Normal container publication MUST stop at its first failure, skipping later containers and all pouch publication; pouch publication MUST stop at its first failure. A single failure MUST preserve the original exception; independent hook and publication failures MUST preserve both original exceptions in AggregateException. Shop purchases MUST sort stock and send the purchase event only after Commit finishes publication.

#### Scenario: Multiple pouch mutations retain their own facts
- **WHEN** a transaction changes pouch count from 10 to 13 and then from 13 to 12
- **THEN** the first publication sends the +3 message and event 10 to 13, and the second sends the -1 message and event 13 to 12

#### Scenario: Reentrant pouch mutation preserves captured events
- **WHEN** a pouch publication starts another transaction before the earlier event is sent
- **THEN** each event reports its originating mutation's captured previous and new counts, regardless of current live pouch state

#### Scenario: Equipment lifecycle fails after commit
- **WHEN** an equipment lifecycle effect throws after storage commits
- **THEN** later required lifecycle effects and transaction publication are still attempted under the equipment owner's existing cleanup policy and storage is not rolled back

#### Scenario: Shop publication fails after purchase commit
- **WHEN** container or pouch publication throws after a purchase commits
- **THEN** later stock sorting and the purchase event are skipped, storage remains committed, and the original publication exception propagates directly

### Requirement: Disposable atomic mutation scope
An item transaction MUST expose Begin, Commit, and Dispose, with every participant resolved and validated before locking. It MUST capture all snapshots before establishing originating-thread bindings. Construction failure MUST remove bindings, release all acquired locks, leave storage unchanged, and publish nothing. Dispose without commit MUST restore all captured references, counts, and revisions before clearing bindings, pulsing waiters, and releasing locks. Commit MUST declare already-mutated storage irreversible before dropping snapshots and releasing mutation locks; it MUST retain scope bindings while executing callbacks, then clear bindings under the ordered storage locks and pulse waiters. Cleanup MUST attempt every owned resource; callbacks MUST NOT execute while a transaction lock remains intentionally held. A foreign overlapping Begin waits for a bound scope and retries ordered acquisition, while same-thread overlapping Begin throws. Repeated owner-thread disposal MUST be inert and repeated commit MUST NOT retry publication.

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
Every transaction MUST have one explicit owner. A same-thread Begin on storage already bound to a transaction MUST throw, while a foreign overlapping Begin MUST wait until the existing scope ends. All transaction participants MUST be supplied to Begin; ordinary operations MUST NOT dynamically enlist storage. Ordinary `IItemContainer` mutations MUST use the active scope automatically when their storage is enlisted and MUST remain standalone when it is not. MoneyPouch exact operations MUST own a scope when none of their required storage is bound, participate when all required storage belongs to one active current-thread transaction, and throw before mutation for partial or conflicting enlistment. `IItemContainer.TryTransferTo` MUST require both source and destination storage in the caller-owned transaction and MUST NOT create a nested transaction or acquire missing storage. Partial enlistment, conflicting active scopes, nested Begin, and wrong-thread use MUST throw `InvalidOperationException` before mutation. Participant aliases MUST be deduplicated independently from first-seen publication order.

#### Scenario: Independent overlapping scopes contend
- **WHEN** another thread owns any required storage and the current thread owns none of it
- **THEN** Begin waits on deterministic storage locks and proceeds after the other scope completes

#### Scenario: Nested Begin is rejected
- **WHEN** the current thread begins a transaction for storage already enlisted in its active transaction
- **THEN** Begin throws and leaves the existing scope unchanged

#### Scenario: A helper requires missing storage
- **WHEN** a composable operation requires A and B but the caller's scope includes only A
- **THEN** it throws without locking or mutating B, and A remains bound to the original scope

#### Scenario: A MoneyPouch method rejects partial participation
- **WHEN** public `TryAddExact` or `TryRemoveExact` is called while only some required pouch or inventory storage is enlisted
- **THEN** it fails before changing either scope or storage

#### Scenario: A MoneyPouch method rejects conflicting scopes
- **WHEN** required pouch and inventory storage belong to different active transactions
- **THEN** it fails before changing either scope or storage

#### Scenario: Opaque pouch participant contributes all required storage
- **WHEN** a transaction begins with the MoneyPouch aggregate
- **THEN** pouch storage and every inventory storage contribution are enlisted together

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
- **THEN** that storage is locked and snapshotted once without changing participant publication order

### Requirement: Automatic ordered transaction completion
Commit MUST perform automatic completion after unlock without a separate caller publication call. Fixed pre-publication domain completion MUST precede normal container publication and domain-owned batches MUST retain their existing failure policy. Container publishers MUST retain existing observable order independently of lock order; the first failure MUST skip later container publishers and all post-publication domain completion. Fixed post-publication domain completion MUST execute in mutation order only after normal container publication, with storage irreversible and locks released. Its first failure MUST stop later completion. The transaction MUST NOT store executable domain callbacks or offer callback registration. Repository-owned mutation boundaries MUST invoke fixed owner completion stages. Domains MUST own pending completion data keyed by the originating scope, and discard it on rollback or unsuccessful completion without observable effects. A single failure MUST preserve its original exception; multiple hook failures and an independent publication failure MUST survive as original leaf exceptions in one flat AggregateException. Validation determining mutation eligibility MUST remain before commit.

#### Scenario: Container publication fails
- **WHEN** a container publisher throws
- **THEN** later containers and all post-publication domain completion are skipped and storage stays committed

#### Scenario: Post-publication domain completion fails
- **WHEN** post-publication domain completion throws after all container publication
- **THEN** earlier completion remains observable, later completion is skipped and storage stays committed

#### Scenario: Equipment hooks and publication both fail
- **WHEN** an equipment-owned hook batch and normal publication both throw
- **THEN** required equipment hooks have been attempted and AggregateException retains both original failures

#### Scenario: Rollback discards owner completion
- **WHEN** equipment and pouch mutations record pending effects but the scope is disposed without commit
- **THEN** storage is restored, pending owner facts are discarded without observable effects, and a later unrelated scope executes none of those effects

#### Scenario: Multiple mutations retain order across owners
- **WHEN** one scope records interleaved equipment or pouch mutations, including aliased participants
- **THEN** completion executes each pending operation once in mutation order independently of participant and lock order

#### Scenario: Completion starts another scope
- **WHEN** post-unlock domain completion synchronously starts a new scope on the same owner
- **THEN** pending facts remain associated with their originating scope and neither scope consumes or discards the other's facts

### Requirement: Read-only item access is an explicit capability
`IReadOnlyItemContainer` MUST expose indexed/enumerable item reads and item-specific queries without mutation or transaction participation. `IItemContainer` MUST inherit this capability and retain mutation members. `IEquipmentContainer` MUST compose an `IReadOnlyItemContainer Items` view and retain its equipment-slot indexer and semantic operations.

#### Scenario: Equipment exposes only a read-only composed view
- **WHEN** a caller accesses equipment through `IEquipmentContainer`
- **THEN** generic item reads and queries are available through `Items`, equipment-slot reads remain available through the `EquipmentSlot` indexer, and no generic mutation or transaction capability is exposed

#### Scenario: Equipment read view reflects its storage
- **WHEN** equipment contains an item
- **THEN** the read-only view reports the same capacity, slots, enumeration, ID lookup, counts, and containment as the equipment storage

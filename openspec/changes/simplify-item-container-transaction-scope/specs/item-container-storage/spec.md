## ADDED Requirements

### Requirement: Disposable atomic mutation scope
An item transaction MUST expose Begin, Commit, and Dispose, with every participant resolved and validated before locking. It MUST capture all snapshots before establishing originating-thread bindings. Construction failure MUST remove bindings, release all acquired locks, leave storage unchanged, and publish nothing. Dispose without commit MUST restore all captured references, counts, and revisions. Commit MUST declare already-mutated storage irreversible before dropping snapshots, removing bindings, releasing locks, and executing callbacks. Cleanup MUST attempt every owned resource; callbacks MUST NOT execute while a transaction lock remains intentionally held. Repeated owner-thread disposal MUST be inert and repeated commit MUST NOT retry publication.

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
Standalone helpers MUST create a transaction when none of their required storage belongs to a current-thread scope. Bindings owned by another thread MUST serialize through deterministic storage locks. If any required storage belongs to a current-thread scope, all required storage MUST belong to that same transaction. Partial enlistment, conflicting current-thread transactions, explicit nested Begin, and wrong-thread use of the same transaction MUST throw InvalidOperationException without acquiring missing locks or creating another transaction. Participant aliases MUST be deduplicated independently from first-seen publication order.

#### Scenario: Independent overlapping scopes contend
- **WHEN** another thread owns any required storage and the current thread owns none of it
- **THEN** Begin and standalone helpers wait on deterministic storage locks and succeed after commit or rollback releases them, without joining the foreign scope

#### Scenario: A helper requires missing storage
- **WHEN** an outer scope includes A but a helper requires A and B
- **THEN** the helper throws without locking or mutating B, and A remains bound to its original scope

#### Scenario: Storage belongs to different scopes
- **WHEN** required storage belongs to different active transactions
- **THEN** participation throws without mutation or nested scope creation

#### Scenario: A composite participant aliases storage
- **WHEN** an ordinary participant and a composite participant contribute the same storage
- **THEN** that storage is locked and snapshotted once without duplicating or reordering publication

### Requirement: Automatic ordered transaction completion
Commit MUST perform automatic completion after unlock without a separate caller publication call. Post-commit hooks MUST precede normal container publication and domain-owned batches MUST retain their existing failure policy. Container publishers MUST retain existing observable order independently of lock order; the first failure MUST skip later container publishers and all after-publication actions. Deferred after-publication actions MUST execute in registration order only after normal container publication, with storage irreversible and locks released. Their first failure MUST stop later actions. The transaction MUST store only actions; domains MUST own the captured facts and effects. A single failure MUST preserve its original exception; multiple hook failures and an independent publication failure MUST survive as original leaf exceptions in one flat AggregateException. Validation determining mutation eligibility MUST remain before commit.

#### Scenario: Container publication fails
- **WHEN** a container publisher throws
- **THEN** later containers and all after-publication actions are skipped and storage stays committed

#### Scenario: An after-publication action fails
- **WHEN** an after-publication action throws after all container publication
- **THEN** earlier actions remain observable, later actions are skipped and storage stays committed

#### Scenario: Equipment hooks and publication both fail
- **WHEN** an equipment-owned hook batch and normal publication both throw
- **THEN** required equipment hooks have been attempted and AggregateException retains both original failures

## RENAMED Requirements

- FROM: `### Requirement: Transaction commit and publication are explicit phases`
- TO: `### Requirement: Transaction commit includes publication`
- FROM: `### Requirement: Explicit economic publication preserves domain behavior`
- TO: `### Requirement: Automatic economic publication preserves domain behavior`

## MODIFIED Requirements

### Requirement: MoneyPouch separates gameplay and mutation capabilities
`IMoneyPouchContainer` MUST expose normal coin-domain operations. `MoneyPouch.Mutations` MUST provide opaque participation in an atomic mutation scope without public staging, snapshot, locking, or publication mechanics. The primary MoneyPouch API MUST NOT expose generic item-ID `Contains` or caller-managed publication receipts.

#### Scenario: MoneyPouch coin availability is domain-specific
- **WHEN** a caller checks whether a character has a positive coin amount
- **THEN** `HasCoins` compares the request against pouch coins and inventory coin item `995` using overflow-safe addition


### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST implement `IItemContainer` and privately compose one `ItemContainerStorage` and one `ItemContainerMutationBoundary`, exposed as `IItemContainerMutationBoundary Mutations`. Ordinary domain implementations privately own concrete `ItemContainer`; ordinary domain interfaces MUST expose `IItemContainer Items` and MUST NOT copy generic forwarding operations. Equipment and MoneyPouch MUST compose storage directly with private mutation boundaries and MUST NOT expose generic `Items` or raw storage boundaries. Equipment MUST itself remain a read-only `IContainer<IItem?>`. TradeOffer, Duel, and Price Checker MUST compose the concrete `ItemContainer` where generic behavior is required. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST NOT be part of its contract. Exact and multi-container mutations MUST use `ItemContainerTransaction` through opaque participants obtained from `.Mutations`. Domain callers MUST NOT cast containers to recover storage infrastructure. `ItemContainerStorage` MUST own low-level slot and mutation mechanics and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `ItemContainerTransaction` MUST own deterministic lock ordering, snapshots, rollback, changed-slot tracking, and post-commit publication. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST be available through a neutral generic operation. MoneyPouch MUST perform pouch and inventory mutations through the same scope, with its participant contributing both storage boundaries; it MUST NOT expose the concrete storage boundary or keep a separate rollback path. Concrete storage and mutation-boundary implementations SHOULD remain assembly-internal implementation details where friend-assembly access is sufficient.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts


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
- **THEN** scopes acquire store locks in the same stable order; an independent helper encountering a scope on another thread waits for its locks and proceeds after that scope unbinds and unlocks


### Requirement: Storage and trade revisions have distinct purposes
Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerTransaction` MUST own lock ordering; `ItemContainerMutationBoundary.TryTransferTo` MUST use a short-lived scope or participate in an existing complete scope. Trade session owners MUST establish scopes around TradeExchange operations rather than acquire storage locks directly. A trade offer's acceptance `Revision` MUST remain domain-owned and MUST advance according to its existing publication semantics, independently of storage revision.

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
`ItemContainerTransaction` MUST provide one disposable atomic scope around existing synchronous domain operations. Every participant MUST be supplied before Begin, and storage aliases MUST be deduplicated without changing first-seen publication order. It MUST own ordered locks, snapshots, rollback and deferred publication. Domain operations MUST retain item semantics; no public mutation methods, dynamic enlistment, separate unit of work, committed-state query, publication call, or ambient joining API may be required. Internal storage participation MUST reject incomplete or conflicting current-thread enlistment and wrong-thread use of the same scope. Async, nested and distributed transaction support MUST NOT be introduced.

#### Scenario: A transaction participant receives a staging context
- **WHEN** an existing domain operation runs inside an active scope containing all its required storage
- **THEN** its ordinary mutations participate without obtaining a public transaction context or acquiring additional locks


### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch exact additions and removals MUST mutate both pouch and inventory storage through the same active scope. Coin, slot-zero sentinel, balance, message, and event rules remain owned by MoneyPouch. Its captured notification facts MUST be owned by the scope and published automatically after commit and unlock, and MoneyPouch MUST NOT snapshot and restore pouch storage as a separate rollback mechanism.

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory mutations are staged but a later participant rejects its operation
- **THEN** transaction rollback restores both stores and no pouch message or event is published


### Requirement: Domain callbacks follow committed storage state
Equipment callbacks, trade settlement publication, inventory/bank/reward events, money-pouch messages, and UI updates MUST remain owned by their domain operations and MUST observe committed storage state.

#### Scenario: Equipment transfer runs callbacks before publication
- **WHEN** an equip or unequip transfer commits successfully
- **THEN** the required equipment callbacks run after both stores commit and before equipment and inventory updates publish

#### Scenario: Trade consumes generic container operations
- **WHEN** TradeExchange stages item mutations for settlement
- **THEN** it uses existing container operations under the session-owned scope's deterministic storage locks and publishes through domain owners only after commit


### Requirement: Transaction commit includes publication
A transaction MUST expose Begin, Commit, and Dispose as its public lifecycle. Dispose without commit MUST restore all enlisted storage and revisions; Commit MUST declare current storage irreversible, release bindings and locks, then perform domain hooks and automatic publication. Callers MUST NOT require a separate publication step or committed-state query. Each changed container MUST publish once in existing observable order, independent of lock order. Completion failures MUST leave committed storage irreversible and MUST NOT be retried by Commit or Dispose.

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
MoneyPouch MUST internally record successful notification amounts and previous counts for automatic publication after container publishers. Callers MUST NOT manage notification receipts. An inventory-only pouch addition MUST NOT publish a pouch effect. Equipment MUST own its ordered lifecycle/profile hook batch and retain attempt-all behavior after storage becomes irreversible and locks are released. Normal container publication MUST stop at its first failure, skipping later containers and all pouch publication; pouch publication MUST stop at its first failure. A single failure MUST preserve the original exception; independent hook and publication failures MUST preserve both original exceptions in AggregateException. Shop purchases MUST sort stock and send the purchase event only after Commit finishes publication.

#### Scenario: Pouch removal consumes inventory overflow
- **WHEN** a committed exact removal consumes pouch and inventory coins
- **THEN** the removal message retains the full requested amount and the pouch event retains its pre-operation count

#### Scenario: Equipment lifecycle fails after commit
- **WHEN** an equipment lifecycle effect throws after storage commits
- **THEN** later required lifecycle effects and transaction publication are still attempted under the equipment owner's existing cleanup policy and storage is not rolled back

#### Scenario: Shop publication fails after purchase commit
- **WHEN** container or pouch publication throws after a purchase commits
- **THEN** later stock sorting and the purchase event are skipped, storage remains committed, and the original publication exception propagates directly

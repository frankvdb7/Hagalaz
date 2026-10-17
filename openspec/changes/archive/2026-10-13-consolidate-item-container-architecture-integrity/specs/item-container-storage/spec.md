# Spec Delta

## MODIFIED Requirements

### Requirement: Equipment publishes only after lifecycle effects
Direct equipment restoration, replacement, full removal, and clearing MUST publish committed equipment state only after all required equipment lifecycle effects have been attempted. Storage MUST commit before lifecycle callbacks run. Clearing MUST remove all equipped items before callbacks and attempt `OnUnequipped` for every previously equipped item. An occupied non-weapon/non-shield replacement MUST obtain authorization before mutation, then perform its storage replacement atomically. Post-commit lifecycle or publication failures MUST NOT roll back committed storage. Every post-commit action MUST be attempted; one failure MUST preserve and rethrow the original exception, while multiple failures MUST be aggregated.

#### Scenario: Equipment replacement callbacks precede publication
- **WHEN** an authorized equipped item is replaced
- **THEN** storage contains the replacement before `OnUnequipped` and `OnEquipped`, and publication follows both callback attempts

#### Scenario: Approved non-weapon replacement is atomic
- **WHEN** an incoming item replaces an occupied non-weapon/non-shield equipment slot after approval
- **THEN** the incoming and displaced item instances are swapped atomically, lifecycle callbacks run after storage commit and unlock, and publication follows both callback attempts

#### Scenario: Deferred equipment approval leaves storage unchanged
- **WHEN** an occupied replacement requests UI approval
- **THEN** inventory and Equipment remain unchanged until acceptance, and the accepted continuation revalidates item identity and eligibility before mutation

#### Scenario: Deferred replacement does not mutate before approval
- **WHEN** replacement requires interactive approval
- **THEN** the incoming inventory item and equipped item remain unchanged until approval is accepted

#### Scenario: Interactive replacement rejects an existing local scope
- **WHEN** the current thread already owns Inventory or Equipment while an occupied replacement is requested
- **THEN** the command rejects before mutation and does not retain a scope across the approval interaction

#### Scenario: Full equipment removal callback precedes publication
- **WHEN** an equipped item is fully removed
- **THEN** storage no longer contains it before `OnUnequipped`, and publication follows the callback attempt

#### Scenario: Equipment clear exhausts callbacks after storage clear
- **WHEN** equipment containing one or more items is cleared
- **THEN** storage is empty before callbacks, every prior item's `OnUnequipped` is attempted, and publication follows all callback attempts

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
- **THEN** Begin throws without waiting and releases any additionally acquired locks

#### Scenario: Nested disjoint transaction remains available
- **WHEN** a thread already owns one scope and begins a transaction over disjoint, immediately available boundaries
- **THEN** the nested scope acquires them without waiting and can complete independently

#### Scenario: Nested Begin on an enlisted boundary is rejected
- **WHEN** the current thread begins a transaction for storage already enlisted in its active transaction
- **THEN** Begin throws and leaves the existing scope unchanged

### Requirement: Mutation authorization and standalone publication belong to the boundary
Each mutation boundary MUST authorize synchronous operations against its storage. A per-operation scope MUST acquire the boundary lock for standalone work or borrow the lock held by its active transaction, and release only a lock it acquired. A manually held lock without a transaction binding MUST NOT authorize mutation. Change attribution MUST occur under the same lock and transaction binding, and MUST reject a boundary outside the active transaction or an unlocked boundary. A scope with no changes MUST publish nothing. Transaction membership is determined from the boundary binding; ambient current-transaction state is prohibited.

An ordinary standalone mutation MUST count as storage ownership from physical lock acquisition through lock release. If it records changes, retained publication ownership MUST begin before the physical lock is released and continue through lifecycle effects and publication, while callbacks run without the lock. Same-thread overlapping use MUST be rejected. When the current thread owns no item-container scope, ordinary mutations and overlapping Begin operations MUST wait for foreign transaction or publication ownership to clear. When the current thread already owns any item-container scope, additional boundary acquisition MUST be nonblocking: it MAY proceed when the lock and logical ownership are immediately available, but MUST throw rather than wait when either is contended or foreign-owned. Multi-boundary acquisition MUST use deterministic order, release the entire acquired prefix before waiting, and retry from the beginning. Standalone cleanup MUST clear ownership and pulse waiters under the boundary lock, including after publisher failure.

#### Scenario: Standalone mutation owns storage while its algorithm runs
- **WHEN** an ordinary standalone mutation algorithm is executing under its boundary lock
- **THEN** the current thread is recorded as owning item-container storage until that lock is released

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

## ADDED Requirements

### Requirement: Storage stacking checks remain storage-safe
Item stacking checks invoked during a container mutation MUST be deterministic decisions over the supplied item data. They MUST NOT mutate unrelated containers, begin transactions, send gameplay events, or block on unrelated resources.

#### Scenario: A stacking check attempts contended storage access
- **WHEN** a stacking check attempts to acquire another boundary while its caller owns storage
- **THEN** the additional operation fails without waiting, deadlocking, or partially changing either container

### Requirement: Partial removal respects the requested total
`Remove(item, ...)` MUST remove no more than the requested total quantity across all matching stacks and return the actual removed quantity. `TryRemoveExact` MUST remain all-or-nothing, and operations that require a complete selected item MUST use exact removal.

#### Scenario: Removal spans matching stacks
- **WHEN** matching stacks together contain more than the requested quantity
- **THEN** removal consumes the requested total and leaves all remaining quantity intact

#### Scenario: Removal has insufficient total quantity
- **WHEN** matching stacks contain less than the requested quantity
- **THEN** partial removal returns the available quantity without removing more than exists

#### Scenario: Exact removal cannot satisfy the request
- **WHEN** matching stacks contain less than the requested exact quantity
- **THEN** exact removal returns false and leaves all storage unchanged

#### Scenario: Failed ground drop does not create a ground item
- **WHEN** an exact removal of the selected inventory item fails
- **THEN** no ground item is created and inventory contents remain unchanged

### Requirement: Shop normalization uses immutable original quantities
Shop normalization MUST compare current stock with original quantities keyed by item ID, without mutating shared live item instances while planning. A failed plan MUST leave storage unchanged; a successful changed plan MUST publish one coherent mutation and preserve unchanged item instances.

#### Scenario: Original stock moves to another slot
- **WHEN** an original stock item is in a different slot than at construction
- **THEN** normalization uses its original item-ID quantity baseline

#### Scenario: Player stock extends beyond original stock entries
- **WHEN** player-added stock occupies a slot beyond the number of original entries
- **THEN** normalization processes it without indexing beyond original metadata

#### Scenario: Planning fails before state replacement
- **WHEN** cloning or planning fails during normalization
- **THEN** live item counts, item references, slots, and storage revision remain unchanged

#### Scenario: Normalization changes only selected stock instances
- **WHEN** normalization changes some stock counts
- **THEN** changed items are replaced by planned copies, unchanged items retain identity, and one publication describes the final state

### Requirement: No-op sorting does not mutate storage
Sorting an already compact container MUST leave its storage revision unchanged and produce no publication. A sort that moves an item MUST advance revision and publish the affected container state.

#### Scenario: Sort has no empty slots
- **WHEN** every item already occupies the compact prefix of the container
- **THEN** sorting preserves order and references without changing revision or publishing

#### Scenario: Sort compacts sparse slots
- **WHEN** sorting moves an item across an empty slot
- **THEN** it compacts items in their existing order, advances revision once, and publishes the change

### Requirement: Decision-critical reads are revalidated under ownership
A preflight read that determines whether an item mutation is valid MUST be checked again after all required mutation boundaries are acquired. Informational reads MAY remain unlocked, and the system MUST NOT introduce a global read lock.

#### Scenario: Source changes after preflight
- **WHEN** the selected source instance or quantity changes between preflight and mutation ownership
- **THEN** the operation rejects without consuming a different or stale item

#### Scenario: Equipment source changes during eligibility preflight
- **WHEN** the selected equipment item is replaced with an equivalent instance after its inventory slot was read
- **THEN** the equip operation leaves the replacement in inventory and does not equip either item

#### Scenario: Destination changes after preflight
- **WHEN** capacity or the target slot changes before mutation ownership is acquired
- **THEN** the operation rechecks the destination and either performs the complete operation or leaves every participant unchanged

### Requirement: Equipment replacement is authorized and atomic
Before mutation, `EquipmentContainer` MUST require `CanEquipItem` and `CanUnEquipItem` to succeed for the relevant items. Rejection MUST leave equipment and inventory unchanged and invoke no lifecycle callback. For an active aura replacement, `AuraEquipmentScript.EquipItem` MUST prompt before delegating to `EquipmentScript.EquipItem`, without holding storage locks or a transaction while waiting. Acceptance MUST continue only if the same aura instance remains equipped. The resulting `EquipmentContainer` operation MUST revalidate the exact incoming inventory instance, equipped instance, and target slot under one transaction. Repeated or concurrent delivery MUST NOT duplicate an item. The transaction MUST remove the exact incoming instance, place the displaced item in the vacated inventory slot, and equip the incoming instance. A rejected or failed storage step MUST roll back both containers without callbacks or publication. This path MUST NOT invoke the old `UnEquipItem` command or recover items through Rewards or Bank.

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
- **WHEN** a storage operation throws after the incoming item has been removed inside the transaction
- **THEN** rollback restores both original item instances and no lifecycle callback or publication occurs

## REMOVED Requirements

### Requirement: Equipment replacement preserves command behavior outside weapon and shield conflicts
**Reason**: The old custom `UnEquipItem` command path is replaced by an aura-specific `EquipItem` prompt and atomic item-container storage mutation.
**Migration**: Aura scripts prompt before delegating to the existing Equipment operation, which mutates inventory and Equipment in one transaction.

### Requirement: Interactive Equipment replacement preserves the incoming item on pre-equip failure
**Reason**: The sequential remove/command/insert workflow and recovery destinations are replaced by an Equipment script interaction followed by one atomic transaction. Compensation to Inventory, Rewards, or Bank is no longer part of normal Equipment behavior.
**Migration**: Use `EquipmentScript.EquipItem` for script-owned interaction and the normal atomic Equipment replacement operation; callers must not depend on recovery storage for failed pre-commit replacements.

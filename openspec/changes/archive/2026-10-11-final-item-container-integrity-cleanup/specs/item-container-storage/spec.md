# Spec Delta

## MODIFIED Requirements

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

#### Scenario: Nested Begin on an enlisted boundary is rejected
- **WHEN** the current thread begins a transaction for storage already enlisted in its active transaction
- **THEN** Begin throws and leaves the existing scope unchanged

## ADDED Requirements

### Requirement: Interactive Equipment replacement preserves the incoming item on pre-equip failure
An interactive occupied non-weapon/non-shield replacement MUST retain the incoming item if the custom unequip rejects or fails, the destination slot remains unavailable, insertion fails before equip, or inventory-removal publication fails after removal committed. Recovery MUST preserve item identity and try the original inventory slot, ordinary inventory insertion, Rewards storage, then Bank storage without overwriting or duplicating the item. If all existing destinations reject it, the operation MUST throw an explicit failure. Once the item is equipped, later lifecycle or publication failures MUST NOT restore it to inventory.

#### Scenario: Interactive replacement fails before equipping
- **WHEN** custom unequip returns false or equipment insertion rejects the incoming item
- **THEN** the same item instance remains accessible through the first accepting recovery destination and is not duplicated or overwritten

#### Scenario: Inventory removal publisher fails after committing removal
- **WHEN** the inventory publisher throws after exact removal has changed storage
- **THEN** the incoming item is restored before the original publisher exception propagates

#### Scenario: No existing recovery destination can accept the item
- **WHEN** inventory, Rewards, and Bank reject restoration after a pre-equip failure
- **THEN** an explicit recovery failure is thrown rather than reporting an ordinary rejected equip

### Requirement: Clearing empty item storage is a no-op
Clearing storage with no non-null items MUST leave its slots and mutation revision unchanged and MUST produce no container publication. A non-null zero-count item sentinel MUST count as occupied and follow the existing clear behavior.

#### Scenario: Empty storage clear does not change storage or publish
- **WHEN** an empty or zero-capacity item container is cleared, including inside a committed transaction
- **THEN** storage revision and contents remain unchanged and no container change is published

#### Scenario: Non-null zero-count sentinel is cleared
- **WHEN** storage containing a non-null zero-count sentinel is cleared
- **THEN** the sentinel is removed and the storage revision advances as for other nonempty storage

## MODIFIED Requirements

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

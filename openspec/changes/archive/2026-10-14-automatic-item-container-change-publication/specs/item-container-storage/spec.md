# Spec Delta

## MODIFIED Requirements

### Requirement: Domain-owned containers control mutation publication
Ordinary domain containers MUST expose item reads through `IReadOnlyItemContainer`; Equipment MUST expose a read-only item view and transaction participation without generic mutation. Writes MUST use the owning domain's existing or narrowly required mutation commands. Registered domain owners MUST automatically complete changed batches after a successful outermost standalone mutation, standalone transfer, or transaction commit. Completion MUST run after physical locks are released and before logical ownership is released; logical ownership prevents interleaving while domain callbacks run without holding mutation locks. Completion remains synchronous; game-tick batching is deferred until measured update volume justifies it. Raw ownerless `ItemContainer` instances MUST retain manually consumable tracker changes.

#### Scenario: Domain item views cannot bypass their owner
- **WHEN** a consumer accesses Inventory, Bank, Reward, Familiar Inventory, or Shop Stock items through its public interface
- **THEN** it can query the items but cannot invoke generic storage mutations

#### Scenario: Equipment exposes a read-only item view
- **WHEN** a caller accesses equipment through `IEquipmentContainer`
- **THEN** it can query its items and participate in a transaction, but cannot invoke generic storage mutations through the item view

#### Scenario: Standalone domain mutation publishes committed changes
- **WHEN** an outermost standalone mutation changes a registered domain container
- **THEN** its owner completes the committed batch once after the storage mutation lock is released and before logical ownership is released

#### Scenario: Standalone publication does not require rollback state
- **WHEN** an ordinary single-container domain mutation succeeds outside a caller-owned transaction
- **THEN** it retains boundary ownership through domain publication without capturing a rollback snapshot

#### Scenario: Multi-owner transaction completes before releasing ownership
- **WHEN** a caller commits mutations to multiple domain containers
- **THEN** the transaction invokes each changed owner once in participant order before disposal, and foreign mutations wait until disposal

#### Scenario: Nested mutations complete as one batch
- **WHEN** nested mutation scopes record changes for one registered owner
- **THEN** the outermost scope completes one merged batch after all nested scopes end

#### Scenario: Ownerless item containers remain pull-based
- **WHEN** a raw ownerless `ItemContainer` changes outside a transaction
- **THEN** no domain event is emitted and its owner can consume the committed tracker batch manually

#### Scenario: Rollback does not publish domain changes
- **WHEN** a domain mutation participates in a transaction that is disposed without Commit
- **THEN** storage and tracker state are restored or discarded without a domain notification

### Requirement: Cross-container transfer completes registered owners
`IItemContainer.TryTransferTo(...)` MUST use one atomic storage transfer under deterministic boundary locking. It MUST participate in a shared active transaction when both boundaries are enlisted and otherwise perform an atomic standalone transfer without creating a transaction. Success MUST record committed changes for both trackers once; rejection MUST leave both stores and trackers unchanged. A standalone transfer MUST retain logical ownership of both boundaries, release both physical locks, then complete each distinct registered owner in source-then-destination order. Ownerless trackers MUST remain manually consumable.

#### Scenario: Standalone transfer completes registered owners
- **WHEN** an exact transfer succeeds between two containers with registered owners outside a transaction
- **THEN** both owners complete once after both physical locks are released and before either logical ownership claim is released

#### Scenario: Standalone transfer with an ownerless side
- **WHEN** an exact standalone transfer succeeds and only one boundary has a registered owner
- **THEN** that owner completes while both boundaries remain logically owned and the ownerless side retains its tracker changes

#### Scenario: Ownerless transfer remains pull-based
- **WHEN** an exact transfer succeeds between two raw item containers outside a transaction
- **THEN** both storages change atomically and each tracker records its changed slots without invoking external code

#### Scenario: Transactional transfer rolls back without publication
- **WHEN** an exact transfer succeeds in a shared transaction that is disposed without Commit
- **THEN** both storages and provisional tracker changes are restored

#### Scenario: Failed transfer leaves both trackers unchanged
- **WHEN** the source lacks the requested amount or the destination cannot accept it
- **THEN** the operation returns false without changing either storage or tracker

### Requirement: Transaction construction and commit preserve storage state
`ItemContainerTransaction` MUST resolve and validate all participants before locking, deduplicate repeated boundaries without changing participant encounter order, acquire boundaries in deterministic lock order, capture snapshots, and establish bindings only after construction succeeds. Construction failure MUST release acquired locks without changing storage or publishing. Dispose without Commit MUST restore snapshots and discard provisional tracker changes. Commit MUST make storage irreversible, promote all participant changes, release physical locks, then run each distinct owner's completion-effects phase followed by its publication phase while transaction bindings remain until Dispose. Completion failures MUST NOT roll back committed storage or be retried.

#### Scenario: Construction fails during snapshot capture
- **WHEN** snapshot capture throws after locks have been acquired
- **THEN** all acquired locks are released, no binding survives, and storage and publication remain unchanged

#### Scenario: Scope exits without commit
- **WHEN** an early return, exception, or cancellation exits an active scope
- **THEN** every participant is restored while its lock and binding are held, provisional changes are discarded, and no domain effect is published

#### Scenario: Commit completes all storage before invoking owners
- **WHEN** a transaction commits changes for multiple registered owners
- **THEN** every participant's tracker changes are promoted and physical locks released before any owner callback runs

#### Scenario: Commit automatically completes registered owners
- **WHEN** all mutations succeed and `Commit()` returns
- **THEN** tracker changes are promoted, physical locks are released, and each registered owner has completed before the transaction binding is released by `Dispose`

#### Scenario: Commit invokes each owner once in separate phases
- **WHEN** a transaction commits with one or more registered owners
- **THEN** each reference-distinct owner receives one effects-phase invocation followed by one publication-phase invocation in participant order

#### Scenario: Commit failure does not reverse storage or retry completion
- **WHEN** an owner callback throws after the transaction becomes irreversible
- **THEN** committed storage remains changed, disposal does not roll it back, and another Commit cannot replay completion

#### Scenario: Foreign operations wait through committed owner completion
- **WHEN** an owner completes changes after Commit but before transaction disposal
- **THEN** foreign mutations and tracker reads wait until the scope releases its bindings

### Requirement: Mutation boundaries own synchronization and automatic completion
Mutation boundaries MUST authorize synchronous mutations, transaction participation, deterministic locking, and tracker access. They MUST coordinate the optional domain completion owner without storing generic publication delegates. Standalone raw-container operations MUST record committed changes directly in the tracker; transaction mutations MUST remain provisional until Commit. Tracker reads MUST obey boundary ownership and MUST NOT expose provisional changes.

#### Scenario: Completion runs without a physical mutation lock
- **WHEN** a registered owner completes a standalone mutation or committed transaction
- **THEN** the boundary's physical mutation lock is not held while logical ownership remains active

#### Scenario: Failed and no-op mutations do not complete
- **WHEN** a mutation rejects its operation or records no changed slots
- **THEN** no owner completion or publication occurs

#### Scenario: Transaction owner cannot read provisional changes
- **WHEN** the owning thread calls `GetSlots()` during an active transaction
- **THEN** the call is rejected until Commit; after Commit the owner can consume the committed batch before Dispose

#### Scenario: Raw container change batch is consumed once
- **WHEN** a raw container mutation commits and its owner calls `ChangeTracker.GetSlots()`
- **THEN** the returned batch contains the changes and a later retrieval is empty unless another mutation commits

#### Scenario: Transaction owner consumes after commit only
- **WHEN** the owning thread calls `GetSlots()` during an active transaction
- **THEN** the call is rejected until Commit; after Commit it can consume the committed batch before Dispose

#### Scenario: Foreign tracker reader waits through owner completion
- **WHEN** a committed transaction retains a domain binding while the owner publishes
- **THEN** a foreign tracker reader waits until the scope is disposed and then sees only unconsumed committed changes

### Requirement: Equipment and MoneyPouch own specialized publication
Equipment MUST run its lifecycle effects before registered containers publish committed changes. MoneyPouch MUST retain FIFO immutable change facts and publish its messages/events after ordinary container owners affected by the same operation. Both owners MUST discard provisional facts on rollback. Post-commit failures MUST leave storage irreversible and attempted batches MUST NOT be replayed.

#### Scenario: Equipment effects precede its event
- **WHEN** a committed Equipment mutation has lifecycle effects
- **THEN** all required effects are attempted before the Equipment event is sent

#### Scenario: Equipment effects precede inventory and equipment publication
- **WHEN** a committed Equipment mutation has lifecycle effects and changes Inventory
- **THEN** all required effects are attempted before Inventory publication, which precedes Equipment publication

#### Scenario: MoneyPouch publishes captured facts after container events
- **WHEN** a committed operation changes both a pouch and an ordinary item container
- **THEN** the ordinary container event precedes the MoneyPouch message/event, which uses its captured counts and change amount

#### Scenario: MoneyPouch publishes captured facts after inventory events
- **WHEN** a committed operation changes a pouch and its inventory
- **THEN** the inventory event precedes the MoneyPouch message/event, which uses its captured counts and change amount

#### Scenario: Rollback discards specialized facts
- **WHEN** Equipment or MoneyPouch changes are staged in a transaction that is disposed without Commit
- **THEN** no lifecycle effect, pouch message, or domain event is emitted

#### Scenario: Equipment hook failure does not roll back storage
- **WHEN** an Equipment lifecycle hook throws after Commit
- **THEN** Equipment attempts its required lifecycle effects, committed storage remains changed, and the failure propagates without retry

### Requirement: Committed item-container changes are consumed by domain owners
Each mutation boundary MUST own one change tracker. Concrete `ItemContainer.ChangeTracker` MUST expose that same tracker. The tracker MUST expose committed changes only, report whether unconsumed changes exist, atomically consume each retrieved batch, and preserve full-container changes separately from empty batches. A full-container change is represented by `null`; no changes are represented by an empty `HashSet<int>`. Registered owners MUST consume committed batches during automatic completion; ownerless containers MUST retain explicit pull-based consumption. Generic storage MUST NOT publish domain events.

#### Scenario: Standalone changes are accumulated and consumed
- **WHEN** standalone mutations change one or more slots of an ownerless container and its owner retrieves the tracker batch
- **THEN** the tracker returns the union of changed slots, and a later retrieval returns an empty set unless another mutation committed

#### Scenario: Full-container change dominates slot changes
- **WHEN** a full-container change is recorded with partial changes before or after it
- **THEN** the next retrieval returns `null` for the full container and subsequent retrieval returns an empty set

#### Scenario: Transaction changes are committed or discarded
- **WHEN** a transaction mutates a boundary and commits or rolls back
- **THEN** commit makes its changes consumable to the owner while rollback discards only provisional changes and preserves earlier unconsumed committed changes

#### Scenario: Explicitly suppressed mutation is not tracked
- **WHEN** an operation is invoked with `publishChanges: false`
- **THEN** it changes storage without adding changed slots to a later tracker batch

#### Scenario: Registered owner completion consumes tracked changes once
- **WHEN** a registered owner publishes a committed batch successfully or its publisher throws
- **THEN** the batch is consumed once and a later retrieval does not replay it

#### Scenario: Ownerless containers retain pull-based tracking
- **WHEN** a raw ownerless container commits a mutation
- **THEN** no domain event is emitted and its owner may consume the committed batch through `ChangeTracker.GetSlots()`

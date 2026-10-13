# Spec Delta

## REMOVED Requirements

### Requirement: Containers compose one authoritative item store
**Reason**: This requirement couples generic storage composition to mutable domain item views and automatic publication, both of which are replaced by domain-owned commands and ChangeTracker consumption.
**Migration**: The new domain-owned publication and transaction lifecycle requirements preserve the one-store, encapsulation, and participation rules while specifying read-only domain views.

### Requirement: Cross-container transfers commit both stores atomically
**Reason**: The existing requirement makes a low-level transfer invoke generic publishers, which conflicts with callback-free storage and owner-controlled publication.
**Migration**: The replacement keeps the atomic storage transfer and tracker attribution; affected domain owners publish after commit.

### Requirement: Transaction construction and rollback preserve storage state
**Reason**: The existing lifecycle completes publication inside Commit and releases bindings before Dispose, which no longer matches explicit domain publication.
**Migration**: The replacement keeps rollback-on-dispose and the irreversible Commit point, while retaining bindings until scope disposal after owner publication.

### Requirement: Commit completes domain effects and publication in order
**Reason**: Container event publication is no longer transaction infrastructure. Domain owners invoke their own publication after Commit.
**Migration**: Equipment and MoneyPouch keep their domain-owned completion semantics; operation coordinators attempt participating owners in their established order before disposing the transaction.

### Requirement: Mutation authorization and standalone publication belong to the boundary
**Reason**: A generic boundary no longer invokes publication or retains ownership for a hidden publisher callback.
**Migration**: The boundary continues to own synchronization and transaction binding. Domain commands retain the committed binding through explicit publication; raw containers expose tracker batches for their owner to consume.

### Requirement: Automatic economic publication preserves domain behavior
**Reason**: The requirement describes automatic transaction-driven container publication.
**Migration**: Domain owners explicitly publish their typed events and preserve Equipment and MoneyPouch ordering and failure policy.

### Requirement: Read-only item access is an explicit capability
**Reason**: The requirement covers Equipment alone and states no rule for ordinary domain containers.
**Migration**: The new domain item view requirement covers all domain-owned storage and retains Equipment's read-only contract.

## ADDED Requirements

### Requirement: Domain-owned containers control mutation publication
Ordinary domain containers MUST expose item reads through `IReadOnlyItemContainer`. Equipment MUST expose a read-only item view and transaction participation without generic mutation. Writes MUST use the owning domain's existing or narrowly required mutation commands. A standalone command MUST publish its committed tracker batch after releasing physical locks and before releasing its logical transaction binding. A caller-owned transaction MUST publish each affected domain through that owner after Commit and before disposing the transaction. Raw `ItemContainer` remains directly mutable and publishes no domain events; its owner consumes `ChangeTracker` explicitly.

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
- **THEN** storage and tracker state are restored/discarded without a domain notification

### Requirement: Commit separates irreversible storage from domain publication
`ItemContainerTransaction.Commit()` MUST promote provisional tracker changes, make storage irreversible, and release physical mutation locks without invoking domain publishers. It MUST retain transaction bindings until `Dispose()` so domain owners can consume committed batches on the originating thread without an overlapping mutation. Mutations after Commit MUST be rejected. Dispose before Commit MUST roll back; Dispose after Commit MUST only clear pending completion facts and release ownership. Publication failures MUST NOT roll back storage, retry a consumed batch, or leave a binding behind.

#### Scenario: Committed owner consumes changes while scope remains bound
- **WHEN** a domain owner consumes its tracker after Commit and before transaction disposal
- **THEN** it receives the committed changes while foreign operations remain blocked

#### Scenario: Publication failure still releases committed scope
- **WHEN** a domain publisher throws after Commit
- **THEN** storage remains committed, the batch is not replayed, and scope disposal releases every binding

#### Scenario: Dispose without commit restores all participants
- **WHEN** a transaction exits before Commit
- **THEN** every participant is restored and no committed tracker batch is published

### Requirement: Mutation boundaries own synchronization without publishing
Mutation boundaries MUST authorize synchronous mutations, transaction participation, deterministic locking, and tracker access. They MUST NOT store or invoke generic publication delegates. Standalone raw-container operations MUST record committed changes for later tracker consumption; transaction mutations MUST remain provisional until Commit. Consuming a batch MUST happen under the boundary synchronization rules and MUST never expose provisional changes.

#### Scenario: Raw container change batch is consumed once
- **WHEN** a raw container mutation commits and its owner calls `ChangeTracker.GetSlots()`
- **THEN** the returned batch contains the changes and a later read is empty unless another mutation commits

#### Scenario: Transaction owner consumes after commit only
- **WHEN** the owning thread calls `GetSlots()` during an active transaction
- **THEN** the call is rejected until Commit; after Commit it can consume the committed batch before disposal

#### Scenario: Foreign tracker reader waits through owner publication
- **WHEN** a committed transaction retains a domain binding while the owner publishes
- **THEN** a foreign tracker reader waits until the scope is disposed and then sees only unconsumed committed changes

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

### Requirement: Equipment and MoneyPouch own specialized publication
Equipment MUST run its owner-local lifecycle effects before consuming and publishing committed Equipment changes. MoneyPouch MUST retain FIFO immutable change facts and publish its messages/events after other domain containers affected by the same operation. Both owners MUST discard pending facts on rollback. Post-commit failures MUST leave storage irreversible, consume each attempted batch once, and release transaction ownership.

#### Scenario: Equipment effects precede its event
- **WHEN** a committed Equipment mutation has lifecycle effects
- **THEN** all required effects are attempted before the Equipment event is sent

#### Scenario: MoneyPouch publishes captured facts after container events
- **WHEN** a committed operation changes both a pouch and an ordinary item container
- **THEN** the ordinary container event precedes the MoneyPouch message/event, which uses its captured counts and change amount

#### Scenario: Equipment hook failure does not roll back storage
- **WHEN** an Equipment lifecycle hook throws after Commit
- **THEN** required hooks and the Equipment event follow Equipment's attempt-all policy, storage remains committed, and the failure propagates after cleanup

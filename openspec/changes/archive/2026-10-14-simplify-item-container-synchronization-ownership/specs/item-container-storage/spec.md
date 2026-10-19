# Spec Delta

## REMOVED Requirements

### Requirement: Mutation boundaries own synchronization and automatic completion

**Reason**: The requirement names a combined implementation type that is being removed. Access authorization, write lifetime, tracker state, and domain completion remain required but are assigned to their existing or newly focused owners.

**Migration**: Use one per-storage synchronization authority, one write scope per operation, the transaction for multi-storage lifecycle, and the domain owner for completion.

## MODIFIED Requirements

### Requirement: Per-storage synchronization owns access lifecycle

Each storage MUST have exactly one synchronization owner that authorizes synchronous access and coordinates physical locking and logical ownership. Storage algorithms MUST remain synchronization-agnostic. Standalone and transactional domain completion MUST run without physical mutation locks while logical ownership still prevents foreign mutation and tracker reads.

#### Scenario: Completion runs without a physical mutation lock
- **WHEN** a registered owner completes a standalone mutation or committed transaction
- **THEN** the synchronization owner's physical mutation lock is not held while logical ownership remains active

#### Scenario: Nested writes complete as one batch
- **WHEN** nested write scopes record changes for one registered owner
- **THEN** the outermost scope completes one merged batch after all nested scopes end

#### Scenario: Foreign tracker reader waits through owner completion
- **WHEN** a committed transaction retains a domain binding while the owner completes
- **THEN** a foreign tracker reader waits until the scope is disposed and then sees only unconsumed committed changes

#### Scenario: Failed and no-op mutations do not complete
- **WHEN** a mutation rejects its operation or records no changed slots
- **THEN** no owner completion or publication occurs

#### Scenario: Raw container change batch is consumed once
- **WHEN** a raw container mutation commits and its owner calls `ChangeTracker.GetSlots()`
- **THEN** the returned batch contains the changes and a later retrieval is empty unless another mutation commits

#### Scenario: Transaction owner consumes after commit only
- **WHEN** the owning thread calls `GetSlots()` during an active transaction
- **THEN** the call is rejected until Commit; after Commit it can consume the committed batch before Dispose

### Requirement: Storage is synchronization-agnostic

`ItemContainerStorage` MUST own item state, mutation revision, mutation algorithms, snapshots, and restoration only. It MUST NOT own synchronization or transaction state. Its per-storage synchronization owner provides synchronous authorization and protection. Transactions MUST coordinate synchronization owners and access storage only for the item-state data those owners protect.

#### Scenario: Storage mutation algorithms do not acquire synchronization
- **WHEN** an internal storage algorithm mutates item state
- **THEN** it performs item-state validation and mutation while its synchronization owner provides authorization and protection

#### Scenario: Storage mutation invalidates enumeration
- **WHEN** storage changes after an enumerator is created
- **THEN** the enumerator detects the storage revision mismatch

#### Scenario: Empty bulk addition is not a mutation
- **GIVEN** an item container and an enumerator created before the operation
- **WHEN** `TryAddRange` receives no effective items, including an empty range or a range containing only null entries
- **THEN** the operation succeeds with no changed slots
- **AND** storage contents and mutation revision remain unchanged
- **AND** the existing enumerator remains valid
- **AND** no participant publishes a committed mutation or runs a committed domain effect

### Requirement: Cross-container transfer completes registered owners

`IItemContainer.TryTransferTo(...)` MUST use one atomic storage transfer under deterministic synchronization locking. It MUST participate in a shared active transaction when both synchronization owners are enlisted and otherwise perform an atomic standalone transfer without creating a transaction. Success MUST record changes for both trackers once; rejection MUST leave both stores and trackers unchanged. The storage algorithm MUST NOT invoke publishers or external callbacks. For a standalone transfer, the shared transfer operation completes registered owners after both locks are released, in source-then-destination order; for a transactional transfer, `Commit()` completes them in resolved-synchronization encounter order.

#### Scenario: Opposite-direction standalone transfers use one lock order
- **WHEN** two threads request opposite-direction transfers between the same storages
- **THEN** both acquire synchronization owners in the same deterministic order without deadlock or partial mutation

#### Scenario: Transfer joins a complete transaction
- **WHEN** both storages belong to the same active transaction
- **THEN** the transfer participates without creating another transaction and its changes remain provisional until commit

#### Scenario: Transfer rejects partial transaction participation
- **WHEN** only one required storage belongs to the current thread's active transaction
- **THEN** the operation throws before acquiring or mutating the missing storage

#### Scenario: Standalone transfer completes registered owners
- **WHEN** an exact transfer succeeds between domain containers outside a transaction
- **THEN** both trackers record their changes and each registered owner completes once, after both physical locks are released and before either logical ownership claim is released

#### Scenario: Standalone transfer with an ownerless side
- **WHEN** an exact transfer succeeds and only one boundary has a registered owner
- **THEN** that owner completes while both boundaries remain logically owned, and the ownerless side retains its tracker changes

#### Scenario: Ownerless transfer remains pull-based
- **WHEN** an exact transfer succeeds between raw ownerless containers outside a transaction
- **THEN** both trackers record their changed slots without invoking domain completion

#### Scenario: Transactional transfer rolls back without publication
- **WHEN** an exact transfer succeeds in a shared transaction that is disposed without Commit
- **THEN** both storages and provisional tracker changes are restored

#### Scenario: Failed transfer leaves both trackers unchanged
- **WHEN** the source lacks the requested amount or the destination cannot accept it
- **THEN** the operation returns false without changing either storage or tracker

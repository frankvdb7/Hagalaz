## MODIFIED Requirements

### Requirement: Complete same-scope participation
Transaction membership MUST be established explicitly by supplying every storage participant to `ItemContainerTransaction.Begin(...)` before locks are acquired. Once storage is enlisted, its ordinary `IItemContainer` mutations MUST participate in that transaction: mutations update storage under the active scope and change publication is deferred until commit. Mutations against storage not supplied to `Begin(...)` MUST remain standalone and MUST NOT be dynamically enlisted. Ordinary mutation methods MUST NOT create, commit, or dynamically join a transaction. Bound storage MUST record changes through its active transaction, while unbound storage MUST publish immediately. `.Mutations` MUST expose the opaque transaction participant and atomic cross-container `TryTransferTo` operation; it MUST NOT duplicate ordinary add, range-add, remove, sort, or clear methods.

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
- **WHEN** a transaction begins with the MoneyPouch `Mutations` participant
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

#### Scenario: Enlisted ordinary mutation publishes after commit
- **GIVEN** a transaction begins with a container's mutation participant
- **WHEN** the container is changed through its ordinary mutation API
- **THEN** storage changes immediately and publication is deferred
- **AND** commit publishes the change once after transaction locks are released

#### Scenario: Enlisted ordinary mutation rolls back on disposal
- **GIVEN** a transaction begins with a container's mutation participant
- **WHEN** the container is changed through its ordinary mutation API and the transaction is disposed without commit
- **THEN** storage is restored to its snapshot and no change is published

#### Scenario: Unenlisted mutation remains outside another transaction
- **GIVEN** A and B are independent containers
- **AND** a transaction begins with only A's mutation participant
- **WHEN** A and B are mutated and the transaction is disposed without commit
- **THEN** A is restored and publishes nothing
- **AND** B retains its mutation and publishes normally

### Requirement: MoneyPouch separates gameplay operations from opaque participation
`IMoneyPouchContainer.Mutations` MUST expose an opaque `IItemContainerTransactionParticipant`. The MoneyPouch participant MUST contribute pouch storage and every required inventory storage. `TryAddExact` and `TryRemoveExact` MUST own a transaction when none of their required storage is bound; when all required storage belongs to the same active current-thread transaction they MUST participate in that scope; partial or conflicting enlistment MUST fail before mutation. Exact pouch methods MUST remain on `IMoneyPouchContainer`, not its opaque participant.

#### Scenario: MoneyPouch coin availability is domain-specific
- **WHEN** a caller checks whether a character has a positive coin amount
- **THEN** `HasCoins` compares the request against pouch coins and inventory coin item `995` using overflow-safe addition

#### Scenario: Exact MoneyPouch operation participates in complete scope
- **GIVEN** a transaction includes the pouch and all inventory storage contributions
- **WHEN** a public MoneyPouch exact operation is called
- **THEN** it changes enlisted storage without creating or committing another transaction

#### Scenario: Exact MoneyPouch operation rejects partial enlistment
- **GIVEN** only some pouch-required storage is enlisted
- **WHEN** a public MoneyPouch exact operation is called
- **THEN** it throws before changing pouch or inventory state

### Requirement: Equipment exposes semantic mutation operations
Equipment storage mutations performed by `TryRestoreEquippedItem`, `TryReplaceEquippedItem`, `RemoveEquippedItem`, and `ClearEquipment` MUST participate when Equipment storage is already enlisted. Typed lifecycle completion MUST be deferred through the owning transaction for enlisted storage; otherwise lifecycle completion and publication occur immediately. Operations without lifecycle effects MUST use normal change publication, which defers automatically when enlisted.

#### Scenario: Stale equipment replacement is rejected
- **WHEN** the expected item instance no longer occupies the requested slot
- **THEN** replacement returns false without changing storage, publishing an equipment update, or running lifecycle callbacks

#### Scenario: Equipment storage mutation participates in an active scope
- **WHEN** restoration, replacement, removal, or clearing is called while Equipment storage belongs to an active transaction
- **THEN** the storage mutation participates in that transaction, with lifecycle completion and publication deferred until commit

#### Scenario: Equipment replacement joins enlisted storage
- **GIVEN** Equipment storage is enlisted in a transaction
- **WHEN** an item is replaced through the simple Equipment operation
- **THEN** storage changes within that transaction and lifecycle/publication completion waits for commit

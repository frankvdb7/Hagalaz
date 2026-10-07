# Spec Delta

## MODIFIED Requirements

### Requirement: MoneyPouch separates gameplay operations from transaction participation
`IMoneyPouchContainer` MUST expose normal coin-domain operations and implement the empty public `IItemTransactional` capability marker. It MUST NOT expose a `Mutations` property or storage boundary. Its concrete domain implementation MUST provide exactly the pouch storage and one inventory item-storage boundary through the internal `IItemTransactionSource`. `TryAddExact` and `TryRemoveExact` MUST own a transaction when the current thread owns neither required boundary, participate in the same active current-thread transaction when it owns both, and throw before mutation for partial or conflicting current-thread ownership. A foreign transaction MUST be handled by the standalone transaction's normal contention behavior. The primary MoneyPouch API MUST NOT expose generic item-ID `Contains`, a domain-specific mutation boundary, or caller-managed publication receipts.

#### Scenario: MoneyPouch coin availability is domain-specific
- **WHEN** a caller checks whether a character has a positive coin amount
- **THEN** `HasCoins` compares the request against pouch coins and inventory coin item `995` using overflow-safe addition

### Requirement: Complete same-scope participation
Every transaction MUST have one explicit owner. A same-thread Begin on a boundary already bound to a transaction MUST throw, while a foreign overlapping Begin MUST wait until the existing scope ends. All transaction participants MUST be supplied to Begin; ordinary operations MUST NOT dynamically enlist storage. MoneyPouch exact operations MUST own a scope when the current thread owns neither its pouch nor its single inventory boundary, participate when both are owned by the same active current-thread transaction, and throw before mutation for partial or conflicting current-thread ownership. A foreign overlap MUST be handled by the standalone `Begin` path's normal contention. `IItemContainer.TryTransferTo` MUST own and commit a short transaction when neither required boundary is bound, participate without committing when both boundaries belong to the same active current-thread transaction, and reject partial or conflicting enlistment before mutation. `IMoneyPouchContainer.TryTransferCoinsFrom` MUST require its source boundary, pouch boundary, and single inventory boundary to belong to the same active caller-owned transaction; it MUST NOT create a transaction. Partial enlistment, conflicting active scopes, nested Begin, and wrong-thread use MUST throw `InvalidOperationException` before mutation. Participant aliases MUST be deduplicated independently from first-seen publication order; two different mutation boundaries for one storage MUST be rejected before locking, while repeated contributions of the same boundary instance are valid.

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
- **WHEN** public `TryAddExact` or `TryRemoveExact` is called while only one of the pouch and inventory boundaries is owned by the current thread
- **THEN** it fails before changing either scope or storage

#### Scenario: A MoneyPouch method rejects conflicting scopes
- **WHEN** the pouch and inventory boundaries belong to different active current-thread transactions
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

# Spec Delta

## MODIFIED Requirements

### Requirement: Transaction membership uses aggregate capabilities
`IItemContainer` and `IMoneyPouchContainer` MUST inherit the empty public `IItemTransactional` marker. Neither interface MUST expose a public `Mutations` property or mutation boundary. `ItemContainerTransaction.Begin(...)` MUST accept `params IItemTransactional[]` and callers MUST pass participating domain aggregates directly. Repository implementations MUST resolve contributions through the internal `IItemTransactionSource`; unsupported external marker implementations MUST fail clearly before any boundary is locked. `ItemContainer` MUST contribute its single boundary, and MoneyPouch MUST contribute its own storage and each required inventory storage. Contribution resolution MUST validate before locking, deduplicate storage aliases, preserve first-seen participant order for publication, and retain deterministic lock ordering independently.

#### Scenario: Callers enlist aggregates directly
- **WHEN** a caller opens a transaction for ordinary containers and a MoneyPouch
- **THEN** it passes the `IItemContainer` and `IMoneyPouchContainer` instances directly to `Begin(...)`

#### Scenario: Unsupported marker implementation is rejected before locking
- **WHEN** `Begin(...)` receives an external `IItemTransactional` that is not a repository transaction source
- **THEN** it throws an argument exception before acquiring boundary locks or mutating storage

#### Scenario: Composite participant aliases storage
- **WHEN** a MoneyPouch and another participant contribute the same underlying storage
- **THEN** that storage's owning boundary is locked and its state snapshotted once while participant publication order remains first-seen order

### Requirement: Cross-container transfers resolve destination capability internally
`IItemContainer.TryTransferTo(...)` MUST perform the existing atomic transfer algorithm between ordinary item containers. When neither storage is bound, it MUST own and commit a short transaction after the exact transfer succeeds; when both storages belong to the same active current-thread transaction, it MUST participate without committing. Partial or conflicting enlistment MUST be rejected before mutation. Destination storage MUST be resolved through the internal `IItemTransactionSource` contract without requiring a concrete `ItemContainer`, and a destination MUST contribute exactly one storage boundary. It MUST NOT dynamically enlist missing storage. It MUST validate and plan both changes before mutation, update each storage revision once on success, and defer publication until the owning transaction commits. A valid transfer rejection MUST return false without mutating either storage.

#### Scenario: Interface decorator supplies transfer storage
- **WHEN** the destination is exposed through an `IItemContainer` decorator backed by an internal transaction source
- **THEN** `TryTransferTo(...)` transfers using the contributed boundary without a concrete `ItemContainer` cast

#### Scenario: Standalone transfer owns a short transaction
- **WHEN** neither storage is bound and an exact transfer can succeed
- **THEN** the public method commits both changes and publishes them without a separate caller transaction

#### Scenario: Transfer rejects partial or conflicting enlistment
- **WHEN** source and destination participate in different scopes or only one storage is enlisted
- **THEN** the operation throws before either storage changes

### Requirement: MoneyPouch exact operations preserve complete-scope behavior
MoneyPouch exact operations MUST own a transaction when all required pouch and inventory storage is unbound, participate when all required storage belongs to the same active current-thread transaction, and reject partial or conflicting enlistment before mutation. Their public domain methods MUST retain the existing coin, overflow, sentinel, and notification behavior.

#### Scenario: Exact MoneyPouch operation participates in a complete scope
- **GIVEN** a transaction includes the MoneyPouch aggregate and all of its inventory storage
- **WHEN** a public exact pouch operation is called
- **THEN** it mutates within that scope without creating another transaction

#### Scenario: Exact MoneyPouch operation rejects partial enlistment
- **GIVEN** only some required pouch or inventory storage is enlisted
- **WHEN** a public exact pouch operation is called
- **THEN** it throws before changing pouch or inventory state

# Spec Delta

## MODIFIED Requirements

### Requirement: Cross-container transfers commit both stores atomically
An exact cross-container transfer MUST enter through `IItemContainerMutationBoundary` and use the one caller-owned `ItemContainerTransaction` to validate and plan source removal and destination insertion before changing either store, acquire distinct locks in stable order, call the single low-level `ItemContainerStorage` transfer algorithm, advance each storage revision once, and publish changed slots only after successful commit. The transfer boundary MUST NOT create or commit a transaction. A standalone domain owner MUST supply both boundaries before mutation; a composable operation MUST use the existing transaction only when both boundaries belong to it.

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

### Requirement: Multi-container mutations use an instance transaction
`ItemContainerTransaction` MUST provide one disposable atomic scope around existing synchronous domain operations. Every participant MUST be supplied before Begin, and storage aliases MUST be deduplicated without changing first-seen publication order. It MUST own ordered locks, snapshots, rollback and deferred publication. Domain operations MUST retain item semantics; no public mutation methods, dynamic enlistment, separate unit of work, committed-state query, publication call, or ambient joining API may be required. Internal storage participation MUST reject incomplete or conflicting current-thread enlistment and wrong-thread use of the same scope. Async, nested and distributed transaction support MUST NOT be introduced.

#### Scenario: A transaction participant receives a staging context
- **WHEN** a caller begins one transaction with every required participant and performs domain mutations
- **THEN** participating mutations use that owner scope without obtaining a public transaction context or acquiring additional locks

#### Scenario: A cross-storage mutation has no owner scope
- **WHEN** a caller attempts a cross-storage mutation without an active transaction containing both boundaries
- **THEN** the operation throws before changing either storage

#### Scenario: A helper requires missing storage
- **WHEN** a scope includes A but a composable operation requires A and B
- **THEN** the operation throws without acquiring or mutating B, and A remains bound to its existing scope

#### Scenario: Storage belongs to different scopes
- **WHEN** an operation requires storage bound to different active transactions
- **THEN** it throws without mutation or nested transaction creation

#### Scenario: Standalone domain operation owns its transaction
- **WHEN** a public standalone operation transfers or composes mutations across storage
- **THEN** it begins a transaction with all participants before mutation and commits exactly once on success

#### Scenario: A composite participant aliases storage
- **WHEN** ordinary and composite participants contribute the same storage
- **THEN** storage is locked and snapshotted once and participant publication order remains first-seen order

### Requirement: Complete same-scope participation
Every transaction MUST have one explicit owner. A transaction begin on storage already bound to a current-thread transaction MUST throw. A composable operation MUST require all of its storage to be enlisted in the one caller-owned transaction and MUST never create, commit, or discover a transaction. Standalone operations MUST explicitly begin their own transaction. Bindings owned by another thread MUST serialize through deterministic storage locks. Partial enlistment, conflicting current-thread transactions, nested Begin, and wrong-thread use MUST throw `InvalidOperationException` before mutation. Participant aliases MUST be deduplicated independently from first-seen publication order.

#### Scenario: Independent overlapping scopes contend
- **WHEN** another thread owns any required storage and the current thread owns none of it
- **THEN** Begin waits on deterministic storage locks and proceeds after the other scope completes

#### Scenario: Nested Begin is rejected
- **WHEN** the current thread begins a transaction for storage already enlisted in its active transaction
- **THEN** Begin throws and leaves the existing scope unchanged

#### Scenario: A helper requires missing storage
- **WHEN** a composable operation requires A and B but the caller's scope includes only A
- **THEN** it throws without locking or mutating B, and A remains bound to the original scope

#### Scenario: Storage belongs to different scopes
- **WHEN** required storage belongs to different active transactions
- **THEN** the operation throws without mutation or nested transaction creation

#### Scenario: A composite participant aliases storage
- **WHEN** two participants contribute the same underlying storage
- **THEN** that storage is locked and snapshotted once without changing participant publication order

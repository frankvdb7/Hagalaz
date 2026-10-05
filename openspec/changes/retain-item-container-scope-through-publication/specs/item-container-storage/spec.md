# Item container transaction scope delta

## MODIFIED Requirements

### Requirement: Transaction commit includes publication
`ItemContainerTransaction.Begin(...)` MUST create one synchronous scope over all resolved storage contributions. Mutation locks MUST be held while the transaction is Active. `Commit()` MUST make eager storage mutations irreversible and release mutation locks before committed domain completion and publication while retaining each storage binding to the committing scope. A foreign overlapping `Begin(...)` MUST wait until the scope finishes; a same-thread overlapping `Begin(...)` MUST throw. Ordinary mutation of storage still bound to a Committed scope MUST be rejected. After completion/publication and non-observable pending cleanup, bindings MUST be cleared under the ordered storage locks, waiters MUST be awakened, and the transaction MUST become Completed. Publication failure MUST NOT leak scope ownership, roll back storage, or permit publication retry. Rollback MUST restore snapshots before clearing bindings and releasing mutation locks. Transaction use remains synchronous and thread-affine.

Storage mutation methods MUST validate transaction access while holding their own mutation lock and MUST NOT expose transaction ownership as a mutation result. When publication ownership is needed, `ItemContainer` or the owning domain aggregate MUST capture the transaction owner under the same lock, perform the storage mutation, release the lock, and pass that captured owner explicitly to notification. Publication attribution MUST NOT re-read the storage's current transaction binding after mutation. A later transaction MUST NOT acquire ownership of an earlier standalone mutation's publication. Standalone publication remains non-transaction-scoped; this rule guarantees attribution, not retained-scope isolation for standalone callbacks. Mutation rollback is limited to item references, counts, slot topology, and revision; arbitrary mutable item metadata such as `ExtraData` is not deep-snapshotted.

#### Scenario: Foreign scope waits through publication
- **WHEN** one transaction publishes committed changes and another thread begins an overlapping scope
- **THEN** the second scope waits until publication and pending cleanup finish, then begins against the committed storage

#### Scenario: Same-thread callback cannot reenter its committed scope
- **WHEN** committed publication attempts an overlapping `Begin(...)` or ordinary mutation on the same thread
- **THEN** the operation throws `InvalidOperationException`, while committed reads remain available

#### Scenario: Publication failure releases waiting scopes
- **WHEN** a publisher throws after commit becomes irreversible
- **THEN** the failure propagates after cleanup, bindings are cleared, waiters are awakened, and a later scope can proceed without publication retry

#### Scenario: Standalone publication retains mutation-time attribution
- **WHEN** the owning aggregate captures a null transaction owner for a standalone mutation and another transaction binds the storage before notification
- **THEN** notification publishes as standalone and the later transaction does not record or roll back the earlier mutation

#### Scenario: Reacquisition interruption does not abandon teardown
- **WHEN** `Thread.Interrupt()` interrupts committed teardown while it waits to reacquire a storage mutation lock
- **THEN** teardown records the interruption, retries that lock, clears all bindings under the complete ordered lock set, wakes waiters, and propagates the interruption after releasing locks

#### Scenario: Storage commits before publication
- **WHEN** all staged mutations succeed
- **THEN** storage becomes irreversible before Commit releases locks and invokes deferred domain effects and publication

#### Scenario: Staging fails
- **WHEN** a scope exits without Commit after domain rejection or a mutation exception
- **THEN** disposal restores every enlisted store and revision, and emits no effects

#### Scenario: A participant publisher fails
- **WHEN** one participant throws during publication
- **THEN** later participants are skipped, storage remains committed, the original exception propagates directly, and another Commit is invalid and Dispose emits nothing

### Requirement: Mutation authorization and publication attribution are captured under lock
Every storage mutation MUST validate transaction access while holding that storage's mutation lock. Storage mutation methods MUST NOT expose transaction ownership as a mutation result. When publication ownership is needed, `ItemContainer` or the owning domain aggregate MUST capture the current transaction owner while holding the same lock, perform the storage mutation, release the lock, and pass that captured owner explicitly to notification. Publication MUST NOT re-read the storage's current transaction binding after mutation. A later transaction MUST NOT acquire ownership of an earlier standalone mutation's publication. Standalone publication remains outside a retained transaction scope; this invariant guarantees attribution, not scope isolation for standalone callbacks.

#### Scenario: Standalone publication cannot be attributed to a later scope
- **WHEN** the owning aggregate captures no transaction for a standalone mutation and another transaction binds the storage before its publisher runs
- **THEN** the original mutation publishes standalone and the later transaction neither records nor rolls back that mutation

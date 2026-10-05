# Item container transaction scope delta

## MODIFIED Requirements

### Requirement: Transaction commit includes publication
`ItemContainerTransaction.Begin(...)` MUST create one synchronous scope over all resolved storage contributions. Mutation locks MUST be held while the transaction is Active. `Commit()` MUST make eager storage mutations irreversible and release mutation locks before committed domain completion and publication while retaining each storage binding to the committing scope. A foreign overlapping `Begin(...)` MUST wait until the scope finishes; a same-thread overlapping `Begin(...)` MUST throw. Ordinary mutation of storage still bound to a Committed scope MUST be rejected. After completion/publication and non-observable pending cleanup, bindings MUST be cleared under the ordered storage locks, waiters MUST be awakened, and the transaction MUST become Completed. Publication failure MUST NOT leak scope ownership, roll back storage, or permit publication retry. Rollback MUST restore snapshots before clearing bindings and releasing mutation locks. Transaction use remains synchronous and thread-affine.

Mutation rollback is limited to item references, counts, slot topology, and revision; arbitrary mutable item metadata such as `ExtraData` is not deep-snapshotted.

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
- **WHEN** a mutation boundary attributes a change while no transaction is bound and another transaction binds storage before standalone publication
- **THEN** the standalone change publishes after unlocking and the later transaction does not record or roll back it

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

### Requirement: Mutation authorization and attribution are resource-bound
Every storage mutation MUST validate transaction access while holding that storage's mutation lock. Transaction attribution MUST occur while the same mutation lock is still held. The mutation boundary MUST inspect the storage's bound transaction under that lock. If an active transaction is bound, it MUST record the mutation directly into that transaction before the lock is released. If no transaction is bound, the mutation is standalone and observable publication MUST occur only after the lock is released. Ordinary item-domain operations MUST NOT transport transaction identity through mutation results, local publication parameters, ambient context, or transaction-accessor services. Publication MUST NOT re-read storage transaction ownership after the attribution decision has been made. Item-container transaction participation is resource-bound to enlisted storage; implementations MUST NOT use ambient transaction accessors such as `AsyncLocal`, `ThreadLocal`, `ThreadStatic` current-transaction state, or implicit current-scope APIs for ordinary mutation attribution.

#### Scenario: Standalone mutation publishes after unlocking
- **WHEN** a mutation boundary attributes a change while no transaction is bound
- **THEN** it returns that the change was not deferred and the aggregate publishes only after releasing the storage lock

#### Scenario: Transaction-bound mutation is recorded under lock
- **WHEN** a mutation boundary attributes a change while an active transaction is bound
- **THEN** it records the change before the lock is released and the aggregate does not publish standalone

#### Scenario: Transaction attribution uses no ambient scope
- **WHEN** an ordinary item-container mutation runs
- **THEN** participation is determined from the storage binding under its lock without ambient state or transaction identity passed through the operation

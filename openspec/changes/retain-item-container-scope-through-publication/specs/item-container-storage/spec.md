# Item container transaction scope delta

## MODIFIED Requirements

### Requirement: Transaction commit includes publication
`ItemContainerTransaction.Begin(...)` MUST create one synchronous scope over all resolved boundary contributions. Boundary mutation locks MUST be held while the transaction is Active. `Commit()` MUST make eager storage mutations irreversible and release boundary mutation locks before committed domain completion and publication while retaining each boundary binding to the committing scope. A foreign overlapping `Begin(...)` MUST wait until the scope finishes; a same-thread overlapping `Begin(...)` MUST throw. Ordinary mutation through a boundary still bound to a Committed scope MUST be rejected. After completion/publication and non-observable pending cleanup, bindings MUST be cleared under the ordered boundary locks, waiters MUST be awakened, and the transaction MUST become Completed. Publication failure MUST NOT leak scope ownership, roll back storage, or permit publication retry. Rollback MUST restore snapshots before clearing bindings and releasing boundary mutation locks. Transaction use remains synchronous and thread-affine.

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
- **WHEN** a standalone mutation records changes and releases its mutation lock before publication while another thread begins an overlapping transaction
- **THEN** the standalone publication owner prevents the transaction from binding until publication finishes, and the standalone change is published before the later transaction begins

#### Scenario: Reacquisition interruption does not abandon teardown
- **WHEN** `Thread.Interrupt()` interrupts committed teardown while it waits to reacquire a boundary mutation lock
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
Every ordinary mutation MUST be authorized by its owning boundary while holding that boundary's mutation lock. Storage mutation algorithms MUST NOT acquire locks or validate synchronization themselves. One per-operation mutation scope MUST acquire the boundary lock for standalone mutations or borrow the lock held by the active owning transaction, and MUST release only a lock it acquired. If the current thread already owns the boundary lock without an active transaction binding, starting an ordinary mutation scope MUST fail. The scope MUST validate the boundary's transaction before allowing a borrowed mutation. Successful change attribution MUST occur while the same boundary lock is still held. Transaction-owned changes MUST be recorded directly into that active transaction; standalone changes MUST be published only after the scope releases its owned lock. A scope that records no changes MUST publish nothing. Ordinary callers MUST NOT branch on deferred/immediate publication state or re-read transaction ownership after attribution. Transaction membership remains boundary-bound; ambient transaction accessors such as `AsyncLocal`, `ThreadLocal`, `ThreadStatic` current-transaction state, and implicit current-scope APIs are prohibited.

#### Scenario: Standalone mutation uses one lock and publishes after unlock
- **WHEN** an ordinary mutation runs on unbound storage and succeeds
- **THEN** its operation scope acquires the boundary lock once, attributes the change while holding it, releases it, and only then publishes

#### Scenario: Transaction-owned mutation borrows its lock
- **WHEN** an ordinary mutation runs on storage enlisted in the current thread's active transaction
- **THEN** its operation scope does not reacquire or release the transaction-owned lock and records changes into that transaction before returning

#### Scenario: Mutation without caller lock is rejected
- **WHEN** a storage mutation algorithm is called without ownership of its mutation lock
- **THEN** it throws `InvalidOperationException` before changing storage

#### Scenario: Manually held lock cannot imply transaction ownership
- **WHEN** the current thread holds a boundary mutation lock that has no active transaction binding and starts an ordinary mutation scope
- **THEN** the scope throws without borrowing or releasing the externally owned lock

#### Scenario: Unsuccessful standalone mutation publishes nothing
- **WHEN** an ordinary operation returns without recording changes
- **THEN** its owned lock is released and no publication occurs

#### Scenario: Transaction attribution uses no ambient scope
- **WHEN** an ordinary item-container mutation runs
- **THEN** participation is determined from the boundary binding under its lock without ambient state or transaction identity passed through the operation

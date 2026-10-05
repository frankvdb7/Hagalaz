## MODIFIED Requirements

### Requirement: Mutation authorization and attribution are resource-bound
Every storage mutation MUST validate transaction access while holding that storage's mutation lock. Ordinary storage mutation algorithms MUST NOT acquire that lock themselves. One per-operation mutation scope MUST acquire the lock for standalone mutations or borrow the lock already held by the active owning transaction, and MUST release only a lock it acquired. If the current thread already owns the lock without an active transaction binding, starting an ordinary mutation scope MUST fail. The scope MUST validate the bound transaction before allowing a borrowed mutation. Successful change attribution MUST occur while the same mutation lock is still held. Transaction-owned changes MUST be recorded directly into that active transaction; standalone changes MUST be published only after the scope releases its owned lock. A scope that records no changes MUST publish nothing. Ordinary callers MUST NOT branch on deferred/immediate publication state or re-read transaction ownership after attribution. Transaction membership remains resource-bound to storage; ambient transaction accessors are prohibited.

#### Scenario: Standalone mutation uses one lock and publishes after unlock
- **WHEN** an ordinary mutation runs on unbound storage and succeeds
- **THEN** its operation scope acquires the storage lock once, attributes the change while holding it, releases it, and only then publishes

#### Scenario: Transaction-owned mutation borrows its lock
- **WHEN** an ordinary mutation runs on storage enlisted in the current thread's active transaction
- **THEN** its operation scope does not reacquire or release the transaction-owned lock and records changes into that transaction before returning

#### Scenario: Mutation without caller lock is rejected
- **WHEN** a storage mutation algorithm is called without ownership of its mutation lock
- **THEN** it throws `InvalidOperationException` before changing storage

#### Scenario: Manually held lock cannot imply transaction ownership
- **WHEN** the current thread holds a storage mutation lock that has no active transaction binding and starts an ordinary mutation scope
- **THEN** the scope throws without borrowing or releasing the externally owned lock

#### Scenario: Unsuccessful standalone mutation publishes nothing
- **WHEN** an ordinary operation returns without recording changes
- **THEN** its owned lock is released and no publication occurs

#### Scenario: Transaction attribution uses no ambient scope
- **WHEN** an ordinary item-container mutation runs
- **THEN** participation is determined from the storage binding under its lock without ambient state or transaction identity passed through the operation

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

### Requirement: Transaction rollback restores under caller-owned locks
Rollback restoration MUST require caller-owned mutation-lock access and MUST NOT reacquire the same lock. It is not an ordinary active mutation and MUST NOT use normal active-transaction mutation authorization. The transaction MUST retain participant locks and bindings until all snapshot restores have been attempted, then clear bindings and release locks.

#### Scenario: Rollback restores while transaction owns locks
- **WHEN** a transaction is disposed without commit
- **THEN** it restores each snapshot while holding the participant locks, attempts every restore, and publishes nothing

### Requirement: Equipment publishes only after lifecycle effects
Direct equipment restoration, replacement, full removal, and clearing MUST publish committed equipment state only after all required equipment lifecycle effects have been attempted. Storage MUST commit before lifecycle callbacks run. Clearing MUST remove all equipped items before callbacks and attempt `OnUnequipped` for every previously equipped item. Post-commit lifecycle or publication failures MUST NOT roll back committed storage. Every post-commit action MUST be attempted; one failure MUST preserve and rethrow the original exception, while multiple failures MUST be aggregated.

#### Scenario: Equipment replacement callbacks precede publication
- **WHEN** an expected equipped item is replaced
- **THEN** storage contains the replacement before `OnUnequipped` and `OnEquipped`, and publication follows both callback attempts

#### Scenario: Interactive non-weapon equipment replacement callbacks precede publication
- **WHEN** an incoming item replaces an occupied non-weapon/non-shield equipment slot through the standalone custom unequip path
- **THEN** the incoming `OnEquipped` callback runs after storage mutation and lock release but before Equipment publication

#### Scenario: Interactive equipment replacement rejects existing ownership before mutation
- **WHEN** inventory or Equipment already belongs to a transaction or standalone publication scope before interactive non-weapon replacement
- **THEN** the operation throws before removing the incoming inventory item or invoking the custom unequip command

#### Scenario: Full equipment removal callback precedes publication
- **WHEN** an equipped item is fully removed
- **THEN** storage no longer contains it before `OnUnequipped`, and publication follows the callback attempt

#### Scenario: Equipment clear exhausts callbacks after storage clear
- **WHEN** equipment containing one or more items is cleared
- **THEN** storage is empty before callbacks, every prior item's `OnUnequipped` is attempted, and publication follows all callback attempts

## ADDED Requirements

### Requirement: Standalone publication retains storage ownership
A standalone mutation that records changes MUST claim storage-owned publication ownership while holding the mutation lock before releasing it. That ownership MUST remain through standalone domain lifecycle effects and publication, while observable callbacks run without the mutation lock. Ordinary mutations MUST reject storage with standalone publication ownership. Explicit transaction `Begin(...)` MUST wait for a foreign standalone publication owner and reject same-thread overlap; a multi-storage Begin MUST release any acquired lock prefix before waiting and retry deterministic acquisition after ownership clears. Standalone completion MUST clear ownership and pulse waiters under the mutation lock after publication. Publication failure MUST NOT leak ownership. If cleanup lock reacquisition is interrupted, cleanup MUST retry, then surface the interruption together with any publication failure.

#### Scenario: Ordinary mutation cannot change state during standalone publication
- **WHEN** a standalone publisher is running with logical publication ownership retained
- **THEN** same-thread and foreign ordinary mutations throw before changing storage

#### Scenario: Equipment lifecycle retains standalone ownership
- **WHEN** standalone Equipment lifecycle effects run after releasing the mutation lock and before publication
- **THEN** an overlapping foreign transaction waits through both lifecycle effects and publication, while same-thread reentrant Begin is rejected

#### Scenario: Standalone publication cleanup survives failure and interruption
- **WHEN** publication fails or cleanup is interrupted while reacquiring the mutation lock
- **THEN** ownership is eventually cleared under the lock, waiters are pulsed, and all independent failures are preserved

### Requirement: Transaction change attribution validates storage ownership
`ItemContainerTransaction.RecordChanges(...)` MUST accept changes only when the given storage is bound to that exact active transaction and the calling thread holds that storage's mutation lock. It MUST reject unbound, differently bound, or unlocked storage before adding change data.

#### Scenario: Change attribution rejects storage outside the transaction
- **WHEN** a transaction records changes for storage that it did not enlist
- **THEN** it throws without binding or mutating the other storage

#### Scenario: Change attribution requires the enlisted storage lock
- **WHEN** an active transaction records changes for its enlisted storage without owning its mutation lock
- **THEN** it throws and leaves transaction change attribution unchanged

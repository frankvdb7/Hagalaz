# Spec Delta

## MODIFIED Requirements

### Requirement: Mutation authorization and standalone publication belong to the boundary
Each mutation boundary MUST authorize synchronous operations against its storage. A per-operation scope MUST acquire the boundary lock for standalone work or borrow the lock held by its active transaction, and release only a lock it acquired. A manually held lock without a transaction binding MUST NOT authorize mutation. Change attribution MUST occur under the same lock and transaction binding, and MUST reject a boundary outside the active transaction or an unlocked boundary. A scope with no changes MUST publish nothing. Transaction membership is determined from the boundary binding; ambient current-transaction state is prohibited.

When a standalone mutation records changes, the boundary MUST claim publication ownership before releasing its lock. It retains that ownership through lifecycle effects and publication while callbacks run without the lock. Ordinary mutations MUST reject same-thread reentrancy and MUST wait for foreign transaction or publication ownership to clear before mutating. Same-thread overlapping Begin is rejected and foreign overlapping Begin waits. A multi-boundary Begin MUST release any acquired lock prefix before waiting and retry deterministic acquisition after ownership clears. Standalone cleanup MUST clear ownership and pulse waiters under the boundary lock, including after publisher failure.

#### Scenario: Standalone mutation publishes after unlocking
- **WHEN** an ordinary mutation succeeds on unbound storage
- **THEN** it acquires the boundary lock once, records changes and claims publication ownership under that lock, releases the lock, and publishes before an overlapping foreign transaction can bind

#### Scenario: Transaction-owned mutation borrows its boundary lock
- **WHEN** an ordinary mutation targets a boundary enlisted in the current thread's active transaction
- **THEN** it neither reacquires nor releases the transaction-owned lock and attributes changes before returning

#### Scenario: A manually held lock does not authorize mutation
- **WHEN** the current thread holds a boundary lock without a transaction binding and starts an ordinary mutation
- **THEN** the operation throws without borrowing or releasing the external lock

#### Scenario: Change attribution requires the enlisted boundary and lock
- **WHEN** changes are attributed for a boundary outside the active transaction or without its mutation lock
- **THEN** attribution throws without adding change data

#### Scenario: An unsuccessful standalone mutation publishes nothing
- **WHEN** an ordinary operation returns without recording changes
- **THEN** its lock is released without publication ownership or notification

#### Scenario: Ordinary mutation cannot change state during standalone publication
- **WHEN** a standalone publisher runs while logical ownership remains on its boundary and same-thread or foreign ordinary mutation starts
- **THEN** same-thread mutation throws before changing storage, while foreign mutation waits until standalone ownership is released and then proceeds

#### Scenario: Foreign ordinary mutation waits through standalone publication
- **WHEN** a standalone publisher runs while logical ownership remains on its boundary and a foreign thread starts an ordinary mutation
- **THEN** the mutation waits without changing storage, then proceeds and publishes after standalone ownership is released

#### Scenario: Foreign ordinary mutation waits through transaction publication
- **WHEN** a committed transaction publisher runs while its boundary remains bound and a foreign thread starts an ordinary mutation
- **THEN** the mutation waits without changing storage, then proceeds and publishes after the transaction binding is released

#### Scenario: Waiting mutation proceeds after publisher failure
- **WHEN** a standalone publisher throws while a foreign ordinary mutation waits on its boundary
- **THEN** standalone ownership is cleared and waiters are pulsed before the original publication exception propagates, allowing the waiting mutation to proceed

#### Scenario: Equipment lifecycle retains standalone ownership
- **WHEN** standalone Equipment lifecycle effects run after unlock and before publication
- **THEN** an overlapping foreign transaction waits through both phases and same-thread reentrant Begin is rejected

#### Scenario: Standalone publication cleanup releases ownership after failure
- **WHEN** a standalone publisher throws after ownership is claimed
- **THEN** cleanup clears ownership and pulses waiters under the boundary lock before propagating the publication exception

#### Scenario: Mutation participation uses no ambient scope
- **WHEN** an ordinary item-container mutation runs
- **THEN** participation is determined from its boundary binding without ambient state or transaction identity passed through the operation

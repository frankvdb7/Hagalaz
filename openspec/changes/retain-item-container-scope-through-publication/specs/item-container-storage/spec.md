# Item container transaction scope delta

## MODIFIED Requirements

### Requirement: Transaction scope remains owned through committed publication
`ItemContainerTransaction.Begin(...)` MUST create one synchronous scope over all resolved storage contributions. Mutation locks MUST be held while the transaction is Active. `Commit()` MUST make eager storage mutations irreversible and release mutation locks before committed domain completion and publication while retaining each storage binding to the committing scope. A foreign overlapping `Begin(...)` MUST wait until the scope finishes; a same-thread overlapping `Begin(...)` MUST throw. Ordinary mutation of storage still bound to a Committed scope MUST be rejected. After completion/publication and non-observable pending cleanup, bindings MUST be cleared under the ordered storage locks, waiters MUST be awakened, and the transaction MUST become Completed. Publication failure MUST NOT leak scope ownership, roll back storage, or permit publication retry. Rollback MUST restore snapshots before clearing bindings and releasing mutation locks. Transaction use remains synchronous and thread-affine.

#### Scenario: Foreign scope waits through publication
- **WHEN** one transaction publishes committed changes and another thread begins an overlapping scope
- **THEN** the second scope waits until publication and pending cleanup finish, then begins against the committed storage

#### Scenario: Same-thread callback cannot reenter its committed scope
- **WHEN** committed publication attempts an overlapping `Begin(...)` or ordinary mutation on the same thread
- **THEN** the operation throws `InvalidOperationException`, while committed reads remain available

#### Scenario: Publication failure releases waiting scopes
- **WHEN** a publisher throws after commit becomes irreversible
- **THEN** the failure propagates after cleanup, bindings are cleared, waiters are awakened, and a later scope can proceed without publication retry

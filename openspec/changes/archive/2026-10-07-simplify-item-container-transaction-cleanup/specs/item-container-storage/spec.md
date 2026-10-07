# Spec Delta

## MODIFIED Requirements

### Requirement: Standalone publication retains boundary ownership
A standalone mutation that records changes MUST claim boundary-owned publication ownership while holding the boundary mutation lock before releasing it. That ownership MUST remain through standalone domain lifecycle effects and publication, while observable callbacks run without the boundary lock. Ordinary mutations MUST reject a boundary with standalone publication ownership. Explicit transaction `Begin(...)` MUST wait for a foreign standalone publication owner and reject same-thread overlap; a multi-boundary Begin MUST release any acquired lock prefix before waiting and retry deterministic acquisition after ownership clears. Standalone completion MUST clear ownership and pulse waiters under the boundary lock after publication. A publisher failure MUST still trigger the normal ownership-release cleanup before propagating. Synchronization invariant failures MUST propagate directly and MUST NOT be aggregated with publication failures.

#### Scenario: Ordinary mutation cannot change state during standalone publication
- **WHEN** a standalone publisher is running with logical publication ownership retained on its boundary
- **THEN** same-thread and foreign ordinary mutations throw before changing storage

#### Scenario: Equipment lifecycle retains standalone ownership
- **WHEN** standalone Equipment lifecycle effects run after releasing the mutation lock and before publication
- **THEN** an overlapping foreign transaction waits through both lifecycle effects and publication, while same-thread reentrant Begin is rejected

#### Scenario: Standalone publication cleanup releases ownership after failure
- **WHEN** the standalone publisher throws after publication ownership was claimed
- **THEN** cleanup clears ownership and pulses waiters under the boundary lock, and the publication exception propagates

### Requirement: Disposable atomic mutation scope
An item transaction MUST expose Begin, Commit, and Dispose, with every participant resolved and validated before locking. It MUST capture all snapshots before establishing originating-thread boundary bindings. Construction failure MUST release acquired locks, leave storage unchanged, and publish nothing because no bindings have yet been established. Dispose without commit MUST attempt every snapshot restore while retaining participant locks and bindings, discard pending completion facts, clear bindings, and release locks; rollback MUST NOT pulse waiters because contenders cannot acquire a participant lock before bindings are cleared. Commit MUST declare already-mutated storage irreversible before dropping snapshots and releasing mutation-boundary locks; it MUST retain boundary bindings while executing callbacks, then clear bindings under the ordered boundary locks and pulse waiters. Domain completion and publication MUST NOT run unless all transaction mutation locks were released. Synchronization invariant failures MUST propagate normally and MUST NOT be aggregated with domain failures. Repeated owner-thread disposal MUST be inert and repeated commit MUST NOT retry publication.

#### Scenario: Construction fails during snapshot capture
- **WHEN** snapshot capture throws after locks have been acquired
- **THEN** all acquired locks are released, no binding survives, and storage and publication remain unchanged

#### Scenario: Mutation scope exits without commit
- **WHEN** an early return, exception, or cancellation exits an active scope
- **THEN** disposal restores every participant and emits no deferred effect

#### Scenario: Completion throws after commit
- **WHEN** a hook or publisher throws after the irreversible transition
- **THEN** committed storage remains permanent, all transaction locks have been released, disposal is inert, and commit cannot retry completion

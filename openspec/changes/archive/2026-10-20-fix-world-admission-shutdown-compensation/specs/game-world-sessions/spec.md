## MODIFIED Requirements

### Requirement: Cleanup is exact-owner and recoverable

The system MUST remove only the local session or distributed claim belonging to the expected session and MUST retain enough local state for reconciliation after a transient infrastructure failure.

#### Scenario: Cleanup does not remove a replacement session

- GIVEN the expected session has been replaced at the same connection
- WHEN exact-owner cleanup runs
- THEN the replacement session and its distributed claim remain intact

#### Scenario: Cleanup failure remains recoverable

- GIVEN exact-owner cleanup cannot complete because infrastructure fails temporarily
- WHEN cleanup returns
- THEN the local state needed to retry the cleanup is retained
- AND the existing lease cycle can retry the cleanup

#### Scenario: Failed admission completes after GameWorker termination

- GIVEN world admission registered a character, failed before session promotion, and scheduled rollback
- AND GameWorker execution has definitively completed before the rollback task receives a tick
- WHEN admission compensation runs
- THEN the registered character's exact local ownership is removed and its owned resources are disposed
- AND the exact persistence lifecycle and session ownership are released or retained for the existing recovery path
- AND compensation completes without requiring another GameWorker tick
- AND compensation never mutates GameWorld state while GameWorker execution may still run

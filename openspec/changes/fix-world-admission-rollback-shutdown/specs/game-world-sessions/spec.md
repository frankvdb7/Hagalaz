## MODIFIED Requirements

### Requirement: Cleanup is exact-owner and recoverable

The system MUST remove only the local session or distributed claim belonging to the expected session and MUST retain enough local state for reconciliation after a transient infrastructure failure. Failed world admission MUST NOT remain indefinitely pending when GameWorker execution becomes unavailable during rollback. Once rollback requires game-thread character cleanup, that cleanup MUST either execute or reach an explicit terminal lifecycle handoff before persistence and session/claim ownership is considered complete.

#### Scenario: Cleanup does not remove a replacement session

- **GIVEN** the expected session has been replaced at the same connection
- **WHEN** exact-owner cleanup runs
- **THEN** the replacement session and its distributed claim remain intact

#### Scenario: Cleanup failure remains recoverable

- **GIVEN** exact-owner cleanup cannot complete because infrastructure fails temporarily
- **WHEN** cleanup returns
- **THEN** the local state needed to retry the cleanup is retained
- **AND** the existing lease cycle can retry the cleanup

#### Scenario: Admission rollback reaches a terminal outcome during shutdown

- **GIVEN** world admission registered a character and then failed
- **AND** the GameWorker begins or completes shutdown before the rollback action's normal tick
- **WHEN** admission performs compensation
- **THEN** the registered-character cleanup action executes through the normal tick or explicit shutdown handoff
- **AND** rollback does not remain indefinitely awaiting an executor that no longer exists
- **AND** persistence and local session/claim cleanup follow the existing exact-owner ordering

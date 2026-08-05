# Delta: game-world sessions

## MODIFIED Requirements

### Requirement: Lost connections are reconciled

The system MUST use the existing game-session lease cycle as the single retry owner for deferred connection aborts and exact-owner claim cleanup.

Each pending-abort reservation MAY be claimed by one processor through a five-minute local abort-processing lease. This timeout is independent of the distributed world-session claim lease. The processing lease MUST contain a unique ownership token and its start time. Completion or release MUST succeed only for the exact processing lease that claimed the reservation; a stale processor MUST NOT clear or release a newer processor's reservation.

The external connection abort operation MUST be idempotent. An expired local processing lease fences stale local state completion, but the processor that exceeded its timeout may still invoke the external abort while a later processor is retrying it.

#### Scenario: Aborting a lost connection fails temporarily

- GIVEN a session has lost its distributed claim
- WHEN the connection abort fails
- THEN the session remains reserved for abort reconciliation
- AND the existing lease cycle retries the abort

#### Scenario: Abort cleanup completes

- GIVEN an abort reservation is being processed
- WHEN the connection abort succeeds
- THEN the reservation is cleared
- AND the connection identifier can be reused

#### Scenario: Deferred abort is retried by the lease cycle

- GIVEN an abort reservation whose previous abort attempt failed
- WHEN the next lease cycle runs
- THEN the reservation is processed again
- AND a successful abort clears the reservation

#### Scenario: Abort failure does not create a second retry subsystem

- GIVEN an abort attempt fails
- WHEN the failure is recorded
- THEN the local reservation remains available for lease reconciliation
- AND no separate retry queue or retry worker is created

#### Scenario: An expired abort processor cannot complete a newer reservation

- GIVEN a pending abort processor has exceeded the five-minute local processing lease
- WHEN the lease cycle claims the reservation with a new processing token
- AND the expired processor later reports completion
- THEN the stale completion is rejected
- AND the newer reservation remains owned by the new processing token

#### Scenario: External abort is safe across processing lease expiry

- GIVEN an abort processor exceeds its local processing lease
- WHEN both the expired processor and the replacement processor invoke the external abort
- THEN the connection terminator treats repeated abort calls as idempotent
- AND local completion is controlled only by the current processing token

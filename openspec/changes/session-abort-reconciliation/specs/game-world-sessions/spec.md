# Delta: game-world sessions

## MODIFIED Requirements

### Requirement: Lost connections are reconciled

The system MUST use the existing game-session lease cycle as the single retry owner for deferred connection aborts and exact-owner claim cleanup.

Each pending-abort reservation MAY be claimed by one processor through a
non-expiring in-process processing marker. The marker MUST contain a unique
ownership token. A failed processor MUST explicitly release the marker so the
existing lease cycle can retry it. Completion or release MUST succeed only for
the exact processing token that claimed the reservation; a stale processor
MUST NOT clear or release a newer processor's reservation.

The external connection abort operation MUST be idempotent. The local
processing token fences stale local state completion while a later processor
is retrying a released reservation.

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

#### Scenario: A stale abort processor cannot complete a newer reservation

- GIVEN a pending abort processor releases its processing marker after failure
- WHEN the lease cycle claims the reservation with a new processing token
- AND the previous processor later reports completion
- THEN the stale completion is rejected
- AND the newer reservation remains owned by the new processing token

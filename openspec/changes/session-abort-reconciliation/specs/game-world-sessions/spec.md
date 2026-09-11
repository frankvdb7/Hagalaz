# Delta: game-world sessions

## MODIFIED Requirements

### Requirement: Lost connections are reconciled

The system MUST use the existing game-session lease cycle as the single retry owner for deferred connection aborts and exact-owner claim cleanup.

Each pending-abort reservation MAY be claimed by one processor through a
non-expiring store-owned processing marker. A failed processor MUST explicitly
release the marker so the existing lease cycle can retry it. Begin, completion,
and release MUST verify the exact session instance, so a stale session MUST NOT
change a reservation for another session using the same connection identifier.

The external connection abort operation MUST be idempotent. The exact-session
check fences stale local state completion while a later processor is retrying
a released reservation.

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

#### Scenario: A stale session cannot complete a newer reservation

- GIVEN a pending abort processor releases its processing marker after failure
- WHEN the lease cycle claims the reservation for a different session instance
- AND the previous processor later reports completion
- THEN the stale completion is rejected
- AND the newer reservation remains owned by the new processing marker

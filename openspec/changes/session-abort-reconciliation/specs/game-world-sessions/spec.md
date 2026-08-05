# Delta: game-world sessions

## MODIFIED Requirements

### Requirement: Lost connections are reconciled

The system MUST use the existing game-session lease cycle as the single retry owner for deferred connection aborts and exact-owner claim cleanup.

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

# Design

## Existing mechanisms reused

- `GameSessionStore` remains the single local owner of session and reservation state.
- `GameSessionLeaseService` remains the single periodic reconciliation owner.
- `IGameSessionConnectionTerminator` remains the boundary for aborting an external connection.
- The BCL `PeriodicTimer` remains the lease-cycle scheduler.

## Design decision

Use one focused `GameSessionAbortCoordinator` to centralize the abort lifecycle:

1. reserve the session for abort;
2. mark the reservation as processing;
3. verify that the connection identifier has not been reused;
4. abort the external connection;
5. complete the reservation, or release only the processing marker so the lease cycle can retry.

The lease service calls this coordinator for both immediate lost-claim handling and deferred reservations. No separate retry queue is required because the lease cycle already provides periodic retry ownership.

`GameSessionStore` represents the processing marker as one nullable `AbortProcessingLease` value object containing a unique token and start time. The local processing timeout is five minutes and is intentionally separate from the distributed claim lease. A completion or release operation must present the exact lease value that began processing, so an expired processor cannot modify a reservation reclaimed by a later processor.

The external connection terminator is assumed to provide idempotent abort behavior. The local token fences state transitions, but it cannot prevent an expired processor from reaching the external abort call after a replacement processor has started.

## Invariants

- A connection identifier with a pending abort reservation cannot be reused.
- A failed abort does not remove the reservation.
- A successful primary promotion is not undone by best-effort cleanup failure.
- Cleanup operations are exact-owner operations.
- Abort-processing completion and release are exact-processing-lease operations.
- There is one retry owner for deferred session cleanup.

## Deliberately rejected alternatives

- A dedicated channel/queue: duplicates lease-cycle retry ownership and adds capacity, overflow, and shutdown behavior.
- A third-party retry package: does not solve the local reservation and exact-owner state problem.
- A generic workflow or state-machine framework: the state transitions are narrow and belong in the existing store.

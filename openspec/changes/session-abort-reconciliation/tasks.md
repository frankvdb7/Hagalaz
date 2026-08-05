# Tasks

## Scope and design

- [x] Record the goal, non-goals, invariants, and acceptance criteria.
- [x] Confirm the existing lease cycle can own deferred retry behavior.
- [x] Remove the dedicated retry queue from production registration.

## Implementation

- [x] Centralize abort lifecycle transitions in `GameSessionAbortCoordinator`.
- [x] Route immediate and deferred aborts through the coordinator.
- [x] Retain failed claim cleanup for lease reconciliation instead of queueing it.
- [x] Preserve cancellation semantics after a committed promotion.

## Verification

- [x] Cover failed abort and successful lease reconciliation.
- [x] Cover cancellation after promotion.
- [x] Cover connection-ID reuse after reconciliation.
- [x] Run GameWorld unit tests.
- [x] Run GameWorld integration tests.
- [ ] Resolve any behavior change where deferred cleanup is reported as logical session removal failure.
- [ ] Ensure a failed completion cannot strand a processing abort reservation.
- [ ] Update the current behavior specification after the remaining review findings are resolved.

# Session abort reconciliation

## Status

Implementation in progress.

## Goal

Ensure that a lost or replaced game-session connection is aborted and retried safely without introducing a second retry subsystem.

## In scope

- Preserve an atomic local reservation while connection abort cleanup is incomplete.
- Centralize abort reservation, execution, completion, and retry-state transitions.
- Reuse the existing game-session lease cycle for deferred abort reconciliation.
- Remove the dedicated retry queue and its duplicate retry ownership.
- Cover abort failure, cancellation after promotion, connection-ID reuse, and reconciliation.

## Non-goals

- General-purpose retry infrastructure.
- A new background worker or queue.
- Redesigning all game-session or character-cleanup behavior.
- Changing distributed claim ownership semantics.
- Adding a third-party queue or workflow dependency.

## Acceptance criteria

- A replaced or lost connection is aborted when possible.
- A failed abort remains retryable through the lease cycle.
- A pending abort reservation prevents unsafe connection-ID reuse.
- Successful abort reconciliation releases the reservation.
- Promotion remains successful if best-effort cleanup fails or request cancellation occurs after promotion.
- Existing unit and integration tests pass.

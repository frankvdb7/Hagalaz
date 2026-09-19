## Context

`AuthenticationService.SignOutAsync` owns the request timeout and normal persistence/session handoff. `CharacterLogoutService` schedules terminal work on the shared GameWorker scheduler. The scheduled work can outlive the `DetachAsync` wait, so request-side `finally` logic cannot be the only owner of recovery eligibility.

## Goals / Non-Goals

**Goals:**

- Keep normal logout recovery hidden until the normal terminal handoff actually fails or is canceled.
- Preserve a cancellation intent across the scheduled terminal transition.
- Make snapshot establishment and promotion of that intent one state-lock operation.
- Preserve original terminal exceptions and existing persistence/session idempotency.

**Non-Goals:**

- Do not change the 30-second sign-out timeout.
- Do not cancel the shared GameWorker task after terminal work has been scheduled.
- Do not add a new worker, queue, retry mechanism, or general logout state machine.
- Do not change map-region or CodeQL behavior.

## Decisions

1. `CharacterLogoutService.DetachAsync` records a canceled terminal wait in the existing `CharacterLogoutState`. It does not mark an absent snapshot recoverable.
2. `CharacterLogoutState.SetSnapshot` promotes the recorded cancellation intent to `RecoveryEligible` while holding the existing state gate. This closes the race between cancellation and snapshot storage without exposing a second coordinator.
3. If the scheduled terminal task fails after the snapshot is stored, the task marks the already-valid logout recoverable before rethrowing. Failures before snapshot storage do not do so.
4. Normal successful detachment does not become recovery-eligible automatically; this preserves the existing persistence handoff gate and prevents recovery from racing the first persistence attempt.

## Risks / Trade-offs

- [Risk] A cancellation intent can remain pending if terminal work fails before snapshot storage. → It is deliberately not included in recovery scanning, and the original failure remains observable for the existing retry/ownership behavior.
- [Risk] Recovery may run after a terminal cleanup exception. → It uses the existing exact character, session-generation, receipt, and persistence revision checks.

## Migration Plan

No data or deployment migration is required. Deploy the service and run the focused logout tests plus the relevant GameWorld/integration/build validation.

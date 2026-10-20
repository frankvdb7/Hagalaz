## Context

`AuthenticationService.SignOutAsync` owns the request timeout and normal persistence/session handoff. `CharacterLogoutService` schedules terminal work on the shared GameWorker scheduler. The scheduled work can outlive the `DetachAsync` wait, so request-side `finally` logic cannot be the only owner of recovery eligibility.

## Goals / Non-Goals

**Goals:**

- Keep normal logout recovery hidden until the normal terminal handoff actually fails or is canceled.
- Preserve cancellation intent on the specific terminal transition, so a failed pre-snapshot attempt cannot poison a later retry.
- Make snapshot establishment and promotion of that intent one state-lock operation.
- Give the normal continuation and recovery scanner mutually exclusive, atomic claims for one stored snapshot.
- Reject a new sign-out while recovery owns the pending snapshot, rather than allowing a second forced persistence command.
- Preserve original terminal exceptions and existing persistence/session idempotency.

**Non-Goals:**

- Do not change the 30-second sign-out timeout.
- Do not cancel the shared GameWorker task after terminal work has been scheduled.
- Do not add a new worker, queue, retry mechanism, or general logout state machine.
- Do not change map-region or CodeQL behavior.

## Decisions

1. `CharacterLogoutService.DetachAsync` records a canceled wait against the current terminal transition in the existing `CharacterLogoutState`. It does not mark an absent snapshot recoverable.
2. `CharacterLogoutState.SetSnapshot` promotes the current transition's cancellation intent to `RecoveryAvailable` while holding the existing state gate. The completion identity prevents an old failed attempt from changing a later retry.
3. `Normal` and `RecoveryAvailable` are the only unclaimed continuation states. `TryClaimNormalContinuation` changes the former to `NormalClaimed`; `TryClaimRecovery` changes the latter to `RecoveryClaimed`. All competing claimants observe the state transition under the same lock.
4. `TryBeginLogout` rejects a new sign-out for a character whose snapshot is recovery-available or recovery-claimed. This prevents a second request from issuing another forced persistence command while recovery owns the handoff.
5. If the scheduled terminal task fails after the snapshot is stored, the task marks the already-valid logout recoverable before rethrowing. Failures before snapshot storage do not do so.
6. Normal successful detachment does not become recovery-eligible automatically; this preserves the existing persistence handoff gate and prevents recovery from racing the first persistence attempt.

## Risks / Trade-offs

- [Risk] A cancellation intent can remain pending if terminal work fails before snapshot storage. → It is scoped to the failed terminal transition and is not copied into a later retry.
- [Risk] Recovery may run after a terminal cleanup exception. → It uses the existing exact character, session-generation, receipt, and persistence revision checks.
- [Risk] Recovery may be scanned by more than one worker. → Only the state-gated `RecoveryAvailable` to `RecoveryClaimed` transition performs work; a failed claim is a no-op and a failed owner releases the claim for a later scan.

## Migration Plan

No data or deployment migration is required. Deploy the service and run the focused logout tests plus the relevant GameWorld/integration/build validation.

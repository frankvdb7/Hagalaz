## Why

When world admission registers a character and the commit then fails, rollback
currently waits for a GameWorker tick. During host shutdown that tick may never
arrive, leaving the session reservation and persistence lifecycle owned by the
terminating process. The fix must preserve worker-owned cleanup during normal
runtime without expanding the scheduler contract.

## What Changes

- Keep registered-character rollback on the existing GameWorker task during
  normal runtime.
- Add a terminal admission compensation path that waits for the actual
  GameWorker execution task to complete before mutating GameWorld state.
- Reuse exact-owner character, persistence, and session cleanup; retain
  recoverable state when external cleanup cannot complete.
- Add deterministic coverage for a rollback whose scheduled task never gets a
  tick because GameWorker execution has terminated.
- Do not change `IRsTaskService` or add lifecycle-specific scheduler APIs.

## Capabilities

### New Capabilities

### Modified Capabilities

- `game-world-sessions`: failed world admission must complete exact-owner
  rollback when host shutdown prevents a future GameWorker tick.

## Impact

- `Hagalaz.Services.GameWorld/Services/WorldSessionAdmissionService.cs`
- `Hagalaz.Services.GameWorld/Services/GameWorkerService.cs`
- `Hagalaz.Services.GameWorld/Startup.cs`
- `Hagalaz.Services.GameWorld.Tests/WorldSessionAdmissionServiceTests.cs`
- `Hagalaz.Services.GameWorld.Tests/GameWorkerServiceTests.cs`
- the existing `game-world-sessions` behavior specification and this change's
  OpenSpec artifacts
- no new package, worker, queue, or scheduler API; the implementation adds only
  a read-only worker-completion capability

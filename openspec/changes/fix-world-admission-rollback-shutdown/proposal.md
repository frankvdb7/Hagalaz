## Why

Failed world admission can enqueue registered-character rollback behind the
GameWorker and then wait forever when shutdown removes the only future tick
executor. The rollback must have a terminal cleanup outcome across the worker
running, stopping, and stopped states.

## What Changes

- Add a narrow lifecycle-critical scheduling contract for rollback work.
- Drain only lifecycle-critical rollback actions at the GameWorker shutdown
  boundary; ordinary gameplay tasks remain queued and are not executed.
- Provide a serialized terminal handoff for lifecycle-critical actions scheduled
  after the worker has completed shutdown.
- Preserve rollback ordering: exact character removal, destruction, persistence
  release, and session/claim cleanup.
- Add deterministic admission and scheduler regression tests.
- Update the game-world session specification with the terminal rollback
  requirement.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `game-world-sessions`: Failed world-admission rollback must reach a terminal
  cleanup outcome when GameWorker execution becomes unavailable.

## Impact

Affected production boundaries are `IRsTaskService`, `RsTaskService`,
`GameWorkerService`, and `WorldSessionAdmissionService`. Existing scheduler
callers and ordinary task execution remain unchanged. No new dependency,
persistence schema, worker, or recovery store is introduced.

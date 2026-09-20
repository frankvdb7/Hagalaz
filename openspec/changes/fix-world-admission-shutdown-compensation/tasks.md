## 1. Worker completion boundary

- [x] 1.1 Add the smallest GameWorker execution-completion contract, complete only when `ExecuteAsync` has returned, and expose the same worker instance through DI.
- [x] 1.2 Remove the `ApplicationStopped` admission dependency while preserving normal GameWorker rollback and exact-owner session cleanup.

## 2. Regression coverage

- [x] 2.1 Cover terminal fallback after actual worker completion without a scheduler tick.
- [x] 2.2 Cover callback ownership before worker completion and the queued-callback-after-completion race.
- [x] 2.3 Cover an executing callback delaying actual worker completion and prove no duplicate direct cleanup occurs.
- [x] 2.4 Cover the worker completion signal and its shutdown/exception behavior.

## 3. Specification and validation

- [x] 3.1 Validate the updated OpenSpec change with `openspec validate fix-world-admission-shutdown-compensation --type change --strict`.
- [x] 3.2 Run focused worker/admission tests, the full GameWorld test project, affected builds, and `git diff --check`.

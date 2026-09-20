## 1. Lifecycle contract

- [x] 1.1 Add the narrow lifecycle-critical scheduler contract and implement atomic running/stopping/stopped acceptance with serialized terminal handoff; verify ordinary task scheduling remains unchanged.
- [x] 1.2 Integrate GameWorker shutdown commencement and completion with the scheduler; verify only lifecycle-critical work is drained.

## 2. Admission compensation

- [x] 2.1 Route registered-character rollback through lifecycle-critical scheduling while preserving exact removal, destruction, persistence release, and session cleanup ordering.
- [x] 2.2 Add deterministic admission tests for accepted-before-stop, scheduling-after-stop, and healthy rollback; verify no test relies on sleeps or a post-stop normal tick.

## 3. Specification and validation

- [x] 3.1 Update the current game-world session specification and validate the change strictly.
- [x] 3.2 Run focused admission, scheduler, and GameWorker tests, the full GameWorld test project, the solution build, and `git diff --check`.

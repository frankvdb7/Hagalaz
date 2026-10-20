## 1. Terminal detach ownership

- [x] 1.1 Record cancellation against the specific terminal transition without marking a missing snapshot recoverable.
- [x] 1.2 Atomically promote a canceled terminal transition to recovery availability when the final snapshot is stored, and preserve availability when cleanup fails after snapshot storage.
- [x] 1.3 Add mutually exclusive normal-continuation and recovery claims under the existing logout-state lock.
- [x] 1.4 Reject a competing sign-out while recovery owns the snapshot, preventing a second forced persistence command.
- [x] 1.5 Release a recovery claim after a failed or canceled recovery attempt without clearing unrelated state.

## 2. Regression coverage

- [x] 2.1 Cover recovery ownership versus a second `SignOutAsync`, with exactly one persistence/session/completion path.
- [x] 2.2 Cover competing recovery operations, with exactly one recovery claim.
- [x] 2.3 Cover terminal failure before snapshot storage followed by a successful retry, with no stale recovery ownership.
- [x] 2.4 Cover cancellation after snapshot publication and cancellation while destruction is blocked.

## 3. Validation

- [x] 3.1 Run focused logout tests, the GameWorld test project, relevant integration tests, the affected build, `git diff --check`, and strict OpenSpec validation.

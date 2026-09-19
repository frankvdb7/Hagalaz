## 1. Terminal detach ownership

- [x] 1.1 Record cancellation of the terminal-detach wait in the existing logout state without marking a missing snapshot recoverable, and verify the focused logout regression test covers it.
- [x] 1.2 Atomically promote a canceled terminal transition to recovery eligibility when the final snapshot is stored, and preserve eligibility when cleanup fails after snapshot storage.

## 2. Regression coverage

- [x] 2.1 Add a deterministic cancellation-before-worker regression that runs the existing recovery path to completion and fails against the previous implementation.
- [x] 2.2 Cover terminal failure before snapshot storage and verify it is not incorrectly recovery-eligible.

## 3. Validation

- [x] 3.1 Run focused logout tests, the GameWorld test project, relevant integration tests, the affected build, `git diff --check`, and strict OpenSpec validation.

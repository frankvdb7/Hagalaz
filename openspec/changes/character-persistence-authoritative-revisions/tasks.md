## 1. Shared contracts and authoritative revision state

- [x] 1.1 Add the persisted snapshot revision to the Characters hydration response, GameWorld hydration saga/message, and `CharacterModel`; initialize and clean up per-character revision state during world sign-in only after successful character removal.
- [x] 1.2 Replace wall-clock revision generation with monotonic per-character allocation owned by `CharacterPersistenceState`; remove the obsolete generator and registration.
- [x] 1.3 Add the shared canonical snapshot fingerprint helper and extend persistence response/acknowledgement contracts with `Committed`, `Duplicate`, and `Conflict` outcomes that have no successful missing-value default.

## 2. Persistence and conflict handling

- [x] 2.1 Add `SnapshotFingerprint` to the character entity and EF model with a migration, update migration metadata/assertions, and preserve the existing concurrency token.
- [x] 2.2 Update GameWorld persistence state/service and logout acknowledgement flow so only matching committed or exact-duplicate outcomes become persisted, missing/unknown outcomes fail closed, and conflicts retain pending state.
- [x] 2.3 Update the Characters consumer and legacy request path to apply higher revisions, acknowledge exact duplicates, and report conflicts without mutating the character graph; preserve outbox, retry, and EF reset behavior.
- [x] 2.4 Update the dehydration state-machine success path and persistence documentation so conflict or missing/unknown outcomes cannot be reported as successful dehydration/logout.

## 3. Regression coverage

- [x] 3.1 Add GameWorld tests for hydration-seeded revision allocation, skew/rollback/restart/migration behavior, failed-removal revision retention, correlation-matched pending acknowledgement, missing/unknown outcome rejection, pending conflict state, and logout completion rules.
- [x] 3.2 Add Characters unit/harness tests for fingerprint storage, exact duplicate delivery, equal/obsolete conflicts, and no mutation on conflict.
- [x] 3.3 Update Characters MySQL integration tests for outbox acknowledgements, concurrent EF retries, conflict outcomes, fingerprint persistence, and failed-commit rollback.

## 4. Validation

- [x] 4.1 Run focused Characters, GameWorld, and data integration tests serially and fix all regressions. Docker-dependent MySQL tests were attempted but could not start because Docker is unavailable in this environment.
- [x] 4.2 Run the solution build, strict OpenSpec validation, and final clean-diff review while preserving unrelated worktree changes.

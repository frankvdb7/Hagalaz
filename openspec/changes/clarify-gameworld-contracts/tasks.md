# Tasks

## 1. Baseline and tests

- [x] 1.1 Record warning-level InspectCode and solution build baselines. Confirm at least 100 unique in-scope GameWorld diagnostics.
- [x] 1.2 Add and run characterization tests for representative GameWorld APIs and partial types before changing production source.

## 2. Refactor

- [x] 2.1 Fix stale XML comments and remove repeated base declarations from GameWorld partial types. Preserve API signatures and the base type for each class.
- [x] 2.2 Review the diff and confirm it contains only the planned documentation, declaration, and test changes.

## 3. Verify

- [x] 3.1 Run focused tests and the GameWorld test project with bounded non-coverage execution.
- [x] 3.2 Repeat the solution rebuild and warning-level InspectCode run. Confirm at least 100 additional warnings resolved and report any new diagnostics.

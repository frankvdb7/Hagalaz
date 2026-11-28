# Tasks

## 1. Baseline and tests

- [x] 1.1 Confirm the warning inventory using the pinned InspectCode and build settings.
- [x] 1.2 Use Rider to locate existing coverage. Add and run GameWorld characterization tests against unchanged production code and capture the deterministic GameWorld assembly baseline.

## 2. Cleanup

- [x] 2.1 Remove at least 500 confirmed warning instances from GameWorld source files. Check the diff for unrelated changes.

## 3. Verification

- [x] 3.1 Run the focused tests and GameWorld test project and verify equivalence of the production assembly.
- [x] 3.2 Repeat the solution rebuild and warning-level InspectCode run. Verify at least 500 additional diagnostics disappeared and check for new warnings.

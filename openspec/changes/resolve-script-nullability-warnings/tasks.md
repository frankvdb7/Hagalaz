# Tasks

## 1. Record the baseline and add tests

- [x] 1.1 Record the clean rebuild diagnostics by source location and diagnostic ID.
- [x] 1.2 Use Rider to find existing coverage. Add tests for duel rule clicks and widget closure, trading input and cleanup, and character design opening and submission.
- [x] 1.3 Run new characterization tests and relevant existing regressions against unchanged production code.

## 2. Refactor script state

- [x] 2.1 Consolidate duel rule click handling and resolve nullable duel participant, widget, and input references.
- [x] 2.2 Resolve nullable trade session and input references while preserving acceptance and settlement ordering.
- [x] 2.3 Resolve character design widget initialization diagnostics.

## 3. Verify

- [x] 3.1 Rerun focused tests and the Game Scripts test project.
- [x] 3.2 Rebuild the solution with the baseline settings. Confirm at least 100 unique source diagnostics have disappeared and review any new warnings.
- [x] 3.3 Review the diff for behavior changes, diagnostic suppressions, and unrelated edits.

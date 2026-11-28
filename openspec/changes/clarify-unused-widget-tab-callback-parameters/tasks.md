# Tasks

## 1. Baseline and tests

- [x] 1.1 Refresh build and Rider inspection baselines. Confirm at least 100 in-scope tab callback warnings.
- [x] 1.2 Trace the affected handlers and add focused characterization tests. Run them against unchanged production code.

## 2. Refactor

- [x] 2.1 Express unused callback parameters as discards while preserving callback signatures and widget behavior.
- [x] 2.2 Review the diff to confirm that used arguments, registrations, and handler bodies are preserved.

## 3. Verify

- [x] 3.1 Run focused and full Game Scripts tests without coverage.
- [x] 3.2 Repeat the solution rebuild and Rider inspection. Confirm at least 100 additional warnings were removed and investigate new diagnostics.

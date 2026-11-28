# Tasks

## 1. Baseline and characterization

- [x] 1.1 Refresh the solution-wide Rider report and verify at least 500 distinct redundant import diagnostics qualify in the two test projects.
- [x] 1.2 Add gameplay characterization cases in both projects and verify they pass before removing any imports.

## 2. Cleanup

- [x] 2.1 Remove at least 500 confirmed redundant namespace directives and verify the diff contains only those deletions and the test-first changes.
- [x] 2.2 Rerun the focused tests and both affected suites without coverage and with a hang timeout.

## 3. Solution verification

- [x] 3.1 Rebuild the solution, repeat the Rider inspection with identical settings, and verify at least 500 fewer distinct warnings. Review and report any introduced diagnostics.

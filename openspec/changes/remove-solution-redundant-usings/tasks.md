# Tasks

## 1. Baseline

- [x] 1.1 Capture and count the solution-wide Rider redundant-using warnings, verifying at least 500 candidates.
- [x] 1.2 Record the baseline and select ordinary namespace directives, excluding aliases, static imports, and conditional regions.

## 2. Cleanup and verification

- [x] 2.1 Remove at least 500 selected imports and verify the source diff contains only those removals.
- [x] 2.2 Rebuild the solution and repeat the Rider inspection. Verify the warning reduction and review new diagnostics.

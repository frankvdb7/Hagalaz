# Proposal

## Why

The previous cleanup removed 103 unique source diagnostics and left 474 in the solution rebuild. A further reduction of 500 requires a baseline that includes Rider inspections as well as build diagnostics.

## What Changes

- Resolve at least 500 additional, distinct C# warning instances across the solution.
- Prioritize nullability, resource and asynchronous work ownership, obsolete APIs, and redundant code.
- Add and run characterization or regression tests before editing the corresponding production code.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

C# production code and its existing test projects. Preserve existing runtime behavior and public contracts. Measure against the current working tree so the previous 103 fixes are excluded.

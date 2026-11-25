# Proposal

## Why

The latest Game Scripts inspection reports 90 redundant imports and 33 invalid XML comments. The comments include stale references that make script APIs harder to review.

## What Changes

- Remove at least 100 additional Game Scripts warnings by eliminating confirmed redundant imports and repairing XML documentation.
- Add and run characterization tests before editing production code.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

Game Scripts source and its test project. Preserve script behavior and public signatures. The starting point includes the fixes already in PR #711.

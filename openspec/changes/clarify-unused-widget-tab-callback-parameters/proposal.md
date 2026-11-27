# Proposal

## Why

The latest Rider report flags 243 unused parameters in the Game Scripts tab widgets. Many click handlers accept a common delegate signature but only use a subset of its inputs.

## What Changes

- Resolve at least 100 additional Rider warnings by making intentionally unused tab callback parameters explicit.
- Add and run focused characterization tests for the affected handlers before editing production code.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

Game Scripts tab widgets and their existing test project. Preserve click mappings, callback behavior, and delegate signatures.

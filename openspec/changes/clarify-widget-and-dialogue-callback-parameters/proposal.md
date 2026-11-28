# Proposal

## Why

The latest Rider report identifies 686 unused lambda parameters across Game Scripts widgets, dialogues, and area scripts. Discards make it clear which inputs each callback consumes.

## What Changes

- Resolve at least 500 additional Rider warnings by replacing confirmed unused callback parameters with discards.
- Add and run characterization tests through registered callbacks before editing production code.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

Game Scripts widgets, dialogues, area scripts, and their existing test project. Preserve registration IDs, delegate signatures, click and input handling, and side effects.

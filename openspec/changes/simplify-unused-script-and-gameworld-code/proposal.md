# Proposal

## Why

Rider reports unused locals and redundant assignments in GameWorld and Game Scripts. Removing these makes it easier to see which values each operation actually uses.

## What Changes

- Resolve at least 100 additional warnings involving unused locals, redundant assignments, and redundant defaults.
- Add and run focused characterization tests before changing the corresponding production code.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

GameWorld and Game Scripts C# source and their existing test projects. Preserve side effects, initialization order, and public signatures.

# Proposal

## Why

Rider reports 135 unused callback inputs in Game Scripts tab widgets after the completed SpellBook and Options cleanup. Explicit discards make it easier to see which inputs each handler consumes.

## What Changes

- Resolve at least 100 additional unused-parameter warnings in tab widget callbacks.
- Add and run representative tests through registered callbacks before changing production code.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

Game Scripts tab widgets and their existing test project. Preserve delegate signatures, registration IDs, callback results, and side effects.

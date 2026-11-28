# Proposal

## Why

The compiler reports 105 nullability warnings across the duel arena, trading, and character design scripts. Making their lifecycle contracts explicit will make it easier to spot when a widget, callback, or session can be absent.

## What Changes

- Add characterization tests before changing production code.
- Resolve at least 100 unique source diagnostics through accurate nullability contracts, checked state, and removal of repeated duel rule handlers.
- Preserve player-visible behavior and verify the warning reduction with matching rebuilds.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

The refactor covers GameWorld scripts for duel setup, trade interactions, and the character design widget, plus their tests. Existing gameplay requirements and widget protocol IDs apply.

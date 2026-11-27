# Proposal

## Why

The current solution Rider inspection reports 557 redundant namespace imports. Removing the confirmed directives reduces inspection noise and keeps source imports aligned with actual references.

## What Changes

- Remove at least 500 ordinary namespace using directives that the refreshed Rider report confirms are redundant.
- Preserve aliases, static imports, conditional compilation, symbol resolution, and executable behavior.
- Rebuild the solution and repeat the Rider inspection with the same settings.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

The cleanup is limited to C# source and test files identified by Rider. It introduces no runtime or wire-contract changes.

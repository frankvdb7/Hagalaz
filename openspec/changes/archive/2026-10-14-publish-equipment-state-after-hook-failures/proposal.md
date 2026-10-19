# Proposal: Publish Equipment state after hook failures

## Why

Equipment storage commits before lifecycle hooks run, so a hook failure must not leave the committed equipment state invisible to clients. The previous behavior attempted Equipment publication after the hooks even when one or more hooks failed.

## What Changes

- Preserve Equipment's attempt-all lifecycle-hook behavior.
- Attempt committed Equipment publication after lifecycle hooks, even when hooks fail, then propagate the hook failures.
- Keep generic transaction and publication failure semantics unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`

## Impact

- `EquipmentContainer` completion behavior and focused GameWorld regression tests.
- Canonical item-container storage specification; no public API or dependency changes.

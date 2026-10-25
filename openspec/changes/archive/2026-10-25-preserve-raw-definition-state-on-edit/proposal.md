# Proposal

## Why

The Cache service's object-name editor loads definitions through the gameplay provider, whose post-decode hook derives values that were absent from the stored definition. Encoding that runtime object during an otherwise narrow name edit can persist those derived values and change cache behavior.

## What Changes

- Decode the mapped object member directly with its codec before applying the name edit, without invoking gameplay post-decode hooks.
- Validate the encoded replacement by decoding it before persisting through the existing `ReplaceArchiveMember` boundary.
- Add codec round-trip and HTTP integration regressions for omitted fields and gateway-derived runtime values.

## Capabilities

### New Capabilities

- `cache-definition-mutations`: Editing an object name preserves stored values for fields not intentionally changed.

### Modified Capabilities

None.

## Impact

Affected code: Cache service object-name mutation endpoint and Cache service integration tests. Existing object providers and gameplay hooks remain unchanged. No cache API, storage format, dependency, or synchronization mechanism is added.

## Scope Boundary

This change covers object-name edits only. It does not redesign definition editing, change missing-definition behavior, alter other mutation endpoints, or broaden archive read/write atomicity.

## Acceptance Criteria

1. Object-name mutation decodes the original mapped physical member without applying gameplay post-decode hooks.
2. An edit changes the object name and preserves omitted/default encoded fields, including gateway-related values.
3. The encoded member is decoded successfully before it is passed to `ReplaceArchiveMember`.
4. A real HTTP integration test proves the stored member retains its original raw state after a name edit.

## Stop Conditions

Stop if the mapped member cannot be resolved without changing the existing `ReplaceArchiveMember` contract or if the fix requires provider or gameplay-hook changes.

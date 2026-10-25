# Proposal

## Why

The Cache service edits one field by decoding and re-encoding a complete NPC or object definition. The codecs recognize additional stored values that are not all represented by the gameplay-facing properties, so an otherwise narrow edit can silently change the definition. A read-only audit of the local revision-742 cache also found lossy round trips for Latin-1 names, one NPC spawn direction, and weighted object animations.

## What Changes

- Preserve the recognized NPC and object opcode payloads that were previously consumed and discarded.
- Preserve the source animation weights separately from the normalized values exposed to runtime code.
- Make the shared cache string writer symmetric with the existing single-byte string reader.
- Decode NPC mutation sources from their stored member bytes, without gameplay post-decode hooks.
- Add codec and Cache service regressions, including definitions captured from the revision-742 cache.

## In Scope

Preserve unrelated stored values during the existing NPC combat-level and object-name mutation operations.

## Non-Goals

Do not add generic unknown-opcode preservation, new mutation endpoints, or a replacement for the existing archive persistence operation.

## Capabilities

### Modified Capabilities

- `cache-definition-mutations`: Narrow edits preserve the other stored NPC and object definition values.

## Impact

Affected code: NPC and object codecs and models, cache string encoding, Cache service NPC mutation, codec tests, and Cache service integration tests. The existing archive replacement operation remains the persistence owner. Config and varp-bit codecs were audited against the local cache and require no change.

## Acceptance Criteria

1. NPC and object decode–encode–decode cycles retain the recognized payloads currently discarded by their codecs.
2. NPC names containing existing single-byte non-ASCII cache characters, NPC spawn direction 7, and object animation weights retain their decoded meaning.
3. NPC combat-level and object-name HTTP mutations preserve those values in the persisted archive member.
4. Representative revision-742 NPC and object definitions pass the codec regressions.

## Stop Conditions

Stop and split the work if preserving the identified values requires a generic unknown-opcode framework or changes the archive replacement contract.

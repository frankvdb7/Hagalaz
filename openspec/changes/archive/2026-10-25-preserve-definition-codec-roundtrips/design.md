# Design

## Context

Administrative type mutations decode a physical member, update one field, encode the full definition, then persist it through `ICacheAPI.ReplaceArchiveMember`. Definition codecs also normalize some fields for gameplay use and consume some recognized payloads without retaining them. Exact byte comparison is not a suitable safety check because NPC and object encoders intentionally emit canonical opcode order and representation.

## Goals / Non-Goals

**Goals:** Preserve the decoded source values that survive a definition edit, including the identified wire payloads and raw animation weights.

**Non-Goals:** Add generic unknown-opcode storage, redesign codecs, change object gameplay hooks, or move archive serialization out of `CacheApi`.

## Decisions

1. Keep the additional payloads on the existing concrete `NpcType` and `ObjectType` models. The codecs already own these cache format rules; explicit fields for the recognized opcode ranges are sufficient and avoid an extensible preservation framework.
2. Keep raw object animation weights beside the normalized `AnIntArray784` values. Runtime consumers keep the normalized representation while the encoder writes the original bytes.
3. Use Latin-1 for `WriteString`, matching the current reader's byte-to-character mapping and preserving existing cache names.
4. Decode NPC edits directly from the mapped stored member, as the object-name edit already does. This avoids applying gameplay hooks before persisting a source definition.
5. Preserve the existing `ReplaceArchiveMember` operation as the sole archive mutation boundary.

## Evidence and Verification

The local revision-742 cache contains 15,974 NPC definitions and 78,753 object definitions. A semantic property comparison found only the specific round-trip losses described in this change before the fixes and none afterward. Config and varp-bit definitions also completed a byte round-trip audit with no mismatches.

## Risks / Trade-offs

- [Codec model fields could be mistaken for gameplay values] → Keep raw protocol payloads separate and document their opcode ranges.
- [Future codec changes could add new discarded payloads] → Extend focused codec regressions when adding recognized opcode parsing; this change does not preserve arbitrary unknown opcodes.

## Migration Plan

No stored cache migration is required. Existing mutation endpoints continue using the same request and persistence contracts.

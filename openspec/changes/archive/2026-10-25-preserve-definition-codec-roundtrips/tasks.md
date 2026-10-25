# Tasks

## 1. Audit editable definition codecs

- [x] 1.1 Trace the NPC and object mutation paths and identify source values changed or discarded by full codec round trips.
- [x] 1.2 Compare decoded public definition properties across the local revision-742 NPC and object cache, and audit config and varp-bit definitions.

## 2. Preserve encoded definition values

- [x] 2.1 Retain the recognized NPC and object opcode payloads and raw object animation weights in their concrete models and codecs.
- [x] 2.2 Preserve single-byte cache strings and NPC spawn direction through encoding.
- [x] 2.3 Decode NPC mutation input from the stored member without gameplay post-decode hooks.

## 3. Verify mutation behavior

- [x] 3.1 Add codec round-trip regressions, including representative revision-742 NPC and object definitions.
- [x] 3.2 Add Cache service integration regressions for NPC combat and object name edits.
- [x] 3.3 Run focused codec, string, and Cache service tests and re-run the revision-742 semantic audit.

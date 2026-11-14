# Hagalaz domain glossary

## Cache

**Definition type**: a kind of cache definition (item, NPC, object, sprite, config definition, varp bit), each stored as members of archives in its own cache index.

**Type mutation**: changing one stored definition type entry in the cache. It loads the raw entry without type event hooks, applies the change (or takes a full replacement), checks that encode → decode → encode is byte-identical, skips the write when the result equals the entry's canonical encoding, and otherwise writes the archive member. An absent entry and an entry that cannot be decoded are reported as results, not exceptions. Owned by `TypeMutator<TType>` in `Hagalaz.Cache`.

**Canonical encoding**: `Encode(Decode(stored))` for a definition type entry. For items, NPCs, objects and sprites this differs from the stored bytes (opcode order, written defaults, sprite layout), so "unchanged" is judged against the canonical encoding, never the stored bytes.

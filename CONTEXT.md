# Hagalaz domain glossary

## Cache

**Definition type**: a kind of cache definition (item, NPC, object, sprite, config definition, varp bit, model, billboard, particle emitter, particle effector, material table, animation skeleton/frame, render animation), stored in its own cache index. In code a definition type is a *Type*: an `I…Type` interface in `Hagalaz.Cache.Abstractions` and a mutable `…Type` class in `Hagalaz.Cache/Types` holding exactly the stored fields, nothing derived.

**Codec**: the module in `Hagalaz.Cache/Logic/Codecs` that decodes a Type from its stored bytes and encodes it back, registered in `AddGameCache`. A codec is lossless: `Encode(Decode(stored))` reproduces the stored bytes, unless a format provably allows several layouts, in which case only encode → decode → encode must be stable. Data computed for rendering (generated texture UVs, resolved geometry) is not part of a Type and is derived from it outside the codec. Code that only converts data, such as texture pixels to PNG, is not a codec.

**Type mutation**: changing one stored definition type entry in the cache. It loads the raw entry without type event hooks, applies the change (or takes a full replacement), checks that encode → decode → encode is byte-identical, skips the write when the result equals the entry's canonical encoding, and otherwise writes the archive member. An absent entry and an entry that cannot be decoded are reported as results, not exceptions. Owned by `TypeMutator<TType>` in `Hagalaz.Cache`.

**Region map file**: one of a map region's stored files in cache index 5, named by kind and region coordinates: terrain `m{x}_{y}`, landscape `l{x}_{y}`, underwater terrain `um{x}_{y}` and underwater landscape `ul{x}_{y}`, where the region ID is `(x << 8) | y`. A region may lack any of them; in the local cache every landscape has terrain, but not every terrain has a landscape. Index 5 also holds NPC spawn files (`n{x}_{y}`), which are not region map files.

**Region map archive**: the single owner of the region map files: their names, their lookup, reading and decoding them, which read failures count as corrupt data, and the write rules: a file that is absent or cannot be decoded is reported as an error and never overwritten, a write is built from the stored file and stored with compare-and-swap (retrying when another write landed first), keeps the stored compression, is verified before writing, and is skipped when it equals the stored file's canonical encoding. Owned by `RegionMapArchive` in `Hagalaz.Cache`. Compare-and-swap keeps the readability check and the write together; it does not stop one editor's whole-landscape save from replacing another's.

**Region map revision**: the reference-table versions of a region's terrain and landscape files, read together from one table snapshot. Every cache write changes a file's version, so a changed revision means one of the region's map files was written.

**Canonical encoding**: `Encode(Decode(stored))` for a definition type entry. For items, NPCs, objects and sprites this differs from the stored bytes (opcode order, written defaults, sprite layout), so "unchanged" is judged against the canonical encoding, never the stored bytes.

# Companion client / pinned cache evidence — revision 742

**Source of truth:** the separately maintained [Hagalaz.GameClient repository](https://github.com/frankvdb7/Hagalaz.GameClient). Access to this repository may require authentication. The source files and documentation below were inspected through its GitHub connector on 15 October 2026; **the cache's LFS payload was not downloaded or decoded in this pass**.

## Existing pinned cache fixture

- [Fixture README](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/tests/fixtures/cache742/README.md) — `tests/fixtures/cache742/cache.zip`, tracked by Git LFS, with `cache.zip.sha256`.
- The repository documents archive length **607,410,012 bytes**, source data file size **655,853,516 bytes**, and **38** index-255 reference entries. This is metadata from the fixture README, **not a checksum independently verified here**.
- The archive contains the cache `dat2` and `idx*` files, not an authentication recording. It can be fetched in a trusted checkout with `git lfs pull --include="tests/fixtures/cache742/cache.zip" --exclude=""`.
- The client provides a read-only fixture inspection contract and live client/UI validation tasks. [Cache research workflow](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/cache/README.md).

## Cache location crosswalk (source documented)

The companion client's [cache index map](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/cache-index-map.md) identifies these top-level stores:

| Index | Semantics | Addressing | Feature relevance |
|---:|---|---|---|
| 3 | Widgets/interfaces | group = interface ID; file = component ID | All feature screens, component operations |
| 5 | Playable maps | named groups | Region geometry, objects, travel |
| 12 | CS2 executable client scripts | group = script ID; file 0, ordinary lookup | Challenge, bank, inventory, prayer, shop UI logic |
| 16 | World/location object definitions | group = ID >>> 8; file = ID & 255 | Mining, trees, farming, doors, transports |
| 18 | NPC definitions | group = ID >>> 7; file = ID & 127 | Slayer, quest dialogue, fishing, bosses |
| 19 | Item definitions | group = ID >>> 8; file = ID & 255 | Currency, equipment, crafting recipes |
| 20 | Animation sequences | group = ID >>> 7; file = ID & 127 | Skills, combat, teleport animations |
| 21 | Spot/graphic animations | group = ID >>> 8; file = ID & 255 | Effects, spells, bosses |
| 22 | Varbit definitions | group = ID >>> 10; file = ID & 1023 | Unlocks, quest stages, widget state |

The index map explicitly warns that descriptors for a definition family are not proof that the family resides in a guessed fixed group of index 2. Use the actual store and its decoder.

## Canonical protocol, interface and region evidence

- [Protocol](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/protocol/README.md) — **164 incoming server-to-client opcodes 0–163** and **108 outgoing client-to-server opcodes 0–107**. Read direction carefully: this naming is from the *client* point of view. Registry identification is not proof of exact payload layout.
- [Interfaces](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/interfaces/README.md) — index 3 binary format, component hierarchy, CS2 callbacks and context menu/click dispatch.
- [CS2](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/cs2/README.md) — index 12 scripts, VM and lossless encoding/decoding research.
- [Maps](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/maps/README.md) — index 5 terrain/landscape addressing and object-placement semantics.
- [Regions](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/regions/README.md) — normal/dynamic region loading, server packet boundaries and client scene acceptance.

## Repeatable extraction plan (per feature)

1. Fetch the pinned LFS archive in the companion repository and verify its checked-in SHA-256. Record commit hash, cache checksum, client checksum and build-date uncertainty.
2. Read the canonical cache source locator and inspection contract. Use a deterministic read-only inspector to enumerate definition IDs from the exact stores above. Export stable JSON/CSV with `index,group,file,id,raw_fields,decoded_fields,source_hash`.
3. For world objects/NPCs, join definitions to map spawn data and the Hagalaz server's EF repositories; do not infer coordinates from wiki prose.
4. For a widget, extract index-3 component hierarchy and index-12 referenced scripts, then trace option dispatch to its client outbound opcode and the server's revision-742 decoder/handler.
5. Record observed preconditions, animations, config updates, item and XP changes, error messages and success timing using deterministic client/server tests.
6. Compare this direct evidence against `docs/revision-742/reference/` and `docs/revision-742/data/`, correcting secondary rows that conflict with the actual version.
7. Track completeness per feature separately for **cache data**, **historical mechanics**, **server ownership**, **wire/UI evidence**, **content scripts**, **tests** and **persistence**. No feature is complete merely because its IDs were exported.

## Scope and safety

Do not commit decrypted secrets, account files, production keys, user cache profiles or captured credentials. The pinned fixture README states these are excluded from the sanctioned LFS archive. Run client tests against the approved fixture; do not silently regenerate or replace it. Use the companion repository's existing inspection/deobfuscation tooling rather than creating a parallel custom archive parser for Hagalaz.

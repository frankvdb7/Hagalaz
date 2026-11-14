# Proposal

## Why

Six Cache API endpoints change stored definition types (item, NPC, object, config definition, varp bit, sprite). Each wires three loose helpers in `TypeMutationUtilities` its own way, and the copies have drifted:

- There are two load paths. Item, NPC and object read the raw entry. Config and varp bit load through `provider.Get`, which runs type event hooks.
- There are three verify rules. Sprite writes without checking. Item checks inline. The rest only check that the encoded bytes decode.
- Missing and unreadable entries surface as whatever exception the chosen path throws.
- The persistence code has no tests.

Nothing owns the type mutation rule, so every new definition-type edit has to re-learn it.

**Goal:** one Cache library module owns type mutation for every definition type, and every type-mutation endpoint behaves the same way for loading, verifying, skipping no-op writes, and reporting missing or unreadable entries.

## What Changes

- Add `TypeMutator<TType>` to `Hagalaz.Cache`. It is one concrete module per definition type, registered in `AddGameCache`, with three operations:
  - **Read:** loads the raw entry without type event hooks.
  - **Mutate:** loads, applies a change, verifies, and writes.
  - **Replace:** verifies and writes a whole new definition for an existing entry.
- **Verify rule for every mutation:** encode, decode, then encode again must produce identical bytes. A mismatch is a codec defect: the module throws and nothing is written.
- **No-op edits:** when the new encoding equals the stored entry's canonical encoding (`Encode(Decode(stored))`), nothing is written.
- **Results:** the module reports outcomes through FluentResults. `TypeAbsentError` covers a missing entry and `TypeUnreadableError` covers a stored entry that cannot be decoded. Endpoints map them to HTTP 404 and HTTP 422 the same way for every type.
- **Endpoints moved onto the module:**
  - item definition update and item definition read;
  - NPC combat level;
  - object name;
  - config definition update;
  - varp bit update;
  - sprite image replace.
- Config and varp bit stop loading through `provider.Get` for mutations.
- **Sprite image replace becomes checked:** it is now verified, and it is rejected with 404 for a sprite ID that has no stored entry.
- **Removal:** delete `TypeMutationUtilities`. Its multipart image parsing moves into the sprite endpoint owner, its only caller.

**Reused, not added:**
- FluentResults (already used by `Hagalaz.FluentResults`, `Hagalaz.Game.Abstractions` and `Hagalaz.Services.Characters`);
- the existing codecs and `ITypeData` registrations;
- `ICacheAPI.ReplaceArchiveMember` and its write lock;
- the existing 404/422 problem responses.

No new worker, store, queue or lock.

### Non-goals

- A compare-and-swap check against concurrent edits of different fields on the same type. No current endpoint edits only some of a type's fields. The module's interface is where it would be added later.
- Item duplication (`AppendArchiveMemberCopy`). Choosing a free ID is a different operation.
- Moving the map writers' `MapArchiveUpdateResult` onto FluentResults, or any other map archive work.
- Changing what the codecs write. Item, NPC and object encoding already adds zero-valued opcodes and reorders opcodes relative to stored bytes.
- New mutation routes, DTO changes, or frontend changes.

### Acceptance criteria

1. Every listed endpoint changes the cache only through `TypeMutator<TType>`, and `TypeMutationUtilities` no longer exists.
2. Mutating an item writes back no hook-derived fields (note, lend, bought).
3. For every listed mutation route, a missing type entry returns HTTP 404 and an undecodable stored entry returns HTTP 422. Neither changes the cache.
4. An encoding that is not byte-stable through encode → decode → encode is never written.
5. An edit whose encoding equals the canonical encoding performs no cache write.
6. Sprite image replace is verified and returns 404 for a missing sprite.
7. Existing routes, JSON contracts, validation and authorization are unchanged, and existing tests pass.

### Stop conditions

- **A codec is not byte-stable on real data.** If an existing definition-type codec turns out not to be byte-stable through encode → decode → encode on real cache data, stop and propose a codec fix as a separate change. A check against the real cache found all six codecs stable on every decodable entry.
- **Partial edits on the same type need protection.** If implementation needs a compare-and-swap check to keep edits of different fields on the same type from overwriting each other, stop and propose it separately.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-definition-mutations`: adds one consistent contract for every definition-type edit: hook-free load, round-trip verification, no-op write skipping, and uniform missing/unreadable responses. Sprite image replacement joins these rules.

## Impact

- **`Hagalaz.Cache`:**
  - new `TypeMutator<TType>` and its errors;
  - FluentResults package reference;
  - `AddGameCache` registrations for the six definition types.
- **`Hagalaz.Services.Cache`:**
  - the item, NPC, object, config, varp bit and sprite endpoint owners;
  - `TypeMutationUtilities` deleted.
- **Tests:**
  - new module tests in `Hagalaz.Cache.Tests`;
  - 404/422 endpoint tests in `Hagalaz.Services.Cache.Tests`;
  - the image-parsing tests move with that code to the sprite endpoint.
- **HTTP behaviour:**
  - undecodable stored entries now return 422 consistently; today some cases give 400 or 500, for example a decode `ArgumentOutOfRangeException`;
  - no-op saves no longer bump the cache version.

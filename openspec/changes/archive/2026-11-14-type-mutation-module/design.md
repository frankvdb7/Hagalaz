# Design

## Context

See proposal.md, section "Why". The current state that shapes the approach:

- **Reading a member:** `TypeMutationUtilities.DecodeRawTypeEntry` resolves the member ID to its physical position through the reference table, then calls `ICacheAPI.Read(index, archive, position)` and decodes it with the codec. No event hooks run.
- **Writing a member:** `TypeMutationUtilities.PersistTypeEntry` calls `ICacheAPI.ReplaceArchiveMember`. That call holds the cache write lock, rebuilds the archive, and throws `FileNotFoundException` when the member does not exist.
- **Codecs:** each definition type has one, registered in `AddGameCache`:
  - `IItemTypeCodec`, `INpcTypeCodec`, `IObjectTypeCodec` and `ISpriteTypeCodec`, all derived from `ITypeCodec<T>`;
  - `ITypeCodec<IConfigDefinition>`;
  - `ITypeCodec<IVarpBitDefinition>`.
- **`ITypeData` classes** (for example `ItemTypeData`) carry the index and the archive arithmetic.
- **Codec stability:** a check against the local game cache found encode → decode → encode byte-stable for all six codecs on every decodable entry. The checked entries:

  | Type | Entries |
  |---|---|
  | Item | 25,467 |
  | NPC | 15,974 |
  | Object | 78,753 |
  | Config | 2,808 |
  | Varp bit | 12,467 |
  | Sprite | 15,173 |

  Stored bytes equal the canonical encoding only for config and varp bit definitions. Item, NPC and object codecs reorder opcodes and write defaults, and the sprite codec re-lays out frames. Sprite 1455 fails to decode.
- **Error responses:** `EndpointErrors.MapException` already maps `FileNotFoundException` to 404 and `InvalidDataException` and `NotSupportedException` to 422. Other decode exceptions (`ArgumentOutOfRangeException`, `EndOfStreamException`) fall through to 400 or 500.

## Goals / Non-Goals

**Goals:**
- One owner for the type mutation rule. See the delta spec for the observable behaviour.
- A module interface small enough that endpoints pass only an ID and a change.

**Non-Goals:**
- Changing `ICacheAPI`.
- Changing how definition types are read for the game (`ITypeProvider<T>` and its hooks stay as they are).

## Decisions

### `TypeMutator<TType>` owns type mutation in `Hagalaz.Cache`

The class is concrete and generic over the definition-type interface (`IItemType`, `INpcType`, and so on). It is built from `ICacheAPI`, `ITypeCodec<TType>` and `ITypeData`, and registered once per type in `AddGameCache` (item, NPC, object, config definition, varp bit, sprite).

Operations:

- `Result<TType> Read(int id)`: resolves the member, reads it, and decodes it with no hooks.
- `Result Mutate(int id, Action<TType> change)`:
  1. Read the entry.
  2. Compute its canonical encoding.
  3. Apply `change`.
  4. Encode and verify.
  5. If the result equals the canonical encoding, return success without writing.
  6. Otherwise call `ReplaceArchiveMember`.
- `Result Replace(int id, TType type)`:
  1. Check the member exists.
  2. Encode and verify.
  3. Compare with the stored entry's canonical encoding when it decodes, and skip the write if equal.
  4. Otherwise write.

`Replace` does not fail on an undecodable stored entry, because it never needs the old contents. It skips the no-op comparison instead.

The member ID is resolved to its physical position through the reference table inside the module. Callers never see positions or archive arithmetic.

Decoded types that implement `IDisposable` (sprites) are disposed by the module once they have been encoded.

**Rejected alternatives:**
- **Keep the static helpers and fix each endpoint.** This keeps three loose functions that each endpoint must combine correctly, which is how the paths drifted.
- **Put the module in `Hagalaz.Services.Cache`.** Loading, verifying and writing members are cache rules. The map writers already live in `Hagalaz.Cache`.
- **Add an `ITypeMutator<T>` interface.** Only one adapter exists, so the seam would have nothing behind it. Endpoint tests use the real mutator over a fake `ICacheAPI`.

### Verify by exact round trip

The rule is `Encode(t) == Encode(Decode(Encode(t)))`, and on failure the module throws `InvalidOperationException`. This works the same for every type without a per-type comparer.

**Rejected alternatives:**
- **"Decodes without error"** (today's check). This misses lossy encodings.
- **Per-type `PatchIsExact` comparers** (as the map writers use). These need one comparer per type for no extra guarantee, since every codec is byte-stable.

### No-op test uses the canonical encoding

An edit is unchanged when its encoding equals `Encode(Decode(stored))`.

**Rejected alternative:** comparing with the stored bytes. That would treat every first edit of an item, NPC, object or sprite as a change.

### Outcomes as FluentResults errors

`TypeAbsentError` and `TypeUnreadableError` derive from FluentResults' `Error`.

- **Absent:** the reference table has no entry for the member. The module checks this before reading or writing.
- **Unreadable:** any exception other than `OperationCanceledException` thrown while decoding the stored entry. This covers `ArgumentOutOfRangeException`, as seen for sprite 1455.

Endpoints map absent to 404 and unreadable to 422, using `EndpointErrors.Validation`-style problem details. A failed verify stays an exception, so the existing exception handler returns 500.

**Rejected alternatives:**
- **An enum like `MapArchiveUpdateResult`.** The user chose the Result pattern.
- **A custom Result type.** FluentResults is already the repo's Result library, so a second one would duplicate it.

`Hagalaz.Cache` takes a package reference to FluentResults 4.0.0, the version the repo already uses. The repo does not centrally pin package versions.

### State transitions and ownership

- **One write per mutation.** The only state transition is the member write, and `ReplaceArchiveMember` owns it under the existing cache write lock.
- **Single owner of mutation behaviour.** `TypeMutator<TType>` is the only place that does loading, verifying and skipping no-ops for mutations.
- **No retries or reconciliation.** There is no retry loop: last writer wins for concurrent full edits of the same type, as today. See proposal.md, section "Non-goals".

### Endpoint changes

- **What endpoints keep:** validation and response DTOs.
- **What they inject:** `TypeMutator<TType>` instead of codec, `ITypeData` and `ICacheAPI`. Config and varp bit no longer inject their provider for updates.
- **Item update:** passes a change that casts to `ItemType` and applies the DTO.
- **Item definition GET:** uses `Read`.
- **Sprite image replace:** builds the new `SpriteType` and calls `Replace`.
- **Image parsing:** `ReadImageRequestStreamAsync` moves into `SpriteDefinitionsEndpoints`, and its tests move with it.

## Risks / Trade-offs

- **Hook-free reads change what the item GET returns.** The GET already reads raw, so this is not new for items. Config and varp bit edits now skip their providers, and neither type has a hook, so the decoded values are the same. → Covered by a module test that item mutations persist no hook-derived fields.
- **No-op detection costs an extra encode per edit** (the canonical encoding of the stored entry). → Negligible for single-definition admin edits.
- **Undecodable entries can no longer be edited.** An edit of an undecodable entry used to fail with an arbitrary status and now fails with 422. Replace still works for undecodable sprites. → Intended. The spec requires it.

## Migration Plan

This is an in-process refactor with no data migration. Rollback is a revert of the change.

# Tasks

## 1. Type mutation module in Hagalaz.Cache

- [x] 1.1 Add a FluentResults 4.0.0 package reference to `Hagalaz.Cache` and add `TypeAbsentError` and `TypeUnreadableError` (FluentResults `Error`). Verify that `dotnet build Hagalaz.Cache` succeeds and that `packages.lock.json` is updated.
- [x] 1.2 Implement `TypeMutator<TType>.Read`: it resolves the member through the reference table and decodes with no hooks; a missing member gives `TypeAbsentError`, and any decode exception other than cancellation gives `TypeUnreadableError`. Verify with `Hagalaz.Cache.Tests` cases for present, absent and undecodable entries (mocked `ICacheAPI`).
- [x] 1.3 Implement `Mutate`: read, take the canonical encoding, apply the change, encode, verify the round trip, skip the write when the result equals the canonical encoding, otherwise call `ReplaceArchiveMember`. Verify with tests:
  - a change is written;
  - an unchanged edit writes nothing, including when the stored bytes differ from the canonical encoding;
  - an unstable encoding throws and writes nothing;
  - absent and unreadable entries write nothing;
  - an item edit persists no note, lend or bought values derived by `ItemTypeEventHook`.
- [x] 1.4 Implement `Replace`: check the entry exists, encode, verify, skip the write when the result equals the stored entry's canonical encoding (only if the stored entry decodes), otherwise write; dispose `IDisposable` types. Verify with tests:
  - a replacement is written;
  - a missing entry gives `TypeAbsentError` and writes nothing;
  - an undecodable stored sprite is still replaced;
  - an identical replacement writes nothing.
- [x] 1.5 Register `TypeMutator<TType>` in `AddGameCache` for item, NPC, object, config definition, varp bit and sprite. Verify with a test that resolves all six from a service provider built by `AddGameCache`.

## 2. Endpoints use the module

- [x] 2.1 Move the item definition update and the item definition GET to `TypeMutator<IItemType>`, mapping `TypeAbsentError` to 404 and `TypeUnreadableError` to 422. Verify that the existing item endpoint and mutation tests pass unchanged.
- [x] 2.2 Move NPC combat level, object name, config definition update and varp bit update to their `TypeMutator<TType>`, with the same 404/422 mapping; config and varp bit stop using `provider.Get` for updates. Verify that the existing `cache-definition-mutations` tests (object name, NPC combat level) pass.
- [x] 2.3 Move the sprite image replace to `TypeMutator<ISpriteType>.Replace` with the 404 mapping, and move `ReadImageRequestStreamAsync` and its two tests into the sprite endpoint owner. Verify that the moved tests pass.
- [x] 2.4 Delete `TypeMutationUtilities` and remove its `using static` imports. Verify by grepping that no references remain and that `dotnet build Hagalaz.sln` succeeds.
- [x] 2.5 Add `Hagalaz.Services.Cache.Tests` endpoint tests: one mutation route returns 404 for a missing type entry and 422 for an undecodable entry, and neither writes. Verify that the tests pass.

## 3. Integration check

- [x] 3.1 Run `dotnet test` for `Hagalaz.Cache.Tests` and `Hagalaz.Services.Cache.Tests`, check the diff for leftover helpers or new warnings, and confirm acceptance criteria 1–7 in proposal.md are met.

## Workflow follow-up

- Sync the `cache-definition-mutations` delta into `openspec/specs/` and archive the change after review.

# Proposal

## Why

After the snapshot migration, the generic and sprite single-definition providers can return a factory fallback when the reference table has no parent archive entry. Their previous `Get` paths read the parent archive first and surfaced `FileNotFoundException`. Generic `GetRange` still surfaces that error, so the same missing definition can produce a fallback for an individual request and an error for a range request.

## What Changes

- Preserve the missing-parent archive failure for `TypeProvider<T, TD>.Get` and `SpriteTypeProvider.Get` without reconstructing the complete archive.
- Keep factory fallback when a parent archive exists but the requested child mapping is absent.
- Keep the existing missing-parent and missing-child fallback behavior for the item, NPC, and object providers that use the shared child decoder directly.
- Add provider regressions and update the cache snapshot specification.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-archive-member-snapshots`: specify provider behavior for missing parent archives separately from missing child mappings.

## Acceptance Criteria

- Generic `Get` and `GetRange` throw `FileNotFoundException` when the reference table has no parent archive entry for the requested type.
- Sprite `Get` throws `FileNotFoundException` for an absent parent archive.
- Generic and sprite providers still return factory fallbacks when the parent archive exists but the child mapping is absent.
- Item, NPC, and object providers retain their existing fallback for absent parent or child mappings.
- Mapped member reads continue to use snapshot-backed `ICacheAPI.Read` and propagate missing-file or decode failures.
- Strict OpenSpec validation and focused cache tests pass.

## Non-goals and Stop Conditions

- Do not restore complete-archive reads for generic or sprite single-definition requests.
- Do not change specialized item, NPC, or object provider fallback behavior.
- Do not add boolean policy flags, exception frameworks, new cache infrastructure, or endpoint-specific logic.
- Do not change cache error mapping, custom-provider delegation, hooks, or range algorithms.

# Design

## Context

The Cache service registers quest, animation, and graphic providers through `TypeProvider<T, TD>`. Their individual endpoints call `Get`; range endpoints call `GetRange`. `CacheApi.ReadArchive` rejects a missing parent archive with `FileNotFoundException`, which the endpoint maps to 404. Before the snapshot migration, generic and sprite `Get` called `ReadArchive` before checking the child mapping. The snapshot path now checks the child first, so an absent parent currently returns a factory-created definition.

The item, NPC, and object provider implementations already used mapping-first fallback behavior before the snapshot migration. They now call the shared child decoder directly and must keep that behavior.

## Goals / Non-Goals

**Goals:**

- Make generic and sprite individual reads agree with their historical missing-parent contract and the generic range path.
- Keep snapshot-backed reads for mapped physical members.
- Preserve child-missing fallbacks and the specialized item, NPC, and object contracts.

**Non-Goals:**

- Reconstruct a complete archive on the single-definition path.
- Change range algorithms, custom provider delegation, or cache exception mapping.

## Decisions

1. **Separate the required-parent entry path from the existing fallback path.** Generic and sprite `Get` use a named helper that reads the reference table once, requires the parent archive entry, then delegates to the same child decoder. The existing `DecodeFromCache` entry point remains mapping-first for item, NPC, and object providers.

2. **Pass the already-read table into child decoding.** This keeps the parent check and child lookup on the same memoized table result and avoids a second lookup. It adds no coordination or cache state.

3. **Keep snapshot-backed member reads.** Once both parent archive and child mapping exist, the shared decoder continues to call `ICacheAPI.Read(indexId, archiveId, physicalMemberIndex)`. Missing or corrupt mapped data continues to propagate from the cache API.

4. **Keep range behavior unchanged.** Generic `GetRange` already calls `ReadArchive` before checking children, so it continues to throw for a missing parent and return child fallbacks for a missing child in an existing archive.

## Risks / Trade-offs

- **[Risk]** A missing parent archive could be an incomplete cache rather than an intentionally absent definition. **Mitigation:** Restore the pre-migration generic and sprite `Get` behavior; child absence inside an existing archive still returns a factory fallback.
- **[Risk]** Tightening the shared child decoder would change item, NPC, and object behavior. **Mitigation:** Keep its existing fallback entry point and use a separate named path only for generic and sprite providers.

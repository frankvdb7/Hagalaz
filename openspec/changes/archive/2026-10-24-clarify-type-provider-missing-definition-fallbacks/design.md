# Design

## Context

See `proposal.md` for the behavior question and `specs/cache-archive-member-snapshots/spec.md` for the clarified contract. `TypeProvider.DecodeFromCache` checks the logical reference-table child before calling `ICacheAPI.Read`; `CacheApi.Read` remains responsible for physical archive/member reads and archive decoding.

## Goals / Non-Goals

**Goals:** Preserve missing-definition factory fallback for absent logical mappings, while keeping mapped storage and decode failures visible.

**Non-Goals:** Change production provider or cache APIs, range-read behavior, hook frequency, or the archive snapshot implementation.

## Decisions

1. **Keep mapping-first single-definition reads.** An absent parent archive entry and an absent child entry both mean there is no logical definition to decode, so the existing provider factory fallback remains. Eagerly reading a complete archive before checking the mapping would reintroduce archive reconstruction on the optimized path and would not be needed to establish the intended contract.

2. **Do not convert mapped read failures into fallbacks.** Once a logical child is mapped, `ICacheAPI.Read` owns physical validation and decoding. Its missing-file and corrupt-data exceptions propagate through the provider; tests will assert the provider does not mask them.

3. **Test the boundary with existing components.** Use real `ReferenceTable` and `ReferenceTableEntry` objects to represent absent parent and child mappings, and mock only the `ICacheAPI` boundary to assert the provider's direct call/exception behavior. Existing cache API tests cover physical missing-member and corrupt-archive failures.

4. **Keep test resource ownership explicit.** Dispose the temporary `MemoryStream` created by the raw-mutation integration store helper after its synchronous write copies the bytes.

## Risks / Trade-offs

- [A missing logical archive entry may represent an incomplete cache as well as an intentionally absent definition] → Preserve the existing factory fallback contract for unmapped definitions; mapped-but-missing storage remains an error.
- [Mocked cache failures alone would not prove the cache can produce those failures] → Keep the production trace explicit and retain cache API tests for missing and corrupt archive data.

## Migration Plan

No runtime migration is required. The change clarifies behavior and adds regression tests without modifying production code.

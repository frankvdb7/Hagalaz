# Design

## Context

See `proposal.md` for the production path and `specs/cache-definition-mutations/spec.md` for the behavior contract. Gameplay providers run post-decode hooks; archive mutation already belongs to `ICacheAPI.ReplaceArchiveMember`.

## Goals / Non-Goals

**Goals:** Keep the object editor's source of truth at the mapped physical member bytes, then validate and persist the changed definition through the existing cache API.

**Non-Goals:** Change gameplay provider decoding, add cache synchronization, change the archive replacement API, or migrate other mutation endpoints.

## Decisions

1. Resolve the object archive and logical child through `ObjectTypeData` and the cached reference table, then read the mapped physical member with `ICacheAPI.Read`. Decode it directly with `IObjectTypeCodec`; do not call `ITypeProvider<IObjectType>.Get`, because that invokes gameplay post-decode hooks.
2. Change only `Name`. Encode the result, decode the encoded bytes once to reject invalid output before persistence, then pass those bytes to `ReplaceArchiveMember`. The cache API remains the owner of archive rebuild, locking, and snapshot invalidation.
3. Keep the endpoint explicit and callback-free. Do not introduce an editing service or a second archive persistence path for this single operation.

## Risks / Trade-offs

- [A missing logical member has no stored definition to preserve] → Let the existing cache lookup and replacement failures surface; do not synthesize an editable definition.
- [A second edit can overlap the raw member read and replacement] → Keep the existing operation-level consistency boundary; broader read-modify-write atomicity is outside this change.

## Migration Plan

No data migration or operational change is required. Existing stored definitions can be edited through the same endpoint; only unrelated fields stop acquiring runtime-derived values during the edit.

# Design

## Context

See `proposal.md` for the production stale-read path and `specs/cache-archive-member-snapshots/spec.md` for the behavior contract. The Cache service currently injects `IFileStore` into raw mutation handlers, while `CacheApi` owns the same singleton store, snapshot cache, writer lock, and reference-table provider.

## Goals / Non-Goals

**Goals:** Make both existing raw mutation routes participate in the cache owner's local write ordering and invalidate the cached state affected by the raw target.

**Non-Goals:** Observe direct store writes outside `CacheApi`, refresh other processes, update container versions or reference-table metadata for raw writes, or make a type read atomic across its separate logical-map and physical-member API calls.

## Decisions

1. **Keep mutation ownership in `CacheApi`.** Add raw write and copy operations to the existing `ICacheAPI` contract, and have the routes call them. These operations still call `IFileStore` directly and preserve the current input bytes and copy behavior. Routing through `CacheApi.Write` is rejected because it changes container versions and reference-table metadata. A new invalidation service or event mechanism is unnecessary because the endpoints are in the same process and `CacheApi` already owns the relevant state.

2. **Reuse the existing reader/writer lock and snapshot cache.** Hold `_snapshotReadWriteLock` exclusively over the raw store operation and its `finally` invalidation, matching `CacheApi.Write`. Cache API reference-table lookups take its read lock while the provider loads or returns a table, so a cold pre-write load cannot finish and repopulate the memo after a raw table invalidation. For ordinary data files, invalidate the exact `(indexId, fileId)` snapshot. For a target `(255, indexId)`, clear that memoized reference table and remove every retained snapshot for `indexId`; a changed table can alter both member count and logical-to-physical mapping. The existing cache state lock remains internal to `ArchiveSnapshotCache`.

3. **Let `ReferenceTableProvider` own memo eviction.** Add an index-specific invalidation operation to `IReferenceTableProvider` and implement it by clearing that provider's cached entry. No second table cache or synchronization primitive is introduced. `CacheApi` calls it only for a valid data index targeted as a raw reference table.

4. **Keep endpoint validation and results unchanged.** Base64 validation and endpoint response shapes remain in the handlers. The raw API methods accept the decoded stream or copy source identifiers and preserve current byte-for-byte writes; they do not encode containers or mutate metadata.

## Risks / Trade-offs

- [A raw write can partially alter the store before throwing] → Invalidate affected in-process cached state in `finally`, and allow the original store exception to propagate.
- [A table lookup may already have returned the old mapping before a raw write begins] → The writer waits for the lookup, then invalidates the memo; a `Get` already between logical lookup and member read may finish using its captured mapping, while later calls refresh.
- [A reference table can change while a caller is between logical lookup and member read] → The endpoints provide sequential post-mutation freshness; making those two existing API calls one atomic logical read would change the provider contract and remains outside this fix.
- [A direct store write or another process still bypasses this owner] → Keep the existing restart requirement for unsupported external mutations.

## Migration Plan

No data migration is required. Existing routes keep their paths, request/response shapes, and raw byte semantics. Restarting the Cache service is unnecessary for writes issued through these routes after deployment; external or direct store edits still require restarting affected readers.

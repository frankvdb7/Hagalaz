# Centralize reference-table memoization in CacheApi

## Why

The cache snapshot owner and the reference-table memo owner are currently split between `CacheApi` and `ReferenceTableProvider`. This makes write freshness depend on a public provider invalidation method and lets provider-level memoization duplicate state ownership. The Cache service's raw write and copy endpoints use the same `ICacheAPI` instance as type reads, so its existing write lock is the correct owner for local state refresh.

## What Changes

- Make `ReferenceTableProvider` decode each reference table on demand without retaining state or exposing an invalidation API.
- Move bounded per-index reference-table memoization into `CacheApi`, coordinated by its existing reader/writer lock.
- Clear affected memoized tables and archive snapshots after supported local writes, including on write failure.
- Keep `CacheWriter` on its independent provider decode and preserve raw byte semantics.
- Stream raw copies directly from the source store stream to the destination store.
- Correct the living cache specifications and remove the unrelated Angular test hunk from this PR.

## Acceptance Criteria

- `IReferenceTableProvider` exposes only `ReadReferenceTable`; each provider call returns a newly decoded table and releases temporary resources on success or failure.
- Concurrent cold `CacheApi` reads decode once per index; different indices can load concurrently; warm reads do not call the provider; failed loads retry.
- CacheApi write invalidation happens under its writer lock. High-level writes invalidate the affected ordinary-index table memo and exact snapshot even when the writer throws. Raw ordinary writes/copies invalidate only the destination snapshot. Raw reference-table writes/copies invalidate that table memo and all snapshots for its index.
- The real `CacheWriter` persists updated archive and reference-table metadata while using a table object independent from CacheApi's memo.
- Existing #506 snapshot behavior, type-provider behavior, sparse reference-table correctness, same-service raw mutation behavior, and GameWorld override isolation remain intact.
- The unrelated Angular assertion hunk is restored to its base version.

## Non-goals

- No new invalidation service, event bus, generic cache, public maintenance method, or second coordination mechanism.
- No changes to sparse-ID representation, type-provider fallback contracts, range/archive read atomicity, or cross-process/direct-store coherence.
- No TTL, definition-object caching, or unrelated admin-webapp fix.

## Runtime Boundary

The guarantee covers writes issued through the same `CacheApi` instance as reference-table and single-member reads. External processes and direct `IFileStore` writes outside that instance remain outside automatic coherence.

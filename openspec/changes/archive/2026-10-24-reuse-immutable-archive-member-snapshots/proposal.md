# Proposal

## Why

GameWorld's measured single-definition path reconstructed 260 distinct index-16 archives 19,967 times, spending about 3.1 seconds decompressing and 0.8 seconds splitting archive members in one login. Reusing immutable member bytes at the cache boundary addresses that repeated work while keeping each type decode fresh.

## What Changes

- Add bounded, per-`CacheApi` retention of packed archive-member snapshots for single-member reads.
- Share concurrent cold reconstruction for the same archive and order those reads against local `CacheApi.Write` calls.
- Move generic and specialized single-definition provider reads to the existing physical-member `ICacheAPI.Read` API.
- Preserve mutable detached `ReadArchive` results and existing sequential range paths.
- Measure actual snapshot cold loads and cache activity through the existing cache metrics.

## Capabilities

### New Capabilities

- `cache-archive-member-snapshots`: Read archive members from bounded immutable snapshots with fresh streams, same-archive cold-load sharing, and local write invalidation.

### Modified Capabilities

None. Revision-742 archive decoding behavior is unchanged.

## Impact

Affected code is limited to `Hagalaz.Cache`, its cache/provider tests, and the cache options/metrics. The public `ICacheAPI` signatures remain unchanged. Existing `ArchiveDecoder`, container decoding, reference-table validation, dependency injection options binding, and `CacheApi.Write` remain the source mechanisms.

## Non-goals and Stop Conditions

- Do not include issue #497 async I/O, FileStore changes, cross-process invalidation, TTL, definition-object caching, HybridCache changes, NPC/region-loading work, or provider range rewrites.
- Keep returned `ReadArchive` objects mutable and detached; snapshots serve only single-member reads.
- Stop and record a follow-up if correctness requires a second write-coordination mechanism, a public test seam, or changes outside the cache/provider boundary.

## Acceptance Criteria

- Single-member reads reuse private immutable member bytes, return fresh read-only streams, and share a concurrent same-archive cold load.
- Retention has a validated configurable byte budget, LRU eviction, and no permanent retention for zero-capacity or oversized snapshots.
- `CacheApi.Write` excludes member reads and invalidates the affected snapshot even when the writer throws; external/raw writes remain outside the coherence guarantee.
- Generic and specialized single-item providers use physical member indices without changing fresh decode, factory fallback, or hook behavior.
- Focused cache/provider tests, full GameWorld tests, solution tests/build, strict OpenSpec validation, and diff checks pass. A real runtime A/B is reported only if it can be run in this environment.

# Proposal

## Why

The Cache service's raw write and copy routes mutate the same `IFileStore` used by its singleton `CacheApi`, but currently bypass snapshot invalidation. A retained type archive can therefore serve old definition bytes after a successful in-process endpoint mutation; replacing a raw reference table can also leave `ReferenceTableProvider` using its memoized mapping.

## What Changes

- Route the raw write and copy operations through the existing `CacheApi` owner while preserving the exact raw bytes and avoiding container-version or reference-table metadata updates.
- Serialize raw mutations and Cache API reference-table loads with the existing snapshot reader/writer lock. Invalidate the exact archive snapshot after ordinary raw mutations and, for a raw reference-table file, invalidate the memoized table and all snapshots belonging to its data index.
- Add service-level regression coverage that reads a type, performs each raw mutation route in the same app instance, and reads the updated type.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-archive-member-snapshots`: include supported same-process raw endpoint mutations in the local freshness guarantee and define reference-table refresh behavior.

## Impact

Affected code: `ICacheAPI`, `CacheApi`, `ArchiveSnapshotCache`, `ReferenceTableProvider`, the Cache service mutation endpoints, and their tests. No dependencies or new coordination mechanism are needed.

## Acceptance Criteria

- Raw write and copy routes preserve their current byte-for-byte behavior and do not use `CacheApi.Write`.
- A type read following a raw archive write or copy in the same service instance observes the replacement archive.
- A type read following a raw write to `(255, indexId)` observes the refreshed logical-to-physical mapping and archive layout.
- Raw mutation invalidation occurs before snapshot reads resume, including when the underlying write throws.

## Non-goals and Stop Conditions

- Do not add distributed or external-process invalidation, an event bus, a generic invalidation framework, or a second lock/cache.
- Do not change high-level `CacheApi.Write` container-version or reference-table update semantics.
- Stop if satisfying these criteria requires coordination outside the Cache API/provider boundary or changing the type-provider read contract.

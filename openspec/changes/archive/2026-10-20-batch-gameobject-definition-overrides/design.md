# Design

## Decision

Resolve the distinct object-definition IDs known by `MapRegionLoader` for one region. The loader already has the complete set after decoding static map placements and reading database spawns, so batching belongs at this boundary. Do not introduce a caller-facing preload operation or load unrelated metadata rows.

## Flow

1. `MapRegionLoader` collects distinct IDs from static and database-spawned placements.
2. `IGameObjectService.FindGameObjectDefinitionsByIdsAsync` returns definitions keyed by requested ID. The cache decorator deduplicates and uses a stable key derived from the sorted IDs; repeated requests for the same set share one cached result.
3. On a cold batch, `GameObjectService` asks `GameObjectDefinitionRepository` for only the non-negative requested IDs. The query projects only `gameobject_id`, `examine`, and `gameobject_loot_id` with `AsNoTracking`.
4. The service composes each requested ID from the revision-cache archive definition plus the optional database `Examine` and `LootTableId` overrides. An absent database row retains archive behavior.
5. The loader supplies each resolved definition to the existing builder. Other builder callers and `FindGameObjectDefinitionById` keep their current single-ID behavior and per-ID cache key.

Empty input returns an empty dictionary without SQL. Cancellation flows into the repository query. Region placement, scripts, replacement behavior, lifecycle, and login ordering remain unchanged. No locking or shared DbContext concurrency is introduced; each region batch uses the existing scoped service/repository path, and EF executes one `IN` query for the requested IDs.

The cache stores only the batch result for those requested IDs through the existing HybridCache, GameObject tag, and configured expiration. A deterministic sorted-ID cache key avoids a second cache service or global snapshot. The independent single-ID cache remains for unrelated callers.

## Validation

Unit tests cover de-duplication, exact requested IDs, correct archive/override composition, missing rows, empty input, batch cache reuse, single-ID compatibility, builder-provided definitions, and cancellation. A MySQL 8.4 integration test verifies requested-only filtering, selected columns, empty input, and query count. The representative viewport remains 9 regions, 28,607 placements, and 2,429 distinct IDs; one cold bulk operation per populated region replaces per-distinct-ID lookups.

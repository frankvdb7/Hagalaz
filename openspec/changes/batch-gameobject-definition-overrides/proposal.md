# Bulk-resolve requested GameObject definitions

## Goal

Keep cold visible-region definition lookup bounded by batching only the distinct IDs needed by each region, without a caller-facing preload or whole-table metadata cache.

## In scope

- Add a filtered bulk repository query for requested `gameobject_definitions` IDs.
- Add a caller-facing service operation that resolves requested definitions keyed by ID while keeping cache behavior inside the service decorator.
- Have `MapRegionLoader` resolve one distinct ID set per populated region and feed the definitions into the existing builder.
- Preserve the existing single-ID service and builder paths for unrelated callers.
- Add focused and real-MySQL query-count coverage.

## Non-goals

- Changing visible-region loading, world-login ordering, `Character.OnRegistered`, or region scheduling.
- Changing object placement, script selection, loot/examine precedence, packet or client behavior.
- Adding background warming, another cache service, distributed locks, manual invalidation, or a long-lived global snapshot.
- Changing unrelated persistence fixes, Notes, or other existing dirty working-tree changes.

## Acceptance criteria

- A region cold batch queries only its distinct requested definition IDs in one repository query, not one query per object or per ID.
- Duplicate IDs are resolved once; empty input issues no definition query.
- Repeating an identical sorted ID set reuses the existing HybridCache result.
- The public service exposes definitions keyed by requested ID and no preload/warm-all API.
- The single-ID lookup remains available and cached for unrelated callers.
- Archive definitions remain authoritative, with only existing database metadata overrides applied; absent rows retain archive behavior.
- Static and database-spawned placements receive the corresponding definition without changing their placement or script construction.
- Cancellation reaches the filtered EF query.
- Login ordering and unrelated persistence/Notes changes remain unchanged.

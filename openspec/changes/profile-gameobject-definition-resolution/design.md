## Context

The existing `MapRegionLoader.Load` activity already records one activity per actual region and the GameWorld OpenTelemetry setup subscribes to `Hagalaz.Services.GameWorld` and EF Core command instrumentation. The bulk resolver currently crosses a HybridCache decorator, a service that queries metadata and resolves archive definitions, and an EF repository.

The GameObject type provider is transient and decodes a requested definition in `Get`. It reads a reference table that `ReferenceTableProvider` memoizes, then calls `CacheApi.ReadArchive`; that method reads and decodes the archive for the request. The measured bulk cache may serialize through the configured FusionCache System.Text.Json serializer and registered distributed cache. Those costs need separate measurements.

## Goals / Non-Goals

**Goals:**

- Attribute each bulk request to key preparation, cache API/factory, repository, archive/type-provider, codec, and composition work.
- Distinguish a cache result from a factory execution without logging keys or IDs.
- Keep measurement cardinality bounded by bulk calls, not requested definitions.

**Non-Goals:**

- Changing cache keys, cache granularity, cache lifetime, archive loading, or definition semantics.
- Changing region scheduling, NPC viewports, sign-in ordering, persistence, or client behavior.
- Adding a profiler service, custom cache, global ID set, or high-cardinality key fingerprint.

## Decisions

1. **Reuse the existing GameWorld activity source.** Add a shared internal diagnostics helper using the already-subscribed `Hagalaz.Services.GameWorld` source. Keep the existing region-load activity as the parent and add only bulk-level activities for full resolution, cache access, core resolution, and repository access.

2. **Measure key work as scalar data on the bulk activity.** Time the current `Distinct`, `OrderBy`, array, and key-string construction. Record only the deduplicated count and elapsed time. Do not attach the key, IDs, or a key hash.

3. **Use factory execution as the cache outcome signal.** On a successful HybridCache call, classify the result as a hit when its factory did not run and a miss when it did. Record the cache-call activity duration, factory duration, and elapsed time from factory completion until the cache call returns. On exceptions or cancellation, record an error/cancelled outcome rather than a hit.

4. **Measure repository work around the complete EF method.** The repository activity spans query construction, execution, materialization, and return. Existing EF command spans remain the SQL-only measurement nested under it.

5. **Aggregate archive and codec work without per-definition activities.** In the core resolution activity, accumulate elapsed time around `ITypeProvider<GameObjectDefinition>.Get` calls and codec decoding. The provider measurement includes reference-table access, archive read/container/archive decode, entry extraction, type decode, and hooks; the codec timer separates the object-definition decoder portion. Count archive fallbacks through the registered GameObject-definition factory only while the core resolution activity is current.

6. **Measure composition and collection conversion separately.** Record aggregate elapsed time for core deduplication/DB-ID preparation, metadata override application, core dictionary insertion, and the cached result's final interface dictionary materialization. Do not enumerate the input solely to measure it.

7. **Keep diagnostic overhead bounded.** No activity is created per definition. Per-definition timers run only while the core activity is recording, and all exported attributes are counts, outcomes, or elapsed values without IDs.

Alternatives rejected: a per-definition span or ID/key fingerprint would add unnecessary cardinality and expose identifiers; measuring only the EF child span would miss client-side provider, cache, and composition time; changing cache design before phase measurements would be speculative.

## Risks / Trade-offs

- **Tracing adds small work in the synchronous archive loop.** Gate aggregate timing on an active recording activity and use scalar accumulation only.
- **A cache hit may come from an earlier request, not necessarily a repeated key in this particular login window.** Report hit/miss counts, but leave exact repeated-key and global-ID-overlap counts unknown unless a safe aggregate can establish them without exporting identifiers.
- **Factory-executed distinguishes the cache path but does not label the internal distributed-cache tier.** Report it as a HybridCache hit/miss outcome, not as an L1/L2-specific result.
- **The current running binary cannot emit the new activities.** The user must stop Aspire, rebuild/start the normal runtime, reproduce one login, then stop interacting. No runtime lifecycle action is performed by this change.

## Migration Plan

No data or configuration migration is required. Build and test the instrumented GameWorld output. Then hand control to the user for the exact Aspire restart and one-login reproduction. Analyze the resulting traces before proposing any behavior or performance change. Remove the diagnostic code in a separate decision if the measurements show no useful follow-up.

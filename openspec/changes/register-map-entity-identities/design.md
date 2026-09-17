## Ownership boundary

`MapRegionPart` owns the shared collections that make ground items and game
objects observable. Its existing `Add` methods therefore register the exact
entity with the injected `IEntityStore` before publishing it through the
collection and spawn callback. Both runtime `MapRegionService` additions and
`MapRegionLoader` additions reach this same boundary through `MapRegion.Add`.

The entity classes remain unaware of the store. `MapRegionPart` owns terminal
removal for individual resources, while `MapRegion.Destroy` owns terminal
cleanup of all resources still owned by the region. Loader rollback retains
exact preparation references and unregisters them after cleanup because a
failed region must not use region-wide destruction and duplicate NPC teardown.

## Static object identity

Removing a static object from the active collection moves it to the existing
disabled-static collection without destroying or unregistering it. Re-adding
the same reference removes it from that collection, enables it, and keeps its
existing handle. Replacing it with a different object permanently destroys
and unregisters the disabled reference. Region destruction enumerates both
active and disabled static objects so no disabled handle survives unloading.

## Failure and stale-handle behavior

The existing `EntityStore` remains the sole source of handle assignment,
identity matching, slot reuse, and generation invalidation. Exact resource
removal is followed by store removal even when a resource destroy callback
throws. Loader rollback likewise removes only the exact resources created by
that load attempt. No per-entity state or parallel cleanup mechanism is
introduced.

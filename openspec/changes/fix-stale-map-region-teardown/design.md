# Design

`MapRegionService.FindMapRegion` already returns the canonical active or idle
region without creating or resuming one. It also treats a removed dimension as
an absent lookup result. The fix reuses that lookup for
operations whose target is existing state. A missing region is treated as a
harmless stale operation. An idle region remains idle while its retained state
is removed or updated.

`GetOrCreateMapRegion` remains the owner of legitimate creation and activation
paths such as adding world entities and coordinate-based collision creation.

`MapRegion` keeps exact-instance ownership for game objects. Removal unflags
collision only after the exact object is confirmed as current. Object collision
and existing-object update records are ignored when the referenced object is
not owned by the canonical region, preventing an old callback from affecting a
replacement at the same coordinates.

The regression tests use region removal, suspension, replacement instances,
and semantic client-update gates rather than sleeps.

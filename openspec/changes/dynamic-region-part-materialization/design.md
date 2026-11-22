# Design

## Context

See proposal.md (Why). Current owners:

- `MapRegionLoader.PrepareAsync` decodes the cache region (`IMapProvider.DecodeRegion`: object placements and blocked tiles, both already bridge-adjusted), reads database object spawns, resolves definitions, and builds static/non-static objects plus floor-block tiles. `ApplyPreparedRegion` flags floor blocks and calls `region.Add` (which flags object collision).
- `MapRegionService.CreateDynamicRegion` (async game task) awaits the source region through `MapRegionLoadScheduler`, revalidates it, calls `MakeStandard()` on it, then synchronously calls `WriteBlock` for 4x8x8 parts. The only production entry point is the `::tzhaar` admin command.
- `MapRegion.LoadPartObjectsCore` resolves the source region through `GetOrCreateMapRegion` (not awaited), copies live objects and raw collision. `MapRegionPart.Remove` turns removed statics into disabled-static entries.
- `RotateBlock*`, `DeleteBlock*`, `WriteBlock(partX…)` absolute wrappers, `LoadPartObjects`, `UnloadPartGameObjects` and `AddPartObjects` have no production callers outside `MapRegion`.

## Goals / Non-Goals

**Goals:** one template preparation path for standard loads and dynamic parts; synchronous part mutation on the GameWorker with all async work done before it; unambiguous local-index APIs.

**Non-Goals:** template caching, a world-wide template registry, client map resends, and any change to normal region residency.

## Decisions

### D1. `MapRegionTemplate` prepared by one owner

Add an immutable `MapRegionTemplate` (Abstractions, beside `IMapRegion`): region id, a list of object entries (id, region-local x/y/z, rotation, shape, definition, static flag) and the blocked floor tiles. Add `IMapRegionTemplateLoader.LoadAsync(regionId, ct)` in `Hagalaz.Services.GameWorld/Data`, built from the code currently in `MapRegionLoader.PrepareAsync` (cache decode, database object spawns converted to region-local coordinates, definition lookup). `MapRegionLoader` calls it and then builds objects from the entries, so standard loads stay behaviour-identical. NPC and ground-item spawns stay in `MapRegionLoader`.

The template loader is the single owner of "what a region's base map contains". It reads no live region and no dimension.

*Rejected:* reading the Ready source region (cannot tell configured non-static spawns from runtime objects, and runtime mutations leak in); a cached template store (no measured need; dynamic creation is rare, and caching adds eviction and lifetime questions).

### D2. Async preparation, synchronous materialization

`MapRegionService.CreateDynamicRegion` keeps its readiness path (await and revalidate the source; destination stays `Initializing` until population succeeds). After revalidation it reads the source's part descriptors by local index, loads the template of each distinct referenced source region id with `IMapRegionTemplateLoader`, then performs the synchronous writes. Part operations on `IMapRegion` take the resolved source template as a parameter:

- `WriteBlockByIndex(localX, localY, z, sourcePartX, sourcePartY, sourceZ, MapRegionTemplate source)`
- `RotateBlockByIndex(localX, localY, z, rotation, MapRegionTemplate source)`
- `MakeStandard(MapRegionTemplate own)`

`DeleteBlockByIndex(localX, localY, z)` needs no template and stays on `MapRegion` only, since nothing outside it deletes parts. Dynamic creation copies each source part's presence, source X/Y/plane and rotation, so a dynamic source chains correctly; parts created later in a dynamic region start absent.

The template's region id must match the descriptor's source region; a mismatch throws (programming error, no fallback).

The `MakeStandard()` call on the live source region is removed: for a standard source it only resets descriptors, and with D5 it would rebuild a live region. Reading a dynamic source's descriptors still works because descriptors are data, not content.

*Rejected:* hiding async template loading inside `MapRegion` (blocking `.Result` on the GameWorker) or inside a packet encoder.

### D3. Explicit coordinates

`GetRegionPartData(partX, partY, z)` is renamed `GetRegionPartByAbsolutePart`; `GetRegionPartByLocalPart` moves to `IMapRegion` and is what the service uses. Absolute-coordinate wrappers `WriteBlock`, `RotateBlock`, `DeleteBlock` and the public `LoadPartObjects`/`UnloadPartGameObjects`/`AddPartObjects` are removed (no callers). Rotate/delete use `LocalPartYToPartY` for Y. `DrawRegionDimension` and the `drawPartDimension` overload are removed because templates are dimension-independent (D1).

### D4. Materialization follows the client, bridges included

The client (`Map.readRegionPartLandscape`, `Class274.readGroundMapData`) selects a chunk's objects by their **raw** source plane and reads its terrain from the source plane, then drops objects and blocked tiles one plane where the **destination** tile's plane-1 terrain is a bridge. That flag comes from whichever chunk is drawn on destination plane 1 at that index.

So decoding keeps raw data next to the standard (bridge-adjusted) views: `DecodedMapRegion` and `MapRegionTemplate` carry raw object planes (`RawObjectPlacements` / `SourceObjects`) and raw terrain flags. The bridge rule lives once in `MapTerrain.GetCollisionPlane`; `MapCodec.ToCollisionPlanes` and the provider's blocked tiles use it for standard regions.

For a present part on destination plane `z` with source (sx, sy, sz, r):

1. Floor: each source tile on `sz` with the blocked flag is rotated (`CalculateRotationDelta`) and flagged `FloorBlock` on `GetCollisionPlane(z, bridge(dest tile))`, skipped below plane 0.
2. Objects: each source object on raw plane `sz` whose origin is in the source 8x8 is placed with `CalculateObjectRotationDelta`, rotation `(objectRotation + r) & 3`, on `GetCollisionPlane(z, bridge(dest tile))` for static cache objects and on `z` for configured spawns (they are not in the client map). Where the collision plane is below 0 the client still draws the object (`Map.addToClip` with null collision data), so the server keeps it on `z` without collision (`MapRegion.Add(obj, flagCollision: false)`). Such an object never displaces an object in its slot, and an object with collision silently replaces it, so the reachable bridge object wins regardless of order; plane 0 is built last in a column for the same reason. Standard templates mark these objects `HasCollision = false` for the loader.
3. `bridge(tile)` reads the plane-1 chunk at that tile's index through the inverse rotation; a missing part draws itself in a standard region and nothing in a dynamic one.

For a self-mapped region this equals the standard decoding, so `MakeStandard` matches `MapRegionLoader`.

### D5. Parts track what they built

Bridge objects and blocks land one plane down, and large rotated objects can root in a neighbouring column, so content is not owned by the part that stores it. Each part records the objects and floor tiles it built (`_partContent`, with `_objectOwners` mapping objects back), the columns whose plane-1 chunk it read bridge flags from, and the parts it beat for an object slot.

An edit rebuilds only:
- the edited parts;
- for an edited plane-1 chunk, every part that read the column's bridge flags and whose recorded 64-tile bridge mask differs from the new one (a rewrite that leaves the effective bridge flags unchanged rebuilds no readers);
- transitively, the parts those beat for a slot, which may get it back.

A part rebuilt only because something it depends on changed keeps its live objects: they stay registered while the rebuild runs, and each object the rebuild produces again (same template entry, rotation and shape, same slot and collision) reuses the existing instance, so removed statics, in-place transformations (`UpdateGameObject`) and other runtime state survive. Each object remembers what it was built from, because scripts change live objects in place. Objects removed at runtime and destroyed (configured spawns) are carried as per-part tombstones (slot, layer, template id) and not recreated, and a part beaten for a slot keeps a tombstone for its own removed object when the winner takes over. Leftover live objects that the rebuild no longer produces are discarded. An edited part resets all of this, since its content may differ.

Other parts keep their runtime state (removed statics, spawned objects). Discarding unflags a part's enabled objects, calls `MapRegionPart.DiscardObjects` on the parts storing them (no runtime removal updates) and clears its floor tiles. Runtime objects stored in a replaced part are removed with it. A column never built this way (a region loaded as standard) is rebuilt whole, giving up what its parts store that no part built. Surviving objects overlapping removed collision are re-flagged, in this and resident adjacent regions.

When two parts claim one object slot (tile, layer, plane), checked against enabled and runtime-disabled objects: an object with collision beats one without; otherwise the later part in the client's load order wins. `RegionManager.loadPartObjects` loops plane, then x, then y. Whether the client's scene keeps the later object in that slot was not confirmed (the scene code is obfuscated); the server holds one object per slot. A beaten object another part built is removed silently. A losing part rebuilt alone loses again to the unchanged winner, including one removed at runtime.

Selecting a part's source objects uses a per-template index by (plane, source part), built once on first use, instead of filtering every placement per part. Objects displaced by slot arbitration are collected during materialization and their collision footprints are re-flagged in one pass per plane after all parts are placed.

Edits are plain data: each write, rotate or delete computes the complete target descriptor of a part (source X/Y/Z, rotation, present or absent). The batch is validated against the prospective descriptors (templates, bridge masks) before anything is applied, then applied in one place, so no rollback is needed. Built objects are tracked in one registry (owner part plus the template entry they came from), and the reuse pool and displaced-object list live only for one rebuild pass. An object whose own rebuild no longer produces it as it was (for example it loses its collision under a new bridge) is replaced by the new object in the same slot, not kept out by its own stale copy.

`WriteBlocksByIndex` applies many mappings and rebuilds each affected part once; dynamic creation uses it so spawn hooks run only for final objects. Part operations take templates by region id for every source region the region draws, validated before anything is discarded. `MakeStandard(own)` maps every part to itself and rebuilds all 256 parts from `own`.

### D6. Presence

`HasDrawSource` remains the explicit presence flag; `Erase()` keeps clearing it. Nothing infers presence from `GetHashCode()`. The descriptor encoding stays where it is for #400.

## Risks / Trade-offs

- [A plane-1 change can move objects that other columns placed in it] → those columns are rebuilt with it; columns are found from what they recorded.
- [Unflagging overlapping collision can clear bits set by a neighbour object] → same behaviour as runtime removal today; footprints crossing part boundaries are re-flagged when that neighbour is rebuilt.
- [Template reads hit the cache and database per dynamic creation] → acceptable for an admin-triggered, rare operation; revisit with a cache only if measured.
- [Internal API break on `IMapRegion`/`IMapRegionPart`] → all callers are in this repository and updated in the same change.

## Migration Plan

No data migration. Rollback is a revert of the change.

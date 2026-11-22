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

### D4. Materialization

For a present part with source (sx, sy, sz, r) and destination (dx, dy, dz):

1. Source origin `((sx & 7) * 8, (sy & 7) * 8)` on plane `sz`; destination origin `((dx & 7) * 8, (dy & 7) * 8)` on plane `dz`.
2. Floor blocks: each template blocked tile on `sz` inside the source 8x8 is flagged `FloorBlock` at the destination origin plus `CalculateRotationDelta(localX & 7, localY & 7, r)`.
3. Objects: each template object on `sz` whose origin lies in the source 8x8 is built through `IGameObjectBuilder` with the template definition, at destination origin plus the existing `CalculateObjectRotationDelta` (footprint-aware, using the source rotation), rotation `(objectRotation + r) & 3`, same id and shape, static flag from the template, destination dimension, and added with `MapRegion.Add`, so collision comes from the existing wall/standard/floor-decoration flagging.

Raw collision is never copied, so directional flags are produced by the rotated object, not transformed bit by bit. The formulas in step 2 and 3 are the existing helpers, matching the client's dynamic chunk decoding.

### D5. Discarding old part content

Add `MapRegionPart.DiscardContent()` returning the removed objects: it clears enabled objects and disabled-static entries, destroys and unregisters them, and queues no update. `MapRegion` first unflags collision for each enabled object (existing `UnFlagCollision`), clears `FloorBlock` on the part's 64 tiles, then materializes. Runtime `Add`/`Remove` keep their disabled-static semantics. Ground items, NPCs and characters in the part are not touched.

`MakeStandard(own)` sets every part to self-mapped, present, rotation 0, discards and rematerializes each with rotation 0 from `own`, then clears `IsDynamic`.

### D6. Presence

`HasDrawSource` remains the explicit presence flag; `Erase()` keeps clearing it. Nothing infers presence from `GetHashCode()`. The descriptor encoding stays where it is for #400.

## Risks / Trade-offs

- [Bridge tiles: the client draws dynamic chunks from the source plane's terrain, while decoded blocked tiles and objects are already bridge-adjusted] → match on the adjusted plane, as standard loads do; cover a bridge tile in tests only if a fixture exists, otherwise record as a known limitation.
- [Unflagging overlapping collision can clear bits set by a neighbour object] → same behaviour as runtime removal today; footprints crossing part boundaries are re-flagged when that neighbour is rebuilt.
- [Template reads hit the cache and database per dynamic creation] → acceptable for an admin-triggered, rare operation; revisit with a cache only if measured.
- [Internal API break on `IMapRegion`/`IMapRegionPart`] → all callers are in this repository and updated in the same change.

## Migration Plan

No data migration. Rollback is a revert of the change.

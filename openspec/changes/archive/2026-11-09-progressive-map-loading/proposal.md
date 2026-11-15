# Proposal

## Why

Make a selected map usable while its remaining terrain, textures and landscape load. The current loader waits for all regions, then all appearances, then all textures; the pinned region 12850 alone expands to 1,733 distinct appearance requests across four planes, although the default visible ground plane needs 840.

## What Changes

- Preview transform-only objects using the client's zero-state selection rules with explicit warnings and preserved placement identity; live varp state remains outside this static viewer.

- Opt rendering endpoints into a warned per-face first-texel fallback for genuinely nonfinite texture projections, matching the characterized software client while retaining strict decoding by default and all valid geometry/textures.

- Publish each available region immediately and load visible terrain textures ahead of landscape completion.
- Populate landscape progressively in bounded, coalesced updates, with readable stage/count feedback and camera stability.
- Request appearances and their textures for visible planes only, and request additional assets when another plane or landscape becomes visible without reloading region data.
- Add a read-only bounded appearance batch API, reusing the existing assembly owner, so loading 840 visible keys does not require 840 separate appearance HTTP calls. Preserve existing endpoints.
- Reuse unchanged CPU preparation and GPU textures between progressive updates; retain current cancellation, deduplication, bitmap ownership, warnings and retry semantics.
- Return complete material metadata with each texture image so map textures need one HTTP request instead of separate image and material requests, while preserving material appearance.
- Allow private browser reuse of unchanged texture PNGs through ETag revalidation before image conversion, retaining fresh image/material edits and existing authorization.
- Batch opaque/cutout rendering by material and restrict camera-dependent ordering to transparent faces, preserving client priority, source topology and appearance.

### Goal and scope

One goal: reduce time to usable terrain and progressively complete visible landscape through the existing Maps detail workflow. Backend batching and frontend request/render integration are in scope. Use existing CacheMapSceneStore, CacheMapsService, object model assembly, pure scene preparation and ModelScene ownership; introduce no global asset cache, second loader or dependency.

### Acceptance criteria

- A1: An available region and its floor textures become visible without waiting for slower selected regions or object appearances; scene assets appear before the complete selection finishes.
- A2: Initial requests target visible plane assets. Showing another plane or landscape loads its missing assets once without refetching unchanged region payloads; repeated toggles reuse already-loaded assets. Camera/visibility controls stay usable.
- A3: Appearance batches have a bounded request size and bounded concurrency, preserve per-key success/failure, inherit cache authorization, honor cancellation and match existing single-appearance results. The default ground-plane catalog for the pinned region requires at most 27 appearance requests at batch size 32 instead of 840 single calls.
- A4: Progressive publications are coalesced; unchanged decoded/prepared assets and GPU textures survive updates. Counts distinguish completed work from queued/loading work and partial failures. There is one active selection/retry/resource lifecycle.
- A5: Selection replacement, visibility changes during loading, navigation, late bitmap completion and retry cannot publish stale results, duplicate placements or leak resources. Existing terrain blending, object transforms and model inspection behavior remain valid.
- A6: Record before/after first-terrain, first-textured-terrain, first-landscape, completion, request counts and preparation/upload work for region 12850 and adjacent regions under the same fixture/cache conditions. Tests deterministically prove incremental progress while requests remain pending. Report observed timings rather than guessed speed claims.

### Non-goals and stop conditions

No whole-world/camera-driven region streaming, LOD, model instancing, worker system, persisted/global cache, new network protocol, changes to terrain/object semantics or authentication. Existing HTTP responses plus bounded progressive batches supply the requested streaming experience. Do not add WebSockets/SSE/NDJSON unless measurements establish a remaining transport bottleneck that these changes cannot solve; that requires a follow-up decision. Do not remove transparent priority semantics or weaken fidelity/validation to obtain faster numbers. If profiling shows a worker or renderer replacement is required, report that evidence rather than absorbing it into this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-3d-map-rendering`: Progressive visible map loading, bounded batch appearance reads, asset reuse, progress reporting and cancellation during incremental scene publication. Extends the implemented `maps-region-browser` behavior without changing its table/detail URLs or nine-region bound.

## Impact

Backend: `Hagalaz.Services.Cache/Features/Models/ModelEndpoints.cs`, additive batch DTOs and focused cache-service tests. Frontend: CacheMapsService, CacheMapSceneStore, Maps detail status/visibility wiring, map scene preparation and the shared ModelScene update path, with direct store, pure preparation and renderer regressions. Rendering guidance and loading measurements are updated. No cache writes, gameplay contracts, database migrations or external dependencies.

Follow-up fidelity correction: convert floor definition RGB through the client packed-HSL/software palette with fixed gamma 0.7 before Three.js linearisation, for ordinary and blended terrain alike. Preserve geometry, visibility and texture loading semantics.

Follow-up: reproduce client neighbourhood HSL averaging and pass spatial tints through ordinary and blended terrain, retaining bounded material batching.

Follow-up performance correction: reuse CPU terrain and neighbourhood samples across ordinary texture arrivals, keeping declared IDs stable and retaining special-material invalidation and missing-image fallback.

Follow-up renderer loading correction: retain active materials and unchanged geometry while images arrive, and use stable terrain programs with updated sampler uniforms rather than texture-availability shader variants.

Follow-up: retain complete map geometry across ordinary texture-only publications and invalidate prepared model colours only when decoded colour settings change. Further browser measurements are collected manually by the user.

Follow-up from user-provided Chrome evidence: remove transparent draw-group fragmentation by making the sorted transparent index tail contiguous, preserving client priority/depth order and source topology.

Follow-up from the next user capture: deduplicate texture-interest discovery by appearance key and reuse each appearance's unique texture IDs within its existing selection loader.

Follow-up: reduce temporary allocations and repeated zero writes during landscape vertex expansion, preserving the full geometry attribute contract.

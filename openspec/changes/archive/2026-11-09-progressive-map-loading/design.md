# Design

## Context

See proposal.md for the goal and acceptance criteria. Source inspection found these concrete barriers and reuse opportunities:

- `CacheMapSceneStore.load` has sequential `toArray()` barriers for region reads, appearances and textures. Region state appears only after every region request completes; terrain texture requests cannot start until every appearance finishes.
- Pinned region 12850 contains 5,934 placements, 1,733 distinct `(id, shape, rotation)` appearance keys across all planes, 840 on plane zero and 20 referenced floor texture IDs across all planes. These are offline counts from the existing real-cache projection, not measured HTTP latency.
- The detail defaults to plane zero, but the store currently fetches all planes' asset keys. Visibility currently affects scene assembly only.
- Initially `CacheMapsService.texture` needed both image and material responses because image headers exposed only alpha/tint/brightness. The texture-loading follow-up adds complete material metadata to the image response so the map loader can avoid a second request without losing effect/scroll/repeat metadata.
- `ModelScene.setTextures` disposes every texture and clears the mesh whenever a new map instance arrives. Naively publishing textures individually would repeatedly upload existing images.
- The detail's `scene` computed reruns complete terrain and landscape assembly on any texture/appearance map change. Prepared appearances are cached only within a single build call. Progressive publication therefore needs coalescing and input-scoped CPU reuse.
- `maps-region-browser` is implemented but its delta remains active. This new change depends on that implementation and retains its table/detail URLs, local keys, nine-region bound, blending and failure semantics. Sync/archive the changes in dependency order; do not duplicate its requirements here.

## Goals / Non-goals

Provide progressive loading with fewer asset requests and explicit state/resource ownership. Improve time to useful visible terrain before pursuing unrelated steady-state FPS work. No change to cache byte decoding, terrain/object transforms, transparent priority rules, authentication or global cache ownership. No whole-world streaming or new wire-stream protocol.

## Decisions

### Static preview of transform-only objects

Object 12163 has no base geometry; client varbit 1839 selects canoe-station object 12144/model 11990 when the client configuration state is zero. Resolve one transform at the existing object-render owner using the client's branch rules with explicit zero-valued preview state: configured varbit/varp selects index 0, otherwise use the final fallback. Preserve source DTO identity and shape/rotation while using the selected definition's models, sizes and render settings. Warn with the chosen target and preview assumption; do not claim live game state. A selected -1 means intentionally hidden, returned as a successful empty appearance with a warning; never substitute another visible variant. Keep ordinary non-transform objects unchanged and keep genuine missing-shape/definition failures explicit. Single and batch paths share this behavior.

Transform IDs use the client's nullable big-smart semantics locally in `ObjectTypeCodec`: the two-byte 32767 sentinel becomes -1, while an extended four-byte ID 32767 remains an ID. This applies to indexed targets and the explicit fallback; keep the shared raw big-smart reader unchanged.

### Authorized first-texel compatibility fallback

The user authorized per-face software-client-compatible recovery after investigation of model 48743. Keep strict cache decoding as the default. Rendering endpoints opt into replacing only nonfinite face UV payloads with finite placeholders plus explicit `FirstTexelFaces` source-face metadata. Preserve geometry, texture IDs and every valid UV. Propagate marked face IDs through assembly and DTOs, issue an explicit appearance warning, and carry the marker through shared preparation and map aggregation. The existing renderer samples the centre of the original texture's first texel for marked faces using a fragment-shader override, independently of UV transforms/scrolling and mip derivatives; preserve normal sampling and alpha handling on other faces. Account for the flipped bitmap/DataTexture orientation. Do not add another texture cache or silently substitute face colours. Test strict rejection, permissive metadata, the pinned 47-face client result, assembly offsets/single-batch equivalence, preparation/aggregation, and shader sampling.

### Reported projection regression

Object 45152 shape 22/model 48859 extends the zero-Y case to sloped faces whose Y normal is nonzero but not dominant. With zero Y scale, use the valid X/Z projection whenever the oriented Y normal is nonzero. Keep the client-compatible zero-X side rule and finite-coordinate rejection when the required collapsed axis cannot be avoided. Add the real model and retain all earlier fixtures so the fixes cannot regress each other.

The follow-up object 45092 shape 0/model 48778 has sloped zero-X box mappings. Preserve the client's negative-X side-5 selection explicitly for zero X scale; the original scaled-normal comparison falls through to that side because X is NaN. Do not use unscaled dominant-axis selection for this case. Other mappings retain the unscaled orientation fix. Test both reported model payloads and the single/batch API paths together to catch regressions between these two cases.

Object 33858 shape 0 resolves to real-cache model 36668 and fails finite UV validation. Its box projection has a zero-scale axis unused by the correct face plane. Box-side selection currently cancels scale by dividing the scaled normal, producing NaN for that axis and selecting the wrong plane. Select sides using the cached unscaled orientation matrix instead, algebraically equivalent to the nonzero-scale calculation. Preserve UV scaling, signed orientations and non-finite-coordinate rejection for genuinely invalid projections. Pin the affected real model and verify public decoding and the object appearance path; do not hide the error with fabricated UVs.

### Rendering-speed follow-up

The shared renderer currently submits opaque faces in source order and rebuilds their grouping/depth data on camera movement. Batch opaque/cutout draw indices by material once per geometry update, using a renderer-owned index permutation while keeping source geometry/topology unchanged for animation and particles. Cache those opaque groups and classify transparent faces once per geometry update. Camera motion recomputes depth and priority ordering only for the transparent subset; preserve ascending client priority, descending camera depth and source-order tie breaking. Keep depth writes, material effects, UVs, colours and terrain blends unchanged. Do not reduce pixel ratio or visual fidelity. Verify group-count reduction on a repeatable alternating-material scene and CPU camera-update timings with a mocked GPU; do not present those as measured GPU FPS. Existing model animation/alpha/particle tests must pass. This user-requested follow-up includes steady-state rendering within the existing renderer, not a renderer replacement or worker.

### Texture-loading follow-up

Include the complete existing `GetTextureMaterialDto` as camelCase JSON in the image response's `X-Texture-Material` header, alongside the existing three compatibility headers. The map client consumes the image and metadata together through one cancellable HttpClient request. Keep the separate material endpoint for other callers. This removes one HTTP request and one redundant material-table read/decode per map texture. Preserve four-texture concurrency, floor-first interest, coalescing, bitmap ownership and error reporting. Do not add a cache or increase concurrency speculatively. Verify metadata equivalence and that a visible texture loads with no material request; elapsed browser/network improvement remains unmeasured.

### Browser texture reuse

Use the browser's native private HTTP cache for successful texture images. Send `Cache-Control: private, no-cache` and a versioned weak ETag derived from the source image bytes and complete serialized material metadata. Revalidate on reuse so raw image/material edits remain visible without a global client invalidation system. Compare If-None-Match before PNG decoding/encoding; an unchanged response returns 304 with no PNG body. Authorization still runs for every validation. Preserve image metadata headers and existing routes; do not share ImageBitmaps across selection owners or introduce another asset cache. This speeds repeated downloads/conversions, not first loads or scene preparation. The browser still performs a conditional request.

### 1. Stream scene availability over ordinary bounded HTTP requests

Publish region results as each completes, retaining per-region failures and source ordering for a stable scene. Start each available region's visible floor-texture work immediately. Visible landscape appearance batches proceed independently, so floor images are not queued behind all object models.

Use existing HttpClient requests and one detail-scoped loading owner. The user-visible scene streams progressively; an NDJSON/SSE/WebSocket transport is unnecessary to remove the observed client barriers. Introducing a new protocol would also add framing/auth/error handling before proving a remaining transport problem. The single-region semantic response remains unchanged in this phase.

### 2. Batch object appearance reads at the existing backend owner

Add authorized read-only `POST /api/v1/cache/types/objects/render-appearances` accepting 1..32 placement keys `{id, shape, rotation}`. Validate the entire request's domain and count before cache/model reads. Return keyed rows with either the existing appearance DTO or an explicit status/problem using current error conventions. Deduplicate repeated keys within the request. Keep the single-appearance endpoint unchanged.

Both operations call the same assembly function; do not make internal HTTP calls or copy model transform logic into the batch wrapper. Check cancellation between entries and pass it through any applicable reads. Per-key missing/unsupported failures must not abort other valid entries. Unexpected failures use the established error policy rather than exposing arbitrary exception text or inventing empty geometry.

The frontend uses batches of 32, at most two concurrent appearance batches. This reduces 840 plane-zero calls to at most 27 calls. Region reads remain at concurrency four; texture image/material pairs retain a bounded queue with floor work given priority. A 32-key batch limits latency and payload bursts; an unbounded all-objects response would recreate the original completion barrier.

### 3. Visibility changes request interest, not region identity

The router still owns the region rectangle and the store owns one active selection. The detail supplies visible planes and landscape visibility independently from selection identity. New visible asset interest is derived from already-decoded region data; it does not invoke `load` again or change the URL-owned selection.

Within the active selection, keep deduplicated completed, pending and failed appearance/texture keys. Visibility changes add missing visible work and deprioritize unstarted hidden work. Already-started bounded requests may finish and become reusable assets; do not abort and reissue everything on checkbox changes. Repeated toggles reuse completed assets. No persistent/global cache or second store is introduced.

### 4. Coalesce publication and reuse preparation

Use one selection-owned publication schedule, initially at most four asset publications per second, with immediate first-region publication and a final flush when currently requested work settles. Pending maps/sets accumulate results between publications. Publishing each individual HTTP/image completion is rejected because it would multiply scene builds and GPU churn. No automatic retry timer is added.

Split pure preparation dependencies sufficiently to reuse terrain when only landscape appearances change. Terrain invalidates when region/boundary, plane visibility or relevant floor-material inputs change, including the authoritative blending updates caused by an arriving neighbor. Reuse prepared appearance geometry until that appearance or its relevant material inputs change. Clear these caches with the selection. Retain placement-specific contouring and source geometry immutability.

Keep unchanged GPU texture objects by ID plus asset identity in `ModelScene`; create only new/changed textures and dispose only removed/replaced ones. Rebuild material bindings as needed without reuploading unchanged images. Preserve ordinary model inspection, HDR uploads, material effects and single-model update behavior through shared renderer regressions.

This phase need not eliminate every aggregate buffer copy. Record scene publication/preparation/upload counts and time. If coalescing plus reuse still causes long main-thread stalls, report the evidence before introducing a worker or replacing the renderer with a new streaming engine.

### 5. Keep progress, errors and cleanup under one owner

| Responsibility | Owner |
| --- | --- |
| Region identity/rectangle and list return context | Existing router state |
| Visibility and form drafts | Existing detail signals/forms, forwarded as asset interest |
| Region/appearance/texture queues, deduplication, progress, retry and publication schedule | Existing detail-scoped scene store |
| HTTP/auth/serialization | Existing cache service plus additive batch method |
| Model decode/transforms and per-key outcomes | Existing backend model assembly owner and batch projection |
| CPU terrain/appearance reuse and coordinate conversion | Pure map scene preparation boundary |
| GPU texture/material/geometry lifecycle and camera | Existing ModelScene/viewport boundary |
| Bitmap close, including late resolution | Active scene-store selection lifecycle |

Progress reports region responses and visible asset keys settled/pending, with failure counts separate from successful availability. Totals may grow when another region arrives or visibility expands; do not show a misleading fixed overall percentage. A scene may be usable while loading continues. Retry remains one explicit command through the active selection lifecycle and cannot duplicate existing scene placements.

On selection replacement/navigation, unsubscribe pending work, cancel the publication schedule, invalidate late callbacks, close current/late bitmaps and release CPU/GPU state through their existing owners. Visibility changes within the same selection retain usable content and camera; a new selection uses existing framing rules.

## Risks / trade-offs

- Fewer HTTP calls do not reduce all model decode work. Measure actual first-content and completion times, and do not present the request-count estimate as a speed multiplier.
- Large model batches can stall JSON parsing/preparation. Bound them to 32, coalesce rendering and retain cancellation; adjust the internal batch/concurrency constant only with repeatable measurements.
- Earlier assets expose more intermediate states. Deterministic delayed-response tests must prove partial success, camera stability and final equivalence to existing geometry.
- Texture identity/reuse changes affect all shared model viewers. Preserve single-model, effect/HDR and disposal regressions rather than adding map-only bypasses.
- Ground-plane loading still needs region decoding containing all four planes. Splitting semantic terrain/landscape transport is deferred unless measurements show that region response dominates first content.

## Validation and rollout

Capture the current behavior before implementation using pinned 12850 and a bounded adjacent selection. Record cache conditions, visible planes, requests/bytes, first terrain, first textured terrain, first visible landscape, visible completion, scene publications and texture creation/disposal counts. Keep fixture timing, real-cache timing and user visual confirmation distinct. Respect the user's request to skip further browser inspection; use deterministic tests and an external profiling harness, and report unverified browser timings rather than reopening their browser without a new request.

Add tests that leave one region, batch or texture pending while asserting other content is already published. Compare batch results with existing single reads; test auth, bounds, per-key failures and cancellation. Verify hidden-plane deferral, repeated toggles, late bitmaps, selection replacement, partial retry, unchanged texture reuse and preserved terrain blending/model geometry.

Run focused backend tests, admin coverage, shared renderer tests, production admin build, changed-file Biome, Fallow audit, strict OpenSpec and diff review. Roll back the additive batch caller/endpoint and progressive scheduling together if necessary; no data migration or cache-content changes occur.

## Terrain base-colour correction

Raw definition RGB is not the client display tint. Class491.method6074 converts it to integer HSL (including weighted hue), Class230.method2338 packs it, and Class_xa_Sub3 uses Class356.method4187 plus the gamma-corrected software palette. Use the shared model-lighting palette with deterministic gamma 0.7 for floor base colours, then convert display RGB to linear for Three.js. Ordinary geometry and terrain blend layers must share this calculation, independently of texture availability. Preserve the current unlit terrain, blending weights, source planes and model lighting; this fix does not claim atmosphere, terrain shadows or neighbourhood HSL averaging parity.

## Neighbourhood floor colour follow-up

The palette-only correction does not reproduce Class274.method2692, which sums weighted hue, saturation, lightness, hue multiplier and count over offsets -4 through +5 on each axis before packing HSL. Build summed-area tables per selected region/plane with a selected-neighbour halo, ignoring missing floors and keeping source planes separate. Compute each tile's palette tint from this window. Carry interpolated spatial tints in vertex colours for both ordinary and splatted surfaces; splatting should blend texture samples only, multiplying the vertex tint once. Preserve existing material keys/draw counts, tile-centre texture weights, ownership and special-material paths. Missing unselected neighbouring terrain remains unavailable. This extends the previous base-colour correction; atmosphere/shadows remain outside scope.

## Cached texture arrival CPU work

A real-region CPU replay spends 1.48-1.71 seconds on 21 terrain preparations as twenty texture assets arrive. Terrain geometry currently depends on texture object identity and loaded availability despite opaque texture pixels not changing topology, UVs or tints. Keep declared texture IDs in geometry; ModelScene already falls back to its untextured material for absent texture IDs. Invalidate prepared terrain on alpha/effect mode changes only, since these control the splatting versus ordinary path. Retain colour sampling in the existing MapScenePreparation owner keyed by region-array identity, with no global cache or resource ownership change. Keep shared renderer texture identity and lifetime semantics intact.

## Renderer resource churn during texture arrival

Live cache-service spans for texture images show low tens of milliseconds, while the user reports viewer stuttering. ModelScene currently clears mesh geometry and every material on any texture-map change; the following geometry publication recreates them. Terrain programs also branch at shader-generation time on image availability. Keep resource ownership in ModelScene: retain and reuse currently active ordinary/texture/terrain materials keyed by their actual shader/asset inputs, dispose entries once retired, and preserve existing GPU texture identity. Retain BufferGeometry on image-only updates with the same immutable geometry object. Use uniform-controlled texture availability in terrain shaders with a program key based on layer count, updating sampler uniforms on retained terrain materials. Keep effect callbacks attached only to live materials; preserve animation, first-texel, alpha, particles and ordering. No global cache, worker or new rendering backend is introduced. Record deterministic resource churn separately from actual browser/GPU timing.

## Full landscape rebuilds on image arrival

The Chrome capture shows a 47.14-second cached texture sequence and 4.484 seconds of scripting in its initial 5.196-second recording. Every image publication reevaluates buildMapScene, copies terrain and expands all landscape placements into a fresh geometry object, bypassing ModelScene's retained-object buffers. Extend the existing selection-scoped MapScenePreparation owner to retain the combined result while terrain identity, appearance-map identity and prepared part identities remain unchanged. Prepared part identities depend on unique texture colour inputs (colour blend, brightness and effect-6 lighting), not bitmap or texture object identity. Missing ordinary metadata uses the same neutral defaults as prepared model lighting; legacy unprepared colours retain their existing missing-versus-present distinction. Real colour-setting changes still rebuild, as do new appearance batches, region/plane/visibility changes. Retain warnings with the combined result. No global cache or new asset lifecycle is added. Validate using the saved real Chrome HAR offline; the user now owns browser measurements and prohibits further computer/browser control.

## Transparent draw-call fragmentation from the user profile

The user-provided follow-up HAR shows texture requests finishing at 22.469 seconds versus 47.145 seconds, with all 179 images revalidated and median duration 22.934 ms. The screenshot still shows setGeometry at 931.2 ms total and Three.js needsUpdate at 623.3 ms self. Inspecting Three.js identifies needsUpdate as per-draw attribute-binding checks. An offline replay of the real scene produces 97,590 draw groups, of which 97,367 are transparent. The renderer sorts transparent faces correctly but retains scattered index ranges, preventing consecutive same-material faces from batching. Write sorted faces into a contiguous transparent index tail, retaining the opaque prefix and immutable source indices/attributes. Merge only adjacent equal-material runs in the already-established priority/depth/tie order. Update the index attribute only if its transparent contents change, and rebuild draw groups as before for camera/alpha changes. No material-based reordering, global cache, worker or changed transparency semantics is introduced.

## Repeated texture-interest discovery

The user's next HAR measures final texture response at 11.111 seconds, down from 22.469 seconds, with all 179 images HTTP304 and median 23.117 ms. The supplied Bottom-up screenshot identifies appearanceTextures at 266.7 ms total in its first five seconds. MapSceneLoad.interest scans every model face once per placement on each pump/publication, even when several placements share one appearance. Skip duplicate placement keys before discovering textures and keep a weak per-appearance unique-ID list within the existing selection loader. Appearance payloads are immutable during a selection. This removes repeated face scans without owning image resources or adding a global cache. New selections create a fresh loader and catalog; hidden visibility and request concurrency remain unchanged.

## Landscape expansion allocation work

The latest user HAR finishes all 179 cached texture responses at 10.459 seconds. Its screenshot identifies addVertex and appendPart as major CPU costs. appendPart allocates fresh UV/colour slices and an offset point for every expanded vertex, while addVertex pushes eight zero terrain weights for every landscape vertex. Reuse per-part scratch values synchronously copied by addVertex, and defer the contiguous landscape weight suffix to one zero fill after landscape assembly. Preserve the full eight-weights-per-vertex contract and exact source attributes; no sparse GPU attribute layout or new renderer path is introduced. Verify all geometry attributes with saved real-data hashes before and after, and retain this optimization only if CPU replay improves.

## Direct RGBA buffer assembly

The next user capture finishes texture requests at 9.364 seconds and shows 1.056 seconds total in setGeometry. Write each vertex's RGBA values directly to a Float32Array and attach it as a BufferAttribute. This removes temporary per-vertex arrays and the intermediate flattened array while preserving the same Float32 values and white colour defaults.

## First frame transparent ordering

Geometry setup builds the opaque prefix and classifies transparent faces. The render loop already orders transparent faces using the final camera before drawing. Defer the transparent index and group writes to that pass to avoid sorting and writing them during setup with the previous view matrix.

### Map vertex memory

Only terrain draw groups use blend weights. Keep their attribute data through the last terrain vertex and leave landscape materials on the existing standard attributes. Preserve a source index snapshot for geometry that can update in place; static geometry can reuse its immutable source indices.

### Idle map rendering

The map viewport uses coalesced animation-frame requests for camera, viewport, or scene changes. A stationary map keeps its last frame and samples texture effects on the next redraw. Standalone animated model viewers keep their continuous render loop.

Static landscape previews publish GPU-ready RGBA and UV arrays. The renderer binds those arrays directly and preserves source geometry, material ordering, and the standalone animated model path.

### Prepared landscape memory

The map preparation cache stores compact typed arrays and projects shared source positions once per placement. Expanded corners retain their own colors, UVs, and opacity. Map assembly publishes the same draw data through the existing renderer.

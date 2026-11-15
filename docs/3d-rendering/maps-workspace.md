# Maps browser and region detail

## Region listing

`GET /api/v1/cache/types/maps?offset=0&limit=10` uses the existing cache administrator authorization. `offset` is in 0..65536 and `limit` in 1..200. The response contains `items`, `total`, `offset` and `limit`.

Each item contains packed region `id`, `regionX`, `regionY`, nullable `terrainFileId` and nullable `landscapeFileId`. A region appears when index 5 has its `mX_Y` or `lX_Y` file. Paired files produce one row. Source file IDs are distinct from packed region IDs; presence does not prove decodability or available keys. Rows use ascending region IDs and actual entry totals.

Listing reads named reference metadata without opening payloads. Unnamed metadata produces HTTP 422, distinct from an empty index. Invalid pagination produces HTTP 400.

## Region render API

`POST /api/v1/cache/types/maps/{id}/render` accepts `{}` or `{ "xteaKeys": [1, 2, 3, 4] }`. IDs are 0..65535. Keys contain exactly four signed int32 values; four zeros are valid. The operation is read-only and inherits cache read authorization. Existing map GET/decode contracts are unchanged.

The result contains identity/coordinates, independent `terrainStatus` and `landscapeStatus`, `planes`, `objects`, `floors` and source-specific `warnings`. Each status is `available`, `absent` or `failed`. A landscape decompression failure does not prove wrong keys, and does not discard successfully decoded terrain. Supplying corrected keys and retrying uses the same endpoint.

Each of four planes contains `cornerHeights`, `flags`, `underlays`, `overlays`, `overlayShapes` and `overlayRotations`. Tile arrays are 64 by 64, flattened as `x * 64 + y`. Heights are 65 by 65, flattened as `x * 65 + y`, in signed downward legacy units. East, north and northeast neighbor terrain supplies authoritative outer samples. Null corner entries explicitly identify unavailable boundary data; affected triangles and contoured objects are omitted with feedback. No last-row repetition or generated fallback hides missing data.

Placements retain source plane, ID, shape, rotation and local X/Y before gameplay collision-plane adjustments. Floor IDs are one-based tile IDs, with RGB, texture ID, secondary RGB and legacy texture scale. RGB and actual texture data drive appearance; missing definitions remain warnings.

`GET /api/v1/cache/types/objects/{id}/render-appearance?shape=10&rotation=0` reuses the existing model assembly owner and returns reusable `parts`, footprint, decoration displacement, contour mode/parameter and warnings. Supported shapes are 0..22, rotations 0..3. It applies shape selection, multipart walls/decorations, mirror/winding/UV correction, orientation and definition transforms in legacy order. It preserves the old single-object inspection contract. Failed models retain their object identity and HTTP failure rather than returning an invented empty appearance.

## Admin usage

Open `/cache/types?kind=maps`. The Material table shows actual regions, coordinates and terrain/landscape file presence, with existing page sizes and `offset`/`limit` query parameters. Region ID lookup, ID links and Open map navigate to `/cache/types/maps/:id`. The table requests no terrain, models or textures.

Detail automatically loads one region with 3D terrain and static landscape objects. `width` and `height` query parameters select an east/north rectangle, each dimension 1..3, at most nine regions. Coordinate overflow is rejected. Refresh/history restore the route selection. Back to Maps restores `kind`, `query`, `offset` and `limit`, excluding detail dimensions.

Drag to orbit, use OrbitControls pan/zoom gestures, or use named camera buttons. Focus the viewport for arrow-key orbit, Shift+arrows pan, plus/minus zoom and Home reset. Plane and landscape toggles reuse loaded assets and request only newly needed assets; camera navigation survives visibility updates. New selections fit their bounds. Phone layouts stack controls and keep table scrolling inside its card.

The Region keys panel accepts JSON keyed by selected region ID, for example `{ "12850": [1, 2, 3, 4] }`. Validation happens before requests. Keys remain in detail-component memory, never URLs or persistent storage. Availability and asset warnings identify regions/objects/textures. Retry reloads the current selection through the same store owner.

## Runtime ownership and limits

The route owns applied selection, signal forms own drafts, the table store owns metadata requests and the detail store owns the current scene request set. Selection replacement cancels old HTTP work. Appearance/texture keys are deduplicated per selection, with two concurrent appearance batches and four texture image requests. The store closes bitmap resources, including late bitmap resolutions after cancellation. The map viewport adapts the existing ModelScene lifecycle for an aggregate whole-selection mesh. ModelScene owns controls, renderer, resize observer, animation loop and GPU disposal. The pure assembler owns coordinate conversion, floor topology, placement-specific contouring and translation; it never mutates reusable source model positions.

This is static rendering. Varp-driven variants use the base definition with warnings. Contour modes above 2, nonzero legacy vertex adjustment and palette-index recolors remain explicitly unsupported. Existing decoder limitations, including degenerate UV projections for some real-cache objects, remain visible per-asset failures. Other terrain and supported objects continue rendering. This work does not establish complete GameClient pixel parity or add tile/object inspection, gameplay changes, cache mutation or new dependencies.

## Reference and validation evidence

`TestSupport/Fixtures/maps/` pins real revision-742 terrain and landscape for region 12850, with source commit, cache/payload hashes and independently captured semantic hashes. All terrain tile fields match calls to the existing GameClient terrain decoder. All 5934 source placements match client Buffer primitives and Map source traversal before clipping. Normal tests require no Java, external cache or network. Numeric model-transform/scene tests separately cover rotation, mirrors, walls/decorations, contouring and adjacent coordinates.

Browser validation used the pinned real cache through an isolated read-only loopback API, with fixture authentication only. Real single-region and 2-by-2 scenes, desktop/phone layouts, visibility/camera without refetch, table return context and browser history passed. Separate synthetic API responses exercised missing terrain, failed landscape, invalid keys, corrected-key reload, retry and navigation cleanup. Production authentication/hosted deployment was not tested by that loopback setup. See the OpenSpec task evidence for commands, coverage and inherited quality-gate findings.

## Ordinary terrain material transitions

Ordinary opaque underlays interpolate both decoded tint and textures between neighboring tile centers. Shared vertices use normalized contributions from available tiles in world coordinates, including selected-region boundaries. Each contributing texture retains its own legacy scale. Blends use a single opaque material pass with up to eight layers per triangle; ModelScene owns the borrowed GPU textures and disposes blend materials with the rest of the scene. Missing images retain their decoded floor colour. Overlays and special/transparent underlays use their existing materials.

This interpolation removes abrupt ordinary tile transitions. It does not implement the complete client's 10-by-10 weighted-HSL smoothing or neighbor-aware overlay topology. Unselected neighboring floor data is not inferred or fetched implicitly.

## Progressive loading and appearance batches

`POST /api/v1/cache/types/objects/render-appearances` accepts an array of 1..32 `{id, shape, rotation}` keys. IDs must be nonnegative, shapes 0..22 and rotations 0..3. Empty, oversized, null or invalid entries fail validation before decoding. The authorized read-only operation deduplicates keys and returns rows `{id, shape, rotation, status, appearance, problem}`. Successful `appearance` matches the existing single-object render endpoint. Missing or unsupported entries retain their per-key failure rather than discarding other successful results. Cancellation stops remaining entries. Existing single-object endpoints remain unchanged.

Maps now publish each region as it arrives, start visible floor images independently and add landscape through at most two concurrent 32-key batches. Texture loading remains bounded to four image requests with visible floor work scheduled first. The initial plane-zero view defers hidden-plane-only assets. Showing other planes or landscape schedules missing assets without repeating region reads; completed assets remain reusable until selection replacement. Unstarted hidden work is deprioritized; already-started requests may complete.

Asset publications coalesce over 250ms with an immediate first-region update and a final flush when current work settles. Region/appearance/texture counts and failed-read counts replace an unreliable fixed percentage. Camera and visibility remain usable while the scene loads. Retry uses the same selection owner, and replacement/navigation cancels pending work, publication timers and late bitmap results.

CPU terrain preparation is reused when region, visible planes and relevant floor materials are unchanged. Reusable appearance preparation survives unrelated asset additions and invalidates only when its relevant material inputs change. These caches live only within the detail selection. Aggregate buffers and placement contouring can still be rebuilt at publication boundaries. ModelScene retains unchanged GPU texture identities and disposes removed/replaced textures; its existing owner releases materials and geometry. Bitmaps remain owned by the scene store.

### Controlled loading profile, 2026-11-04

The external harness runs the actual old/new store, geometry preparation and ModelScene texture lifecycle against pinned real region DTOs for 12850 and its east neighbor 13106. Appearance geometry is a synthetic triangle, image bitmaps and the GPU boundary are fixtures, and request delays are controlled at 5ms per old individual request versus 4ms plus 1ms/key per batch. Both versions use the same input/latency rules and plane-zero visibility. These are pipeline-ready/CPU measurements, not live network, browser paint or full-model GPU timings. The baseline eagerly loads all planes as the old code did; the progressive loader requests visible-only assets.

| Selection / version | Appearance calls | Texture HTTP calls | Terrain ready ms | Textured terrain ms | First landscape ms | Visible complete ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 12850 baseline | 1733 | 40 | 124 | 7107 | 6951 | 7107 |
| 12850 progressive | 27 | 34 | 114 | 384 | 384 | 799 |
| Two regions baseline | 2519 | 56 | 209 | 10458 | 10199 | 10458 |
| Two regions progressive | 47 | 52 | 85 | 507 | 507 | 1540 |

For one region, terrain preparation ran 3 times before and 2 after; model preparation ran 1680 times before and 840 after. For two regions, model preparation dropped from 2966 to 1483, with terrain prepared 3 times in both versions because the additional region and floor materials invalidate its inputs. Scene publications rose from 3 to 4 for one region and 3 to 7 for two, providing incremental content. Unchanged texture objects are retained: the progressive runs created/disposed exactly 17 and 26 texture objects respectively, despite multiple publications. This counts GPU resource objects at the mocked renderer boundary rather than physical driver uploads.

The harness asserts that progressive requests contain no hidden-only appearance keys and that the default 12850 scene uses exactly 27 appearance batches. Actual cached-model decoding, large JSON payloads, aggregate buffer copying and placement contouring remain possible bottlenecks; these fixture times must not be presented as a production speed multiplier. No further browser inspection was performed at the user's request. Harness, frozen baseline bundle, exported region data and raw result JSON are outside source in the session visualization directory under `progressive-loading/`.

### Texture request follow-up

Map texture images now carry `X-Texture-Material`, camelCase JSON matching the existing material endpoint's complete DTO. The map loader consumes this header and the PNG in one response, eliminating one HTTP request and a repeated material-table read/decode per texture. Existing image alpha/tint/brightness headers and the standalone material endpoint remain available for other callers. Transparency, colour, effects, scroll, repeat and HDR metadata are preserved. The four-texture limit and selection cancellation remain unchanged. The frontend and cache service must run matching updated builds; missing metadata produces an explicit texture warning. Actual browser loading-time improvement has not been measured.

Texture PNG responses also support native browser caching with `Cache-Control: private, no-cache` and a weak `ETag` covering the source image and complete material metadata. An unchanged `If-None-Match` returns a bodyless 304 before PNG decoding/encoding, letting the browser reuse its saved PNG and headers. Revalidation still reads source/material data and requires authorization; changes produce a fresh 200. This avoids stale raw-cache edits without a separate global client cache or shared bitmap lifetime. It benefits repeat loads, not the first uncached load or bitmap/scene preparation. Browser-cache retention depends on normal browser settings; disabling the cache in developer tools prevents reuse. Browser elapsed-time improvement remains unmeasured.

### Reported projection regressions

#### Transform-only placement preview

Object 12163 shape 10 is a model-less transform definition. In the client's zero-initialized configuration state, varbit 1839 selects canoe-station definition 12144/model 11990. The shared single/batch object-render owner now performs the same one-step selection for static previews before model-shape lookup, retaining placement/source identity while using the selected definition's geometry, sizes, contouring and rendering parameters. Warnings identify the selected definition and distinguish assumed zero state from a selector-less default fallback. No live varp state or automatic first-visible-variant search is simulated.

An explicit hidden target (-1) returns a successful empty appearance with a warning and no model read. Object transform decoding now respects the client's nullable short-ID encoding: two-byte 32767 means hidden; four-byte encoded ID 32767 remains a valid ID. The shared raw big-smart helper is unchanged. Pinned source/target definitions and model provenance are in `TestSupport/Fixtures/objects/README.md`.

Validation: all 183 cache-service tests and 11 existing object-codec tests pass. New regressions cover the real source/selected definition in all four single/batch rotations, preserved source identity and selected geometry, configured and absent selectors, hidden indexed/default targets, and both encodings of 32767. The real-cache registered appearance handler returns HTTP 200 for object 12163 shape 10 in all rotations. This does not establish live game-state parity.

#### Client investigation: object 45291 / model 48743

Executed the original compiled GameClient helpers against the extracted real model using an external Java probe: `new ModelDefinition(payload)`, revision normalization, `Model.method4444`, `method4442` and `method4443`, with a headless `Model_Sub3(null)` used only for its inherited projection methods. Of 171 faces, 138 are textured and 47 produce non-finite UVs. Those 47 reference eleven all-zero scale mappings. The client math produces `[NaN, NaN]` rather than recovering a valid projection; the probe also confirms Java casts a NaN texture index to integer zero.

The software path (`Model_Sub3` to `Class38.method541/542`) passes the generated UVs through, casts interpolated texture coordinates to integer indices, and samples texel `(0,0)` when those indices are NaN. This is a source-traced consequence plus a verified Java conversion, not a full rendered screenshot test. The hardware path stores generated UVs in the vertex buffers (`Model_Sub1` float arrays and `putFloat`) without finite validation. Its GPU pixel result is not established by this investigation and must not be described as a reliable fallback. Neither path rejects the complete object because one face has invalid UVs.

There is also a separate client preference/material path that strips `isGroundMesh` textures when renderer flag `0x40` is set; it runs before projection and is not generic malformed-UV recovery. Normal preference defaults enable the textured path.

No colour fallback was added. After the investigation, the user authorized the first-texel compatibility implementation described below. External reproducible probe and output: session `object-33858/ClientProjectionProbe.java` and `client-48743.log`. No client cache or renderer source was modified and no browser/window was opened.

#### Authorized first-texel compatibility implementation, 2026-11-05

Strict `ModelGeometryDecoder.Decode` retains rejection by default. Model/appearance rendering endpoints opt in to per-face recovery, carrying `firstTexelFaces` through the model DTO with an explicit warning. Assembly offsets the face IDs when combining source models; valid geometry, texture IDs and UVs stay unchanged. Shared preparation converts face metadata to vertex markers and map aggregation retains those markers alongside the existing terrain attributes.

On marked fragments, the existing renderer samples the original texture's top-left texel centre, accounting for the bitmap/HDR row flip. The sampler bypasses animated UV offsets/transforms; ordinary faces retain their normal UV path. Existing material alpha, cutout tests, lighting and effect hooks remain in place. This adds no extra image/texture cache. Shader hook/coordinate tests cover bitmap and HDR assets; actual GPU pixel output remains unverified because the browser was not reopened.

Fresh validation: 57 decoder tests, all 173 cache-service tests, 57 shared-viewer tests and all 307 admin tests pass. Both production app builds pass, as do configured changed-file Biome lint, strict OpenSpec and diff checks. Shared renderer files remain outside the configured Biome include scope. Existing dependency and polyfills warnings remain; no new changed-code warnings were introduced.

The repeated read-only rendering-mode scan of the pinned region-12850 definition set covers 800 objects and 650 models. It returns zero nonfinite or unsupported results; five models use first-texel fallback on 436 faces: model 33355 (7), 33356 (6), 36712 (352), 36541 (24) and 48743 (47). The earlier strict-scan failures remain valid strict-decoder outcomes, but no longer prevent these models from being rendered through the opted-in API.

Pinned regression payloads now cover object 33858 shape 0/model 36668, object 45092 shape 0/model 48778, and object 45152 shape 22/model 48859. Zero-Y projections select the valid X/Z plane when the oriented Y normal is nonzero, including sloped faces where Y is not dominant. Collapsed-X mappings retain the client's negative-X side. Model tests preserve ordinary UV goldens and rejection of genuinely non-finite projections; API tests exercise all three objects in all four rotations through both single and batch handlers with finite/nontrivial UVs and matching geometry.

A read-only scan of all definitions referenced by the pinned region-12850 landscape inspected 800 distinct object IDs and 650 distinct models across their definition shapes. Five additional models still fail finite projection validation: 33355, 33356, 36712, 36541 and 48743. They remain explicit unsupported outcomes; this change does not claim all region projections are repaired. Raw extracted diagnostic payloads and scan output are external session artifacts in `object-33858/` and the session logs. Their repair requires separate diagnosis rather than fabricated UVs or silently omitted geometry.

### Rendering-speed follow-up

`ModelScene` now builds a renderer-owned draw-index permutation with opaque/cutout triangles grouped by material. Source positions, indices, UVs, colours and face IDs remain unchanged for animation and particles. Transparent faces retain their source references and ascending client priority, descending camera depth and source-order ties. Only their depth/order is revisited when the camera moves; cached opaque ranges are reused. Visual resolution, material effects and depth-write rules are unchanged.

A controlled before/after profile used the actual shared renderer with mocked WebGL/OrbitControls, Node 24.16.0 and 12,000 opaque/cutout triangles alternating between two materials. Each of seven independent scenes warmed for 20 frames, then timed 100 orbit/render callbacks. Median CPU camera-update time was 0.6357 ms before and 0.001401 ms after. Draw groups fell from 12,000 to 2. These are synthetic CPU/group-count measurements, not real region-12850 GPU FPS or browser interaction latency. Transparent-heavy scenes still require sorting and individual draw ranges. The profile, frozen baseline and raw runs are external session artifacts under `render-speed/`; reproduce by placing `profile.spec.ts` and the frozen baseline beside the shared renderer as `render-speed-profile.spec.ts` and `model-scene.before-profile.ts`, then running Vitest for the profile and removing those two temporary files.

All 54 shared model-viewer tests passed, including batching, source attributes, alpha transitions, transparent priority/camera reversal, animation, effects and particle ownership. Both admin and main-app production builds and strict OpenSpec validation passed. Shared files are excluded from the repository's configured Biome scope, so the normal changed-file Biome command did not lint them. No browser inspection or GPU timing was performed.

### Cached texture arrival CPU replay (2026-11-05)

Using the real region 12850 DTO, visible plane 0, landscape disabled and 20 floor texture arrivals, three same-process Node CPU replays took 1568.0/1478.9/1707.8 ms before and 110.3/102.1/100.0 ms after terrain reuse. This measures pure preparation, excluding network, image decoding, GPU upload and browser rendering. Ordinary opaque image arrival/replacement now retains declared texture IDs and prepared geometry; absent images use ModelScene's existing fallback. Alpha/effect mode changes still invalidate geometry, and neighbourhood sampling stays selection-scoped in MapScenePreparation. Browser end-to-end timing remains unmeasured.

### Renderer stalls during texture arrival (2026-11-05)

Thirty recent live cache-service texture spans measured 12-29 ms (median 15 ms); these are server durations, not browser round trips. Inspection found ModelScene disposed all mesh geometry/materials on each texture-map change, and terrain shader source varied with image availability. Active material instances are now retained within ModelScene, unchanged immutable geometry retains its vertex/weight buffers, and terrain availability changes only sampler/enabled uniforms. Retired resources and effect callbacks are pruned, and no global cache is introduced. A controlled twenty-image arrival regression verifies zero terrain geometry/material disposals during arrival, stable shader identity, correct image bindings and one-time retirement. Landscape growth still replaces geometry buffers while retaining matching materials. These are deterministic resource/CPU-path guarantees; browser GPU/frame timing remains unmeasured.

### Actual Chrome reload profile (2026-11-05)

Measured the existing authenticated Chrome tab at region 12850, 1x1, plane 0 plus landscape, with browser cache enabled, no network/CPU throttling and the Angular development server. This is one warm reload with DevTools overhead, not a production or before/after benchmark.

- Network finish: 47.14 s. All 179 texture image responses were HTTP 304; median duration 26.20 ms, p95 50.02 ms, maximum 59.68 ms. Their requests span 0.566 s through 47.145 s after navigation. Texture transfer was 90,856 bytes.
- The 27 appearance batches finished by 5.640 s; median request duration 49.96 ms. They transferred 22,549,080 bytes.
- The initial 5.196 s CPU recording contains 4.484 s scripting (86%), 395 ms system, 88 ms rendering and 28 ms painting. Fourteen main-thread tasks exceed 50 ms; the longest is 1,023.577 ms.
- Chrome Bottom-up reports addVertex at 871.954 ms self / 924.420 ms total and setGeometry at 220.846 ms self / 649.286 ms total. These costs are from the initial recording only, not the entire 47-second load.

The browser evidence confirms cache revalidation is fast while frontend work delays progress. Source tracing shows that every texture publication still reevaluates the Maps detail scene, copies terrain and expands all landscape placements into a new combined geometry object. That bypasses the unchanged-object buffer reuse added earlier. Avoiding full landscape reconstruction for ordinary texture-only updates is the next identified optimization. The measurement itself does not establish a new loading-speed improvement or complete GPU timing breakdown.

Trace, sanitized HAR, screenshots and machine-readable summary are saved outside the source tree under the session's progressive-loading/chrome-* artifacts.

### Combined landscape reuse after Chrome diagnosis (2026-11-05)

MapScenePreparation now retains the combined terrain-and-landscape result across ordinary texture-only updates. Prepared model dependencies use unique texture IDs and decoded colour settings rather than bitmap identity. Nonneutral colour blend/brightness changes and effect-6 lighting still invalidate colours, while legacy unprepared appearances retain their missing-material white fallback. Region, appearance and visible-plane updates rebuild correctly and warnings remain attached to the cached combined result.

Offline replay of the saved real Chrome HAR (840 appearance results, plane 0 plus landscape, 1,584,672 expanded vertices) measured twenty late ordinary texture arrivals, excluding initial preparation. Before: 11,269.13 / 10,276.99 / 11,344.22 ms and twenty combined rebuilds each run. After: 12.65 / 11.75 / 18.51 ms and zero combined rebuilds. This is Node CPU preparation only, not a measured browser loading improvement. All 316 admin tests, 70 shared-viewer tests and the admin production build pass; existing polyfills test warning remains.

The user now collects all browser evidence manually; do not use computer/browser automation. Manual comparison: in Chrome DevTools Network leave Disable cache unchecked and use No throttling. In Performance choose Record and reload. After the trace and map loading finish, provide the Performance Summary, top Bottom-up rows, and Network footer Finish value. Compare with the saved baseline (47.14-second network finish, initial 5.196-second trace with 4.484 seconds scripting).

### User-provided follow-up HAR and transparent batching (2026-11-05)

The replacement HAR contains one map reload. Its 179 texture images are all HTTP 304, median 22.934 ms and p95 49.044 ms; the last texture finishes at 22.469 s versus 47.145 s previously (52.34% less elapsed time). All 27 appearance batches finish by 2.990 s versus 5.640 s. A later background request extends the overall HAR to 90.160 s, so that overall finish is not the texture completion metric. The supplied five-second Bottom-up screenshot still reports needsUpdate 623.3 ms self, setGeometry 931.2 ms total, and appearanceTextures 200.5 ms self. The captures include different amounts of progressively available content; these individual function totals are not controlled before/after benchmarks.

Three.js needsUpdate at the reported line is a per-draw attribute-binding check. A real-data replay with an actual Three.js scene/geometry and mocked WebGL renderer finds 97,590 draw groups (97,367 transparent). Sorting transparent faces into a contiguous draw-index tail reduces this to 7,637 groups (7,414 transparent), a 92.17% reduction. The complete rendered face/material sequence has the same SHA-256 checksum before and after; source topology and the existing client priority/depth/tie comparator are preserved. Only consecutive equal-material runs merge, and unchanged transparent index contents are not marked for upload on camera changes. This measures structural draw-group reduction, not GPU frame time. New browser evidence will be supplied manually by the user; no computer/browser control is authorized.

### Second user capture and texture-interest discovery (2026-11-05)

The next replacement HAR measures 179 texture responses completing at 11.111 s, down from 22.469 s in the previous user capture and 47.145 s originally. All 179 responses are HTTP304; median duration is 23.117 ms. The 27 appearance batches finish by 3.544 s. These are individual warm reloads with user-supplied settings, not a controlled statistical benchmark. The supplied Bottom-up screenshot no longer lists per-draw needsUpdate among its visible leading costs; appearanceTextures is 266.7 ms total, while initial landscape assembly remains significant.

MapSceneLoad now skips duplicate appearance keys during interest discovery and retains each immutable appearance's unique texture IDs in a selection-owned WeakMap. Model face texture arrays are scanned once per selection, rather than per placement and pump/publication. The ordered appearance/texture catalog is identical in the real-data replay. Three 100-call CPU replays measured 420.47-476.60 ms before and 60.89-90.12 ms after; these exclude network, decoding and GPU work. All 317 admin and 72 shared-viewer tests, the admin production build, changed-file Biome and strict OpenSpec pass. Existing polyfills test warning remains. User browser validation remains manual, with no computer/browser-control tools used.

### Third user capture and vertex assembly (2026-11-05)

The next HAR records all 179 cached texture responses finishing at 10.459 s (previously 11.111 s; original 47.145 s), with median duration 24.611 ms. The 27 appearance batches finish by 3.701 s. The supplied initial Bottom-up screenshot no longer shows appearanceTextures among its visible leaders; addVertex is 815.0 ms self/892.8 ms total and appendPart is 1,142.2 ms total. Individual reloads vary, so the roughly 6% elapsed improvement is a single capture comparison.

Landscape expansion now reuses per-part position/colour/UV scratch values and fills its contiguous zero terrain-weight suffix once. It retains the full weight attribute contract. Saved real-data replay of 1,584,672 vertices with warm prepared parts reduced three full CPU assemblies from 534.07/671.32/771.13 ms to 265.15/243.89/296.30 ms. The SHA-256 over every output geometry array and terrain blend descriptor is identical before/after. This is offline CPU evidence, with the actual browser improvement awaiting a manual user capture. All 318 admin tests and production admin build pass; no computer/browser-control tools were used.

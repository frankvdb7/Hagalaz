# Tasks

## 1. Loading baseline

- [x] 1.1 Add an external reproducible profiling harness for pinned region 12850 and a bounded adjacent selection; record initial visibility/cache conditions, unique asset keys, request/byte counts, time to terrain publication/preparation, textured terrain, first landscape and visible completion, plus scene-publication/texture-creation counts. Distinguish pipeline-ready timings from actual browser paint; do not reopen the user's browser. Maps to A1, A3, A4 and A6.

## 2. Bounded appearance batch API

- [x] 2.1 Add read-only authorized 1..32-key appearance batching using the existing single-appearance assembly owner; verify empty/oversized/invalid batches fail before reads, unauthorized access is rejected, cancellation stops remaining work, duplicate keys are deduplicated and one missing/unsupported key does not discard successful rows. Preserve and run existing single-appearance regressions. Maps to A3 and A5.
- [x] 2.2 Add batch DTOs/client integration and rendering API documentation; verify keyed batch successes match the existing single endpoint numerically for ordinary objects, walls/decorations, mirrors and contour metadata, and failures preserve source identity/status. Maps to A3 and A5.

## 3. Progressive visibility-aware loading

- [x] 3.1 Remove whole-selection region publication barriers and schedule visible floor textures independently from landscape batches in the existing scene store; add delayed-response regressions proving one region/its floor textures publish while another region/appearance remains pending, bounded concurrency and per-key deduplication. Document loading stages. Maps to A1, A3 and A4.
- [x] 3.2 Forward plane/landscape visibility as asset interest independently of region identity; verify initial hidden-plane assets are deferred, additional visible assets load once without repeated region reads, repeated toggles reuse completed assets and unstarted hidden work is deprioritized. Maps to A2 and A5.
- [x] 3.3 Coalesce progressive asset publications with one selection-owned schedule and final flush; add deterministic tests for progress totals/failures, partial success, selection replacement, pending visibility changes, retry without duplicate placements, navigation cancellation and late bitmap cleanup. Update usage guidance. Maps to A1, A4 and A5.

## 4. Preparation and GPU reuse

- [x] 4.1 Reuse unchanged terrain preparation and prepared appearance geometry across progressive publications within the active selection; verify preparation-count regressions, invalidation for arriving neighbor/floor material/visibility inputs, immutable reusable sources and unchanged final geometry/terrain blending. Document cache lifetime and remaining buffer-copy limits. Maps to A4, A5 and A6.
- [x] 4.2 Update shared ModelScene texture synchronization to retain unchanged GPU textures and release only removed/replaced assets while updating material bindings; verify texture creation/disposal counts, HDR/effect/model inspection regressions, camera stability during progressive arrival and complete disposal on navigation. Maps to A4 and A5.
- [x] 4.3 Add accessible current-stage/count feedback to Maps detail, preserving existing admin layout; verify progress while usable content remains visible, dynamic totals when interest expands, explicit failures/retry and keyboard camera/visibility behavior through component tests. Maps to A1, A2, A4 and A5.

## 5. Integrated performance verification

- [x] 5.1 Repeat the baseline under identical pinned inputs/conditions, compare all loading and preparation/upload metrics, verify ground-plane 12850 uses at most 27 appearance batch requests with no hidden-only requests and confirm incremental content before visible completion. Record bottlenecks and unmeasured browser-paint/interaction evidence without claiming a speed multiplier from request count alone. Maps to A1 through A6.
- [x] 5.2 Run focused backend tests, full admin coverage, shared renderer regressions, production admin build, changed-file Biome, Fallow audit, strict OpenSpec and diff/whitespace review; report inherited versus introduced warnings and verify no cache mutation, global loader/cache, new protocol or dependency was introduced. Maps to A3 through A6.

## Workflow follow-up

- Keep this change unarchived until implementation and acceptance checks are complete.
- Sync/archive `maps-region-browser` before this dependent change when explicitly requested.

## Implementation and verification, 2026-11-04

The existing scene store now owns progressive region/asset loading through one selection-scoped coordinator. Region results publish immediately; visible floor work runs independently of two concurrent 32-key appearance batches. Hidden planes are deferred until requested, and repeated visibility changes retain completed assets. A 250ms publication schedule coalesces intermediate assets and flushes the final update. The detail reports dynamic region/appearance/texture counts and failures. CPU terrain/appearance preparation and unchanged GPU textures are reused; existing model assembly, terrain blending and cleanup ownership remain intact.

### Automated checks

- Full `Hagalaz.Services.Cache.Tests` with isolated `maps-dotnet` artifacts: Passed 148, Failed 0, Skipped 0. This builds the affected backend. Batch tests cover request-domain/count validation, deduplication, independent errors, mirrored walls/decorations/contour equivalence, authentication policy and cancellation between keys. Existing service regressions remain green.
- `pnpm run admin:test:coverage`: 304 passed across 27 files. Whole-target coverage is 75.45% statements, 78.88% branches, 84.67% functions and 85.63% lines. The scene store has 15/15 statements and 2/3 branches covered; the selection coordinator has 163/169 statements and 53/59 branches; pure scene preparation has 216/222 statements and 114/129 branches. Remaining coordinator branches include synchronous reentrant completion and some warning/fallback paths; remaining geometry paths include unavailable contour boundaries and existing topology variants.
- `pnpm exec vitest run shared/model-viewer/model-scene.spec.ts shared/model-viewer/terrain-material.spec.ts`: 26 passed, including texture identity preservation/replacement/removal and existing camera, HDR/effect, terrain-material and GPU lifecycle behavior.
- Production admin build and changed-file Biome pass. Fallow audit still reports inherited exports/complexity and exercised class-member false positives, including `MapScenePreparation.terrain` and `CacheMapsService.regionsResource`; there are no introduced complexity findings. No gate, suppression or dependency was changed. Existing ImageSharp NuGet warnings, package-pruning warnings and the admin polyfills warning remain.
- Strict OpenSpec validation and whitespace/diff review pass. No gameplay contract, cache contents, global loader/cache, authentication scope, wire-stream protocol or dependency manifest changed.

### Requirement evidence

| Requirement | Exact test evidence |
| --- | --- |
| Immediate region/floor progress before slower regions/models | `publishes a region and its floor textures while another region and landscape batch remain pending` |
| Hidden-plane deferral and repeated-toggle reuse | `defers hidden planes and loads newly visible assets once without reloading regions`, `defers hidden floor textures and bounds image work ahead of object textures` |
| Bounded batches and visibility during loading | `bounds batches and concurrency and drops unstarted hidden work during loading` |
| Coalescing/final flush and partial retry | `coalesces partial publications and flushes the last batch immediately`, `keeps partial region and appearance failures visible and retries without duplicate content` |
| Cancellation and decoded image ownership | `rejects invalid rectangles and cancels superseded HTTP work and scheduled publications`, `owns decoded bitmaps until replacement and closes late completions` |
| Input validation/auth/cancellation | `AppearanceBatch_RejectsInvalidInputBeforeDecoding`, `AppearanceBatch_RejectsOversizedInputBeforeDecoding`, `AppearanceBatch_RequiresAuthenticationAndInheritsCachePolicy`, `AppearanceBatch_CancellationStopsBeforeTheNextKey` |
| Per-key outcomes and numeric assembly equivalence | `AppearanceBatch_PreservesSingleResultsDeduplicatesAndKeepsPartialFailures`, `AppearanceBatch_MapsPerKeyExceptionsWithoutDiscardingOtherResults`, `AppearanceBatch_MatchesSingleMirroredWallsDecorationsAndContourMetadata` |
| Preparation and GPU reuse | `reuses terrain across landscape publications and invalidates relevant materials and regions`, `reuses prepared appearances until their own texture inputs change`, `retains unchanged GPU textures across progressive additions and disposes only replacements` |
| Component progress/visibility integration | `reports incremental progress and requests a newly visible plane without reloading its region` |

### Loading profile and runtime integration

The external profiling harness and frozen pre-change bundle are under the session visualization directory's `progressive-loading/`. It exercises actual old/new store, pure preparation and shared texture lifecycle using pinned real region DTOs for 12850 and 13106, synthetic triangle appearances/bitmaps, a mocked GPU boundary and controlled request delays. The baseline used the old eager-all-plane loader; both render plane zero. These are pipeline-ready timings, not browser paint, real model-download timings or physical GPU upload timings. No further browser inspection occurred, as requested.

| Selection | Old / new appearance calls | Old / new first textured terrain ms | Old / new first landscape ms | Old / new visible completion ms |
| --- | --- | --- | --- | --- |
| 12850 | 1733 / 27 | 7107 / 384 | 6951 / 384 | 7107 / 799 |
| 12850 and 13106 | 2519 / 47 | 10458 / 507 | 10199 / 507 | 10458 / 1540 |

The harness asserts exactly 27 appearance batches for the default 12850 view and rejects hidden-only appearance keys. Model preparation counts dropped from 1680 to 840 for one region and from 2966 to 1483 for two; unchanged GPU texture objects were retained across progressive publications. Detailed conditions and terrain/texture/publication counts are recorded in `docs/3d-rendering/maps-workspace.md`. Do not derive a production speed multiplier from these controlled timings. Aggregate buffer copying, placement contouring, real model decoding/JSON size and first-paint/interaction latency remain unmeasured limits of this loading-focused change.

The already-running cache service was rebuilt through its Aspire resource command and returned healthy. A POST through `http://localhost:4410/rdx/cache/api/v1/cache/types/objects/render-appearances` returned the expected unauthenticated 401, verifying registration/proxy reachability without changing user authentication or opening the browser. Other AppHost resources remained running.

## 6. Backend benchmark follow-up

- [x] 6.1 Add permanent C# BenchmarkDotNet coverage for pinned terrain/landscape decoding and equivalent individual-versus-batch appearance request handling/JSON serialization, with allocation reporting, fixture validation and optional real-cache input. Verify dry runs and measured Release runs, record conditions/results and document reproducible commands without changing production behavior.

## 7. Texture-loading follow-up

- [x] 7.1 Include complete material metadata in the existing texture image response and consume it in the map client, eliminating the separate map material request. Verify backend metadata equivalence, frontend one-request loading/material fidelity, cancellation and failures with focused tests; build the admin and validate the change. Keep existing endpoint compatibility and bounded loading ownership.

Validation: all 306 admin tests passed, including the 11 scene-store tests; all 5 backend texture-material tests passed in Release. Admin production build, changed-file Biome, strict OpenSpec validation and diff checks passed. Aspire rebuilt only the cache service and reported it healthy. Existing polyfills and NuGet audit/pruning warnings remain; no new code warnings were introduced. Request count changes from two to one per texture; actual browser/network elapsed improvement remains unmeasured.

## 8. Browser texture caching

- [x] 8.1 Add private revalidated browser caching for texture images with a source-and-material ETag checked before PNG conversion. Verify unchanged/weak/list validators return a bodyless 304, source/material changes return fresh 200 content, and existing image metadata and authorization remain intact. Run focused backend tests and strict validation; rebuild the running cache service without restarting unrelated resources. Document that first-load and browser timing improvements remain unmeasured.

Validation: 30 Release tests passed across texture material, model asset and cache authorization classes. This includes weak/strong/list/wildcard validator matches, bodyless 304 responses, nonmatching validators and real cache mutation invalidation for both source pixels and material brightness. Strict OpenSpec and focused diff checks passed. Aspire rebuilt the cache service with zero errors and reported it healthy; existing dependency warnings remain. No frontend logic changed and no browser inspection/timing was performed.

## 9. Rendering-speed follow-up

- [x] 9.1 Batch opaque/cutout draw indices by material in the shared renderer while preserving source topology and all attributes; cache opaque groups and classify transparent faces per geometry update so camera motion only orders transparent faces. Verify draw-group reduction and repeatable CPU camera-update measurements, preserve priority/depth/ties, animation/alpha and particle regressions, run shared renderer tests, admin build and strict validation. Record mocked-GPU limits without claiming measured FPS or reopening the browser.

## 10. Reported object projection regression

- [x] 10.5 Fix transform-only object 12163 shape 10 using client-compatible static zero-state selection of definition 12144/model 11990. Preserve source identity, use selected render metadata, warn about assumed state, and treat selected -1 as hidden rather than choosing another variant. Add pinned real-data and single/batch rotation regressions plus branch-rule tests; verify the real-cache path, run service tests and rebuild the cache service.

Validation: all 183 cache-service tests and 11 existing object-codec tests pass. New real-fixture cases cover source/selected identity, selected geometry and render settings, all four single/batch rotations and explicit static-state warnings. Synthetic cases prove configured-selector versus default-fallback selection, successful hidden single/batch responses with zero model reads, and nullable short versus extended object-ID encoding. The real-cache handler returns HTTP 200 for all four rotations. Aspire rebuilt the current cache service with zero errors and reported it healthy; strict OpenSpec and focused diff checks pass. No frontend or live client-state mutation was introduced; existing dependency warnings remain and no new C# warnings were emitted.

- [x] 10.4 Implement the authorized software-client first-texel fallback for invalid projected faces in rendering endpoints, preserving strict default decoding, geometry, valid UVs and texture/alpha semantics. Propagate explicit face metadata and warnings through assembly, DTOs, shared preparation, map aggregation and the renderer. Verify the pinned model-48743 47-face result, single/batch reads, first-texel sampling independent of UV scrolling, and prior projection regressions; run backend/shared/admin checks and rebuild the cache service.

Validation, 2026-11-05: all 57 decoder, 173 service, 57 shared-viewer and 307 admin tests pass. The original Java client's exact 47-face set and valid face-0 UV golden are pinned in the decoder regression. Tests also cover strict rejection, raw/single/batch rendering, two-model face-offset assembly, warnings, marker propagation and bitmap/HDR shader sampling independent of scrolling while retaining alpha/effect hooks. Both app production builds, configured changed-file Biome lint, strict OpenSpec and diff checks pass. The rendering-mode 800-object/650-model scan has zero nonfinite or unsupported outputs, with five models using fallback on 436 faces. Aspire rebuilt the cache service and reported it healthy. Existing dependency/polyfills and unrelated test nullability warnings remain; shared renderer files are excluded by the Biome configuration. No browser or actual GPU pixel inspection was performed.

- [x] 10.3 Fix object 45152 shape 22/model 48859 by selecting the collapsed-Y projection plane for sloped faces with nonzero oriented Y normal. Add pinned decoder and all-rotation single/batch API regressions, preserve earlier object and invalid-axis tests, scan region 12850 models for remaining nonfinite projection failures, run decoder/service tests and rebuild the current cache service.

Validation: real model 48859 now decodes with finite UVs and all four real-cache appearance rotations return HTTP 200. All 56 model decoder tests and 168 cache-service tests pass; twelve API cases cover three pinned models through single/batch endpoints in all rotations. Existing used-axis rejection and client projection goldens remain passing. The current Aspire cache service rebuilt with zero errors and returned healthy. Strict OpenSpec and focused diff checks pass; existing dependency warnings and an unrelated ItemTypeLogicTests nullability warning remain. The 800-object/650-model pinned region scan still reports five other nonfinite model projections (33355, 33356, 36712, 36541, 48743), documented as unresolved supported-failure outcomes rather than hidden or claimed fixed.

- [x] 10.2 Add permanent API regressions for the reported walls using real model payloads, covering both single/batch reads and all four rotations. Fix the newly reported object 45092 shape 0/model 48778 by preserving client negative-X side selection for collapsed-X mappings on sloped faces. Keep model 36668 and ordinary projection goldens passing; verify invalid used axes still reject. Run decoder/service tests and rebuild only the cache service.

Validation: both real fixtures are hash-pinned; 52 model decoder tests and all 164 cache-service tests pass. Eight permanent API cases exercise both reported object IDs in four rotations, via production object-definition decoding, model decode/assembly and JSON responses from single and batch handlers. They assert geometry counts, finite/nontrivial UVs on affected faces and identical single/batch geometry. Exact collapsed-X negative-side UV goldens cover model versions 12/14/15. The real-cache probe returns HTTP 200 for object 45092 in all four rotations. Aspire rebuilt the current cache resource and reported it healthy. Strict OpenSpec and focused diff checks pass, with no new C# warnings; existing dependency warnings remain.

- [x] 10.1 Fix object 33858 shape 0/model 36668 texture projection by selecting box sides from unscaled orientation, avoiding invalid cancellation through a zero-scale axis. Preserve ordinary projection results and finite-coordinate validation. Pin the real model fixture and add focused decoder regressions, verify the reported real-cache appearance path and rebuild the cache service. Resume verification after the fix.

Validation: reproduced the reported exception against the real revision-742 cache and pinned 1525-byte model 36668. All 48 model decoder tests pass, including existing client UV goldens, exact zero-unused-axis UVs across versions 12/14/15, and rejection when a zero-scale axis is required. All 156 cache-service tests pass. Registered appearance handlers return HTTP 200 with serialized geometry for object 33858 shape 0 in all four rotations using the real cache. Aspire rebuilt only the cache service and reported it healthy. Resumed verification also confirmed 306 admin tests with coverage and 54 shared viewer tests; strict OpenSpec and focused diff checks passed. Existing dependency warnings remain. No browser inspection was performed.

- [x] 10.6 Correct dark terrain tints using the client floor RGB-to-packed-HSL and software palette conversion with fixed gamma 0.7. Reuse the shared palette owner, apply identical tints to ordinary and blended floors before and after texture loading, pin region 12850 earth/grass values, preserve topology and visibility, and run frontend checks. Keep atmosphere and shadow differences outside this base-colour correction.

Validation: the original Java client Class491/Class230/Class356 and extracted Class393 palette (gamma fixed at 0.7) produce riverbank underlay 164 RGB [48,42,42] and grass underlay 160 [66,81,36], replacing raw [28,24,19] and [41,56,15]. Nine shared palette goldens, ordinary textured/untextured earth and grass, and blended-layer regressions pass. All 66 shared-viewer and 309 admin tests pass; both frontend production builds, configured changed-file Biome lint, strict OpenSpec and focused diff checks pass. The existing admin polyfills test warning remains; shared files are outside the configured Biome scope. An initial shared-suite invocation with jsdom failed on a Node file-URL fixture; rerunning with the suite's Node environment passed all tests. No browser/GPU pixel check, backend change or runtime atmosphere/shadow parity is claimed.

- [x] 10.7 Match client neighbourhood underlay HSL averaging and carry spatial terrain tints through ordinary and blended shader paths. Preserve texture weights, draw batching, planes and topology; verify original-client neighbourhood goldens and frontend regressions/builds.

Validation: original Java Class491/Class230/Class356 and fixed-gamma Class393 palette verify real region 12850 local (40,26) neighbourhood sums [779,14146,4694,5546,100], packed HSL 8727, RGB [101,91,44]. A regression exercises those real floor counts through the summed-area sampler; existing seam/order, missing-floor, plane isolation, loading and geometry tests pass. Shader regression requires vertex colours and rejects fixed per-layer tint uniforms, preserving texture weights/material batching. All 310 admin and 66 shared-viewer tests and both production builds pass. Configured changed-file Biome, strict OpenSpec and focused diff checks pass; shared renderer files remain outside Biome scope and the existing admin polyfills test warning remains. HTTP reads from localhost:4410 confirm both neighbourhood averaging and the new spatial-tint shader are served. No browser/GPU visual match is claimed.

- [x] 10.8 Remove repeated terrain preparation during cached texture arrival. Keep declared floor texture IDs stable with renderer fallback for absent images, invalidate terrain only for material modes that change geometry/blending, and reuse neighbourhood sampling within the existing selection preparation owner. Measure identical real-region texture-arrival sequences before/after and add regressions for reuse, special-material invalidation, region changes and missing-texture rendering.

Validation: three real region 12850 CPU replays with plane 0, landscape disabled and 20 floor texture arrivals dropped from 1568.0/1478.9/1707.8 ms to 110.3/102.1/100.0 ms; this excludes network, decoding, GPU and browser work. Tests prove ordinary arrivals/replacements retain geometry, declared IDs survive missing images, alpha/effect changes invalidate, sampler identity survives material/plane changes, and region replacement invalidates sampling. ModelScene regression proves missing-image fallback, arriving-image material selection, unchanged source geometry and fallback after removal. All 311 admin and 67 shared-viewer tests, production admin build, changed-file configured Biome and strict OpenSpec pass. The existing polyfills test warning remains; no backend or shared renderer production code changed.

- [x] 10.9 Reduce renderer stalls during texture arrival by retaining active material instances and unchanged geometry, updating terrain texture uniforms without availability-dependent shader variants, and disposing only retired resources. Preserve texture replacement, first-texel, effects/alpha, particles and ordering semantics. Verify resource lifetimes and stable shader programs with direct shared tests and a controlled arrival replay; inspect live request latency, run frontend tests/builds and report GPU timing limits.

Validation: thirty recent live texture-image spans are 12-29 ms, median 15 ms (server duration only). Direct tests prove twenty arrivals retain terrain geometry, position/weight attributes and material identity with zero disposals; landscape geometry replacement retains its terrain material, and retirement releases it once. Uniform tests prove enabled/sampler updates after compilation without a changed program key or shader source. Effect reuse/replacement regressions preserve live animation, alpha testing and retirement of old callbacks. All 70 shared-viewer and 311 admin tests and both frontend production builds pass; strict OpenSpec and focused diff checks pass. Existing admin polyfills test warning remains, and shared renderer files are excluded from configured Biome. No backend changes, browser reopening, GPU shader compilation time or measured FPS claim is made.

- [x] 10.10 Reuse combined landscape geometry on ordinary texture-only publications, using decoded colour/effect inputs rather than image identity for prepared-model invalidation. Preserve nonneutral material colour changes, legacy unprepared appearance fallback, region/appearance/visibility updates and warnings. Compare the saved real Chrome-data replay before/after, add focused preparation regressions and run frontend tests/builds. Ask the user to collect browser validation; do not use computer/browser automation.

Validation: all 316 admin and 70 shared-viewer tests, production admin build, configured changed-file Biome and strict OpenSpec pass. Focused regressions prove combined geometry/part identity across ordinary image arrivals, replacements and unrelated metadata; correct invalidation/restoration for colour blend, brightness and effect-6 lighting; preserved warnings, new appearances, region expansion and visibility changes. Legacy unprepared-material fallback regression remains passing. Saved real Chrome HAR replay with 840 appearances and 1,584,672 expanded vertices reduced twenty late ordinary arrivals from 10,276.99-11,344.22 ms with twenty combined rebuilds to 11.75-18.51 ms with zero rebuilds, excluding initial preparation. This is offline CPU evidence only. No browser/computer-control tools were used; further browser measurements await the user's manual results. The existing polyfills test warning remains.

- [x] 10.11 Batch consecutive transparent faces after client priority/depth sorting by writing a contiguous transparent draw-index tail. Preserve source topology, animation/particles, cutout/alpha handling, priority/depth/tie semantics and camera updates. Validate the saved real-scene draw-group counts and focused ordering regressions, run frontend checks and leave browser measurement to the user.

Validation: all 72 shared-viewer and 316 admin tests and both frontend production builds pass. New regressions verify adjacent transparent runs do not cross client priorities, opaque prefix stability, camera reversal/depth ties and unchanged index versions when camera movement does not alter sort order. Existing animation, alpha/cutout, particle and first-texel tests remain passing. Saved real-scene replay reduces draw groups from 97,590 to 7,637 (92.17%) and transparent groups from 97,367 to 7,414; the complete face/material sequence hash is identical before/after. This uses mocked WebGL and is not a GPU timing claim. User HAR analysis measures texture completion 22.469 s versus 47.145 s previously, all 179 images HTTP304 and median 22.934 ms. Strict OpenSpec and focused diff checks pass; shared files are outside configured Biome scope and the existing admin polyfills test warning remains. No browser/computer-control tools were used; actual browser improvement from this draw-index change awaits user evidence.

- [x] 10.12 Avoid repeated appearance face-texture scans in the selection loader: collect texture interest once per distinct placement key and retain each immutable appearance's unique texture IDs within its MapSceneLoad. Preserve request order, bounded scheduling, visibility, progress and selection cleanup. Add a focused scan-count/visibility/selection regression, compare the saved real-data interest replay, run admin/shared checks and request manual browser evidence.

Validation: the user-provided HAR measures texture completion at 11.111 s versus 22.469 s previously and 47.145 s originally; all 179 images are HTTP304 and median duration is 23.117 ms. The new regression proves a shared appearance's face arrays are scanned once across fifty repeated placements, progressive arrivals and visibility toggles, and independently in a new selection, while unique requests and progress remain correct. Existing concurrency, cancellation, retry and hidden-plane tests pass. Saved real-data 100-interest-call replay preserves the exact ordered 840-appearance/179-texture catalog, with CPU times 420.47-476.60 ms before versus 60.89-90.12 ms after. All 317 admin and 72 shared-viewer tests, admin production build, configured changed-file Biome and strict OpenSpec pass. The existing polyfills test warning remains. No computer/browser-control tools were used and browser timing for this loader change awaits the user.

- [x] 10.13 Reduce landscape vertex expansion allocations by reusing per-part scratch colour/UV/position values and bulk-padding landscape terrain weights once per assembly. Preserve complete geometry attributes and source inputs; compare real-data geometry hashes/timings, add a padding regression and run frontend checks. Browser validation remains manual.

Validation: user HAR texture completion is 10.459 s versus 11.111 s previously, all 179 HTTP304 and median 24.611 ms. Saved real-data replay of 1,584,672 vertices with warm prepared parts reduces full CPU assembly from 534.07-771.13 ms to 243.89-296.30 ms. Positions, indices, colours, opacities, UVs, texture IDs, priorities, first-texel markers, weights and terrain blend descriptors produce identical hashes before/after. New padding regression preserves blended prefix and full zero suffix across repeated multi-part placements, with independent UV/colour/texture/first-texel values and immutable input. All 318 admin tests, admin production build, changed-file Biome and strict OpenSpec pass. Existing polyfills test warning remains. Browser timing of this allocation change is not measured; the user supplies manual evidence and no computer/browser-control tools were used.

- [x] 10.14 Write RGBA attributes directly into a typed buffer. Preserve colour and opacity values, compare the saved scene replay, and run shared renderer checks and the admin build. Browser measurement remains manual.

Validation: user HAR texture completion 9.364 s versus 10.459 s, with 179 HTTP 304 responses. Saved scene replay produced identical RGBA bytes and draw order. One paired offline setup replay measured 890 ms before and 563 ms after. GPU timing remains pending manual validation. All 72 shared renderer tests and the admin production build pass.

- [x] 10.15 Sort transparent faces once for the first rendered frame using the final camera. Preserve draw order and verify the saved scene replay and shared renderer tests.

Validation: the latest HAR finishes 179 revalidated textures at 10.288 s versus 9.364 s in the prior capture. The downloaded bundle contains direct RGBA packing. The screenshot identifies a different bundle. Saved scene replay preserves the RGBA and draw order hashes after moving the new-mesh sort to the first frame. Geometry updates that compute normals still complete their index immediately. All 72 shared renderer tests and the production admin build pass.

- [x] 10.16 Reduce allocations and index writes during surface grouping and verify draw order in the saved scene replay.

Validation: Five paired offline replays of region 12850 gave median surface grouping times of 71 ms before and 48 ms after. Median setup and first-frame CPU time was 406 ms before and 369 ms after. The draw order and RGBA hashes matched. All 72 shared renderer tests and the admin production build passed.

- [x] Bound map blend buffers to terrain vertices and reuse static source indices. Check draw ranges and measure memory in an offline multi-region replay.

Validation: Three paired offline Node replays used region 12850 and a translated adjacent copy (3,170,196 vertices). Retained heap and ArrayBuffers fell from 925 MiB to 614 MiB after GC. The renderer weight buffer fell from 96.75 MiB to 3.01 MiB. Median assembly was 1084 ms versus 988 ms and renderer setup through the first frame callback was 637 ms versus 510 ms. Positions, colors, indices, draw groups, and the terrain weight prefix matched. The replay used a mocked WebGL renderer. All 106 focused tests and the admin production build passed.

Orbit regression check: Five paired offline replays drove 16 identical camera moves after initial setup. Median render-loop CPU time was 1309 ms before and 1249 ms after. Both versions produced 13,512 draw groups at the final angle and matching draw-order hashes. Browser GPU timing remains pending.

- [x] Stop continuous idle map rendering and verify redraws for camera and scene changes.

Validation: Three paired replays of the two-region fixture showed no queued frame at rest. Compact byte markers reduced retained heap and buffers from 614 MiB to 575 MiB. Median assembly plus first frame was 1481 ms before and 1460 ms after; 16 camera moves took 1242 ms versus 1036 ms. Final draw hashes matched. All 103 focused tests and the admin production build passed. Chrome task manager and GPU timing still require a browser capture.

- [x] Share static map color and UV buffers between the assembled scene and renderer. Verify draw and memory/CPU regressions.

Validation: Four paired two-region offline replays retained 575 MiB before and 400 MiB after. Median assembly through the first frame was 1428.5 ms before and 1437.5 ms after. Median CPU time for 16 camera moves was 1003.5 ms before and 979 ms after. Position, color, UV, draw indices, blend weights and draw groups matched. The replay mocks WebGL. All 101 focused tests and both web builds passed.

- [x] Reduce static vertex assembly work and pack sequential map indices directly. Compare memory, setup, orbit CPU and draw output.

Validation: Four paired offline replays of two adjacent region copies retained 400 MiB before and 383 MiB after. Median setup through the first frame was 1400.5 ms versus 1324 ms. Median CPU time for 16 camera moves was 954 ms versus 950.5 ms. Draw data hashes and draw group counts matched. All 104 focused tests and both web builds passed. Browser GPU timing remains unverified.

- [x] Compact prepared landscape parts while preserving per-corner attributes and cached reuse. Verify the real-data replay and focused tests.

Validation: Region 12850 appearance data retained about 37 MiB in the prepared cache, down from 62 MiB. Two-region offline replay retained 358 MiB across heap and array buffers, down from 383 MiB, with identical draw data hashes and 15,108 draw groups. Median isolated preparation increased from 378 ms to 406 ms. Both measured full scene setups were faster. All 102 focused tests and the admin build passed. Browser and GPU timings remain unverified.

- [x] Reuse successful unconditional texture GET responses with bounded ASP.NET Core Output Caching and preserve live conditional validation.

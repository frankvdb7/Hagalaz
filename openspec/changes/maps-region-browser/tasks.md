# Tasks

## 0. Region catalog and Maps table

- [x] 0.1 Add an authorized metadata-only paginated region listing from index 5's named reference metadata; verify ascending sparse region IDs, deduplicated terrain/landscape pairs, terrain-only/landscape-only rows, actual totals, pagination validation, empty versus unavailable metadata and no container decoding. Document the list API. Maps to A1 and A6.
- [x] 0.2 Integrate a paginated Material Maps table in the existing Types browser with region ID links, coordinates, file-presence columns, ID lookup and Open map; directly test list-store loading/errors/retry, real page totals, keyboard activation and absence of render/model/texture requests while browsing. Maps to A1, A5 and A6.
- [x] 0.3 Connect table actions to the existing `/cache/types/maps/:id` route and preserve list query/page context; verify Back to Maps, browser history, direct links and list loading failures without confusing region IDs and source file IDs. Update the admin navigation guidance. Maps to A1, A5 and A6.

## 1. Render terrain and floor projection

- [x] 1.1 Add render-only terrain/placement types and decode all four planes without changing gameplay map contracts; verify explicit and implicit height opcodes, upper-plane spacing, flags, underlay/overlay IDs, shapes/rotations, source placement planes and existing gameplay regressions. Maps to A3 and A6.
- [x] 1.2 Implement revision-742 implicit height generation and referenced floor-definition decoding; verify fixed client-derived numeric expectations, floor color/material inputs and malformed terrain/floor data with focused cache tests. Maps to A3 and A6.
- [x] 1.3 Add authoritative border projection using neighboring terrain samples and explicit completeness metadata; verify shared edges/corners for adjacent regions and missing-neighbor behavior without repeated-row or fabricated-height fallbacks. Maps to A2, A3 and A4.
- [x] 1.4 Import a bounded terrain/landscape reference subset with source commit, revision, cache hash and fixture provenance, and document the render projection alongside `docs/3d-rendering/`; verify expected values against the pinned references without a live Java or network dependency. Maps to A3 and A6.

## 2. Region API and static object appearance

- [x] 2.1 Add the authorized read-only per-region render endpoint with independent terrain/landscape availability, cancellation and optional four-integer XTEA input; verify valid reads, absent files, malformed IDs/keys, landscape decode failure with surviving terrain, successful keyed retry and unchanged existing map contracts. Maps to A1, A4 and A6.
- [x] 2.2 Reuse the existing model assembly owner for an additive placement-aware object appearance projection, including shape selection, orientation, mirroring, definition transform order and footprint/contouring metadata; verify ordinary walls, decorations and static objects numerically against pinned fixtures and run existing model endpoint regressions. Maps to A3 and A6.
- [x] 2.3 Document the render API, availability categories, boundary completeness and object appearance contract in the rendering documentation; verify endpoint registration/authorization tests and ensure failed assets identify their source rather than returning invented empty success. Maps to A4 and A6.

## 3. Pure scene assembly and viewport

- [x] 3.1 Build pure terrain geometry/material assembly from semantic render data, reusing existing model/material preparation where applicable; verify flat/sloped tiles, shaped-overlay rotations, four planes, 512-unit scale, coordinate conversion and multi-region seam positions without a GPU. Maps to A2, A3 and A6.
- [x] 3.2 Add placement-specific static landscape assembly with terrain contouring and region translation; verify distinct rotations/heights, wall/decoration offsets, cross-region placement and unchanged reusable source geometry through deterministic tests. Maps to A2, A3 and A6.
- [x] 3.3 Add the dedicated Three.js map viewport with orbit/pan/zoom, reset, plane/landscape visibility and explicit disposal; verify visibility changes without refetch, camera framing, WebGL-unavailable feedback and render-loop/observer/control/GPU-resource cleanup. Document its boundary in the rendering guidance. Maps to A1, A5 and A6.

## 4. Map detail and request lifecycle

- [x] 4.1 Replace the existing generic Maps detail with the 3D map detail, automatically loading its route region with one-by-one default dimensions; add a signal form for URL-owned dimensions and verify direct links, single-region and nine-region states, invalid ID/dimension/coordinate-overflow rejection, refresh/history restoration and the Maps breadcrumb/return context. Maps to A1, A2, A5 and A6.
- [x] 4.2 Add a detail-scoped Maps NgRx store for region results and deduplicated appearance/texture requests with bounded concurrency; directly test loading/error transitions, partial success, stale response rejection, selection cancellation, local key routing and successful retry without duplicate scene content. Keep listing ownership in the separate table store. Maps to A1, A4, A5 and A6.
- [x] 4.3 Add the advanced per-region key input and visible per-region availability/asset warnings; verify signed-integer validation, missing versus failed landscape feedback, no keys in URLs/storage and useful retry while valid terrain remains visible. Maps to A4, A5 and A6.
- [x] 4.4 Connect the detail viewport and Material controls using the existing admin theme; add component regressions for keyboard camera controls, visibility, bitmap ownership/late completion, and returning to the Maps table or another type. Update admin usage/design guidance for list/detail URLs, region bound and key input. Maps to A1, A5 and A6.

## 5. Integrated verification

- [x] 5.1 Run focused cache/service regressions, the affected builds and the admin test target with coverage; inspect and record changed-code coverage, unsupported branches and inherited versus new warnings. Verify the backend projection and frontend scene inputs against the representative pinned real-cache cases. Maps to A1 through A6.
- [x] 5.2 Inspect the actual admin table and detail at desktop and phone widths; exercise pagination, region click-to-load, Back to Maps with restored context, direct detail links, one region and adjacent regions, camera/visibility, selection replacement, missing terrain, unavailable landscape, key correction/retry and navigation cleanup. Record fixture-backed UI evidence separately from live-cache evidence. Maps to A1 through A6.
- [x] 5.3 Run the production admin build, Biome, Fallow audit, strict OpenSpec validation and whitespace/diff review; confirm the diff implements the table-to-detail flow without adding tile/object inspectors, gameplay behavior changes, cache mutation, speculative dependencies or extra state/retry owners. Maps to A5 and A6.

## Implementation evidence, 2026-11-03

The metadata table, backend rendering APIs and 3D detail are implemented. The existing detail URL loads one region; URL dimensions select up to nine adjacent regions. List and scene requests have separate scoped owners. No gameplay map contract, cache data or dependency manifest changed.

### Automated checks

- `dotnet test Hagalaz.Cache.Tests/Hagalaz.Cache.Tests.csproj --artifacts-path <external>/maps-dotnet --filter "FullyQualifiedName~MapRenderDecoderTests|FullyQualifiedName~MapCodecTests|FullyQualifiedName~MapProviderTests" --verbosity quiet`: 19 passed. Existing gameplay map tests remain green.
- `dotnet test Hagalaz.Services.Cache.Tests/Hagalaz.Services.Cache.Tests.csproj --artifacts-path <external>/maps-dotnet --filter "FullyQualifiedName~MapRegionListingTests|FullyQualifiedName~MapRenderEndpointTests|FullyQualifiedName~DefinitionEndpointRegistrationTests|FullyQualifiedName~ModelAppearanceTests" --verbosity quiet`: 35 passed. These test runs build the affected backend projects. Isolated artifacts avoid the already-running cache service's executable lock.
- `pnpm run admin:test:coverage`: 292 passed across 26 files. Whole-target coverage is 74.30% statements, 78.01% branches, 83.46% functions and 84.49% lines. `pnpm exec vitest run shared/model-viewer/model-scene.spec.ts`: 22 passed, separately from the Angular target.
- `pnpm run admin:build`: production build passed. Targeted Biome across all changed/new TypeScript and HTML passed with `--error-on-warnings`.
- Full-project Biome reports one inherited missing-href error in the portal breadcrumb, 16 warnings and two informational findings. The touched generic action link now has a fallback href. Fallow audit still fails on inherited exports/complexity and service-member false positives: `CacheMapsService.regionsResource`, `appearance` and `texture` have exercised production callsites in the scoped stores. Three input warnings belong to the test-only viewport boundary. There are no introduced complexity findings after simplifying scene assembly. No gate or suppression was weakened.
- Strict OpenSpec validation and whitespace review pass. Existing ImageSharp NuGet audit warnings, six old nullable test warnings and the admin missing-polyfills warning remain. No new build/compiler warnings were introduced by this feature.

Changed-code coverage, statements / branches / functions:

| Source | Covered counts |
| --- | --- |
| Region table store | 23/23, 25/25, 11/11 |
| Region table component TS | 44/49, 37/42, 14/14 |
| Selection/key models | 23/23, 32/32, 5/5 |
| Pure scene assembly | 150/156, 82/98, 26/26 |
| Scene store | 55/64, 11/16, 27/30 |
| Detail component TS | 52/58, 19/34, 17/18 |
| Viewport component TS | 86/119, 21/34, 8/24 |

Viewport coverage excludes real WebGL initialization and most template button callbacks from the Angular unit target. Real browser checks and the separate ModelScene suite cover camera/GPU lifecycle behavior. Remaining scene-store branches include texture failures and unsupported alpha modes. Numeric mesh tests cover slopes, four planes, shaped-overlay rotation, adjacent coordinates, absent boundaries, reusable contoured appearances, footprint rotation, tapering and wall/decoration displacement.

### Requirement evidence

| Behavior | Exact automated evidence |
| --- | --- |
| Metadata-only actual sparse catalog | `ListRegions_PaginatesSparseRegionPairsWithoutReadingPayloads` |
| Empty versus unavailable names and auth | `ListRegions_DistinguishesEmptyIndexFromMissingNames`, `ListRegions_InheritsCacheAuthorizationPolicy` |
| Real terrain/source placement reference | `TerrainProjection_MatchesIndependentClientForEveryTileOfRealRegion`, `LandscapeProjection_MatchesClientBufferTraversalForRealRegion` |
| Independent availability and authoritative borders | `RenderRegion_UsesAuthoritativeNeighborBordersAndIndependentLandscapeStatus`, `RenderRegion_ReportsAbsentFilesAndIncompleteBoundaryWithoutGeneratedHeights` |
| Genuine encrypted-container recovery | `RenderRegion_CorrectKeysRecoverLandscapeWithoutReplacingUsableTerrain` |
| Legacy transform order and multipart mirrors | `ObjectRenderAppearance_RotatesBeforeScaleAndAppliesDecodedOffsetsOnce`, `ObjectRenderAppearance_CombinesCornerWallPartsAndPreservesMirroredUvs` |
| Geometry/placement inputs | `samples authoritative slopes and constructs four planes at legacy scale`, `positions adjacent regions together and rotates actual overlay shapes`, `uses wall displacement for decorations and keeps paired decoration parts independent`, `tapers contouring at the configured height and reports unavailable placement terrain` |
| Store cancellation, partial success and ownership | `keeps valid terrain through partial request and appearance failures and retries cleanly`, `rejects wrapping selections and cancels superseded region requests`, `owns texture bitmaps until replacement and closes late bitmap completions` |
| Detail context and local keys | `loads a direct region link and restores table context through Back to Maps`, `loads adjacent regions and applies keys without putting them in the URL` |
| Camera and destruction | `preserves a navigated map camera on visibility updates and reframes a new selection`, `offers keyboard camera movement without triggering browser scrolling`, `does not create a canvas after destruction before asynchronous initialization` |

### Browser and reference evidence

Real-cache validation used the pinned revision-742 cache through an isolated read-only loopback API. The real catalog has 2622 regions. Region 12850 loads four terrain planes and 5934 source placements. Real single-region and 2-by-2 scenes render terrain, buildings, trees and textures together. Desktop 1440px and phone 390px checks passed; camera and visibility changes made no additional API requests. Back to Maps restores offset 10 and browser Back restores the selected detail rectangle.

Separate fixture-backed browser checks verify nine-region direct links, replacement with two regions without stale content, missing terrain, failed landscape, invalid keys rejected before HTTP, corrected-key reload, explicit retry, canvas removal on navigation and WebGL-unavailable feedback. Authentication was a browser fixture; real cache payloads and API projection were used for the real-scene checks. Production authentication and hosted deployment were not tested. Dense nine-region real-cache performance was not measured.

Scripts, screenshots and logs are outside source in `C:/Users/fmavd/.codex/visualizations/2026/11/01/01a1996d-bcaf-7780-964c-e915cce3a246/`, including `map-real-desktop.png`, `map-real-phone.png`, `map-real-adjacent.png` and the real/failure/boundary browser scripts.

The initial reference-capture gap is resolved for terrain and source placements. `TestSupport/Fixtures/maps/provenance.json` pins GameClient commit `2d6d99555c87d23d5909ff0f9dfea1e58fa5bdda`, cache/payload hashes and independently captured semantic hashes. Terrain calls the existing client decoder; landscape calls existing Buffer primitives following Map source traversal before clipping. Normal tests use committed payloads and constants without Java/network dependencies.

Known static-rendering limits remain explicit. Varp-driven variants use base definitions; contour modes above 2 and other documented appearance modes warn. Some real objects fail the existing model decoder, including degenerate UV projections, and are reported individually while supported terrain/objects remain usable. This is not a claim of complete GameClient pixel parity. No tile/object inspectors, cache mutation, extra retry owner or speculative streaming/caching system was added.

## 6. Terrain appearance follow-up

- [x] 6.1 Preserve decoded floor tint after texture loading and blend ordinary underlay colours/textures across tile edges, including selected-region seams. Reuse ModelScene texture ownership and opaque material disposal. Verify normalized corner weights, independent texture scales, missing-texture fallback, special-overlay isolation, shader variants and existing model regressions; run admin coverage/build and document visual limits.

### Terrain follow-up verification, 2026-11-04

The white-tint regression failed before the fix and passed after preserving decoded colour. Ordinary underlay transitions now interpolate texture samples and colours with normalized vertex weights in one opaque shader pass, preserving per-layer scale. Special/transparent materials remain on their established path.

- Full admin coverage target: 296 tests passed across 27 files. Whole-target coverage: 74.63% statements, 78.32% branches, 83.83% functions, 84.81% lines. `terrain-blending.ts` has 34/34 statements and 16/16 branches covered; pure map geometry has 187/194 statements and 97/113 branches covered. Remaining geometry branches include unavailable contour-boundary paths and ordinary topology branches outside this fix.
- Shared `model-scene.spec.ts` and `terrain-material.spec.ts`: 25 tests passed. Regressions cover shader sampling/scale, missing-image fallback, both weight attributes, shader-cache variants, material grouping and disposal on scene replacement, plus existing model camera/render lifecycle.
- Exact new regressions include `retains region 12850 grass colour when its terrain texture finishes loading`, `emits continuous normalized texture weights at terrain material boundaries`, `crossfades neighbouring textures at a shared region edge without changing tile centers`, and `groups blended terrain separately and disposes its material on scene replacement`.
- Production admin build and targeted Biome passed. Fallow audit has no introduced complexity findings; inherited findings and the previously documented Angular/NgRx false positives remain. Whitespace review passes. Existing polyfills and dependency warnings remain.
- Region 12850 rendered the blended scene in the existing authenticated admin tab with no shader errors, and the user confirmed the result. Further browser inspection was skipped at the user's request. No new backend or authentication changes were required.
- This is bounded interpolation for ordinary underlays, with full client smoothing/overlay/environment parity limits recorded in design and rendering guidance.

## Region editor layout

- [x] Align the region editor with the selected reference using the existing Cache Studio shell and live viewport. Verify region navigation and camera controls.

- [ ] Compare the rendered Region 12850 screen with the supplied reference after a user-provided capture.

- [x] Move region controls into the viewport toolbar and give the live map the available workspace height. Preserve region navigation and loading.

- [x] Select a rendered landscape placement in the viewport and show its identity and tile in the inspector. Verify picking, visibility changes, and selection cleanup.

- [x] Add a viewport-first object editing workspace with local transforms, placement catalog, and inspector tabs. Verify draft changes do not write to the cache.

- [x] Display cache names and on-demand mesh previews in object asset cards without extra image requests.

- [x] Browse the full object catalog, place objects on terrain, and list rendered placements in the sidebar.

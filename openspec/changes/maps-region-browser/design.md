# Design

## Context

See `proposal.md` for the goal and acceptance criteria. Source inspection found:

- `src-admin/app/features/cache/pages/types-manager.page.ts` treats Maps as an unpaginated exact-ID lookup and navigates numeric input to a generic detail route. The shared shell already exposes Maps at `/cache/types?kind=maps`.
- `cache-detail.routes.ts` already registers `/cache/types/maps/:id`. Upgrade that route's generic Maps component rather than introduce a parallel map-detail URL.
- `CacheMapsService` only wraps `GET types/maps/{id}`. `MapDefinitionsEndpoints` also exposes a POST decode operation accepting four XTEA integers. Its DTO contains dimensions, flags-derived object placements and counts, not a renderable terrain surface.
- `MapProvider` resolves index 5 names `m{regionX}_{regionY}` and `l{regionX}_{regionY}` through `ICacheAPI`. Absent files become an empty gameplay type, so that result cannot prove a region exists. `MapCodec.DecodeTerrainData` discards heights and floor appearance. Its object decoder lowers bridge placements to gameplay collision planes, which must not be mistaken for source render planes.
- `ModelEndpoints.GetObjectModel` selects shape models and applies definition scale, offsets, recolors and retextures. It explicitly lacks placement rotation and contouring and rejects inverted objects. It is a reusable foundation, not an already complete landscape renderer.
- Installed Three.js and `shared/model-viewer/` already provide model preparation, textures, material semantics and a GPU lifecycle. `ModelScene` fits a camera without moving source vertices. A whole-selection aggregate mesh can reuse that lifecycle while preserving region coordinates.
- `web-3d-map-rendering` and `docs/3d-rendering/` already specify render semantics, coordinate conversion and parity fixtures. The additional GameClient repository is evidence only. The cache service currently has no automatic region-key source, and GameWorld key state is not a cache-service API.

## Goals / Non-goals

Add a render-only boundary with one owner for cache decoding, one owner for applied selection and requests, and one owner for scene resources. Keep the initial scene bounded and read-only. Reuse the existing model and texture path without changing single-model inspection behavior.

The proposal's exclusions apply. In particular, this design does not introduce another global model cache, a GameWorld dependency, Java runtime integration, whole-world payload decoding, or background world streaming. Listing region metadata across the finite region-ID domain is in scope.

## Decisions

### 1. Use the existing Types table and Maps detail URL

`/cache/types?kind=maps` displays a paginated Material table using the established type-browser layout, search and row actions. Columns show region ID, X/Y coordinates, terrain file presence, landscape file presence and Open map. The ID is a normal focusable link; Open map uses the same route. Browse all actual entries rather than ranges of possible IDs. Numeric lookup opens detail directly, matching the existing type-browser convention. Listing failures and unavailable named metadata remain distinct from an empty cache.

Open `/cache/types/maps/:id` and automatically load the route region in the 3D detail. Query parameters `width` and `height` default to one. The path ID is the sole region anchor; do not introduce a competing `region` query parameter. Invalid IDs/dimensions show an error rather than silently selecting another region. Derive coordinates as `x = id >> 8`, `y = id & 255`; grow the rectangle toward increasing X and Y. Require each dimension in 1..3 and reject coordinate overflow before generating packed IDs.

Preserve the browser's `kind`, `query`, `offset` and `limit` context on navigation into detail. Back to Maps reconstructs `/cache/types?kind=maps` with that context and excludes detail-only dimensions; browser Back restores the same state. A direct detail link works independently and returns to the default table context when none was supplied. The shared cache breadcrumb identifies the region and links back to Maps.

The router owns applied list/detail context. Signal forms own drafts. Camera and visibility stay local; keys stay in detail-component memory. Provide the list and detail stores at their own component boundaries. The list must not create a viewport or trigger render/model/texture requests. Replace the generic Maps detail content with the region viewport and controls. A scene embedded in the table would load expensive assets before the administrator selects a map and would conflict with the requested type-browser flow.

### 2. List actual regions from reference metadata

Add an authorized `GET /api/v1/cache/types/maps` with bounded `offset`/`limit` pagination and an actual total count. Return metadata only: packed region ID, X/Y coordinates, separate terrain and landscape file presence, and corresponding source file IDs where present. Include regions for which either file exists, deduplicate pairs, and order by region ID. Do not use map-provider archive size, raw index file IDs or a dense 0..65535 range as the list of maps.

Read index 5's named reference metadata once per listing request. The existing reference table stores filename hashes in entry `Id`, and `GetFileId` scans entries. Build a request-local hash lookup from that table, reuse its filename-hash calculation, and match `mX_Y`/`lX_Y` names over the finite 256-by-256 coordinate domain. Avoid 131072 repeated linear table scans and never read/decrypt every map container to populate the table. A missing named-reference capability is an explicit listing-unavailable result. No background indexer, persisted catalog or second global cache is needed.

The table store owns listing requests, loading, errors and retry independently from the detail store. Paginate real metadata entries with the existing table page sizes and reject invalid pagination at the API boundary. Tests cover sparse IDs, paired files, terrain-only/landscape-only regions, empty metadata, unavailable names and page totals.

### 3. Project terrain for rendering without changing gameplay

Introduce render-focused terrain and placement types in the cache boundary. Decode raw terrain using the existing documented revision-742 opcode semantics, preserving signed heights, underlay/overlay IDs, overlay shape/rotation and flags on four planes. Decode landscape source planes and preserve bridge information for rendering; do not reuse a placement after its plane has been lowered solely for clipping.

Reuse `ICacheAPI` for index/reference lookup, container reads and XTEA decryption. Keep `IMapType.TerrainData`, gameplay region decoding and collision consumers unchanged. Share small proven byte/opcode primitives where useful, but do not force gameplay types to carry renderer data or create a generic codec framework.

Port implicit height generation from the documented client semantics, including unchecked integer arithmetic and the verified lookup-table behavior. Decode referenced floor overlay/underlay definitions in the cache layer; return semantic base colors/material references rather than raw bytecode or guessed colors.

Supply a 65-by-65 height grid for each renderable plane. Read the required east/north/northeast terrain neighbors through the same request-scoped projection to obtain authoritative boundary vertices. Reuse decoded neighbors within that request. Shared selected-region edges must use the same source samples. If required boundary data is absent, mark it incomplete and omit unsupported boundary geometry rather than repeat the last row or fabricate a height. Any client default-height derivation used instead must first be proven by a pinned fixture.

The simpler existing map DTO cannot support rendering because the information is already discarded. Decoding cache bytecode in Angular would violate the existing server-authoritative spec.

### 4. Add a bounded per-region API with independent availability

Add `POST /api/v1/cache/types/maps/{id}/render` under the existing cache read authorization. The request contains optional `xteaKeys`; omitted keys use the existing normal/unencrypted read behavior, and four zero values remain valid. Validate ID and key domains at the API as well as the UI. The endpoint is read-only and receives cancellation for work still in progress.

Return a semantic region result containing identity/coordinates, separate terrain and landscape availability, decoded planes and border completeness, source placements, referenced floor definitions, and object appearance references/placement metadata. Distinguish a known absent file from a failed decode. Do not claim that a generic decompression failure proves encryption or incorrect keys; give key guidance where appropriate and retain the actual failure category.

Available terrain survives landscape decode failure. Region request failure remains visible for that region instead of failing the complete selection. Include structured warnings keyed to region/object identity. Preserve existing map GET/decode contracts and their callers. No server retry or long-lived projection cache is added.

### 5. Reuse model assembly and add placement-aware object appearance

Expose an additive object render-appearance operation keyed by object ID, placement shape and rotation. It returns prepared static model geometry plus footprint and contouring metadata. Factor the existing backend model assembly only as needed so old inspection endpoints and this operation share one model decoding/recolor/retexture/transform implementation.

Use the verified revision-742 rules for shape-to-model mapping, multi-part walls/decorations, mirroring, rotation and definition transform ordering. Apply definition/shape transforms server-side before exposing the reusable appearance. Keep terrain contouring and world translation placement-specific in the pure scene assembler, where the selected terrain samples are available. Do not rotate an already offset inspection model as a shortcut. Preserve source render plane separately from any derived bridge/collision plane.

The store requests each distinct appearance key and texture at most once per active selection, using bounded HTTP concurrency. It reuses existing texture image/material endpoints and shared model preparation. No new global cache or instancing system is needed initially. Unsupported static appearance modes and varp-driven variants are reported explicitly; a real supported representative set must render, including ordinary walls, decorations and contoured objects.

### 6. Separate semantic scene assembly from the Three.js lifecycle

Pure TypeScript region assembly owns one region-local origin, all region offsets and the legacy-to-web conversion. Preserve 512 legacy units per tile and convert downward legacy height to upward web Y exactly once. Reuse the existing model preparation conversion without applying it again to its output. Assemble floor shape/rotation geometry and materials from decoded data, with placement-specific copies where contouring changes vertices.

A dedicated map viewport adapter uses the existing ModelScene lifecycle for the whole-selection aggregate mesh. ModelScene owns the camera, OrbitControls, render loop, ResizeObserver and GPU resources. The pure assembler applies plane/landscape visibility without HTTP refetches. A small additive camera-preserving update method lets visibility and asset updates retain navigation while a new selection fits its bounds. Reuse shared material/model preparation and keep existing inspection behavior. Dispose scene-owned geometry/material/texture resources and close decoded bitmaps through their request owner.

Start with straightforward per-region/per-appearance batching. Add further batching only if the bounded nine-region browser check demonstrates a concrete need. No speculative LOD, worker, streaming manager or new rendering dependency.

### 7. Give selection, retry and cleanup explicit owners

| Responsibility | Owner |
| --- | --- |
| Table page/search and detail anchor/dimensions | Router path and query parameters |
| Table results, loading/errors and listing retry | Table-scoped Maps NgRx store |
| Detail draft fields, validation and optional keys | Map detail component and signal form |
| Current scene requests, per-region results, asset deduplication, loading/errors | Detail-scoped Maps NgRx signal store |
| Cancel/replace selection and scene retry | Detail store, one current-selection subscription/resource lifecycle |
| Region metadata enumeration | Cache reference-table projection and list API |
| Container lookup, decoding and availability | Cache render projection and API |
| Placement geometry and coordinate conversion | Pure scene assembler |
| Camera, visibility, WebGL resources and render events | Map viewport adapter |

Applying a selection cancels the old request set before replacement. Only the current selection may publish data or asset results. Releasing a request also releases any image bitmap it created, including a bitmap that resolves after cancellation. User Retry reloads failed results for the current selection through the same store path. There is no second automatic retry loop. During replacement, keep textual loading feedback and do not label the old scene as the newly selected regions.

For keys, use a collapsed advanced field accepting a JSON object keyed by selected region ID with arrays of four signed integers. Parse and validate locally before passing the relevant array to each request. Do not place keys in query parameters or storage. Changing region selection does not reuse another region's keys accidentally. No file import or key-service integration is necessary for the first version.

### 8. Follow the existing admin visual system

Use the palette and typography from `src-admin/DESIGN.md`: surface `#0b111b`, container `#192433`, on-surface `#edf1fa`, muted `#b9c9dd`, outline `#39485c` and primary `#ffb300`; keep Semplicita Pro and shared Material overrides.

The Maps landing page uses the same search card, table and paginator as the other types. The detail keeps shared navigation and a Back to Maps breadcrumb, then a compact adjacent-region form, a large scene and a region/status/control column. At phone widths, keep the table scrollable within its card and stack detail controls and scene. Left-align text and use existing `studio-*` surfaces and Tailwind utilities.

```text
[ shared Cache Studio type tabs                    ]
[ Region ID lookup                  ][ Find map    ]
[ ID(link) | X | Y | Terrain | Landscape | Open map ]
[ paginator                                       ]

                         opens detail

[ shared navigation / Maps / Region ID            ]
[ Back to Maps ][ Width ][ Height ][ Load regions ]
[ 3D terrain + landscape        ][ Regions/status  ]
[                              ][ Planes/objects  ]
[                              ][ Camera controls ]
[ Advanced region keys                            ]
```

Provide named camera buttons as a keyboard alternative to pointer gestures, including orbit, pan, zoom and reset. Plane and landscape toggles do not cause reloads. Errors, unsupported-appearance counts and retry actions stay outside the canvas and identify their affected regions. No object/tile inspector is added.

## Risks / Trade-offs

- Terrain flags alone look like usable data but cannot define visible ground. Mitigation: render-only projection and numeric tests for explicit/implicit heights, floors and shapes before UI integration.
- The current object endpoint's isolated preview omits placement semantics. Mitigation: share assembly at the backend owner and add fixture-backed orientation, wall/decoration and contouring cases before claiming landscape support.
- Missing neighbors prevent authoritative outer corners. Mitigation: report incomplete boundaries, omit unsupported geometry and prove shared edges against adjacent real terrain.
- A region rectangle does not bound object/texture density. Mitigation: maximum nine selected regions, deduplicated assets, bounded concurrency and measured browser behavior; report remaining failures instead of silently dropping arbitrary objects.
- Encrypted or corrupt landscape can fail while terrain is valid. Mitigation: independent availability and explicit retry with local keys; no GameWorld coupling or invented empty success.
- The existing semantic rendering specification is ahead of the implemented terrain path. Mitigation: sequence the foundational work inside this change and keep all acceptance criteria incomplete until proven, rather than presenting a terrain-only milestone as the requested finished feature.
- Region IDs differ from index archive IDs and an absent gameplay map can look empty. Mitigation: derive the table from named reference metadata, deduplicate by packed region ID, and keep file presence distinct from successful decoding.

## Migration plan

1. Land the metadata listing and paginated Maps table with focused API/store/navigation tests.
2. Land additive codecs/projections and render API behavior with focused regressions, preserving existing map/model contracts.
3. Land pure assembly and the dedicated viewport, then upgrade the existing Maps detail route and connect its store. Document table/detail URLs, return context, the nine-region bound and key input in the admin guidance.
4. Verify table-to-detail/back and direct links, a pinned real single region and adjacent regions, plus fixture-backed loading/failure/cleanup flows at desktop and phone widths. Record changed coverage and inherited warnings separately.
5. Roll back by restoring the generic Maps lookup/detail and removing the additive list/render endpoints/projections; no cache contents, database migration or saved user data are changed.

## Terrain blending follow-up

The first implementation assigned one flat tint/texture to each triangle. Ordinary opaque underlays now sample the four surrounding tile centers at each generated vertex, normalize available contributions and interpolate both decoded colour and texture in a single opaque shader pass. Shared world coordinates give selected adjacent regions the same seam samples. Each triangle uses at most eight contributing material layers; each layer retains its own texture scale. Existing ModelScene texture/GPU ownership remains authoritative. Overlays and transparent/special-effect underlays retain their existing path.

This is a bounded web rendering improvement, not an implementation of the client's full 10-by-10 hue-weighted HSL smoothing, neighbor-aware overlay substitution or atmospheric/shadow lighting. Unknown or unselected neighboring floor data is not fetched implicitly or invented. Complete client appearance parity remains separate from this fix.

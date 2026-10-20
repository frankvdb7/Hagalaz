## 1. Foundation and Client-Parity Reference

- [x] 1.1 Inventory the current `Hagalaz.Web.App` map/rendering implementation and verify whether the installed MapLibre dependency is actively used.
- [x] 1.2 Trace the authoritative `Hagalaz.GameClient` region, terrain, object, model, scene and graphics-backend paths, separating verified behavior from still-obfuscated details.
- [x] 1.3 Document region/chunk structure, terrain opcodes, legacy units, floor material inputs and the broader object/model requirements that future changes will need.
- [x] 1.4 Document the current Hagalaz gaps, target ownership boundaries, renderer decision and explicitly separated follow-up milestones.
- [x] 1.5 Document byte-level map decode/encode behavior: cache-container versus payload boundaries, `mX_Y` terrain, `lX_Y` object delta coding, smart/huge-smart integers, XTEA, source/effective plane handling, canonical encoding, corruption guards, write-back order and round-trip fixtures.
- [x] 1.6 Record known encoder hazards that must not be treated as cache-faithful behavior: the synthetic `MapCodec` length prefix, bridge-adjusted source-plane loss, unsorted per-object location deltas, `WriteHugeSmart` boundary behavior and the missing XTEA-aware cache writer.
- [x] 1.7 Deobfuscate the rendering documentation vocabulary: define evidence-backed semantic names for terrain, scene graph, tiles, floor definitions, occlusion, scene entities, lights and model members; keep generated GameClient identifiers only in one source-locator map and track source-level renaming separately in `Hagalaz.GameClient`.

## 2. Render-Focused Terrain Decoding

- [ ] 2.1 Add focused failing tests for terrain opcode `0`/`1` height semantics, overlay ID/path/rotation, terrain flags, underlay IDs and upper-plane spacing using deterministic byte fixtures.
- [ ] 2.2 Introduce a render-focused terrain representation that preserves visible terrain data without changing the gameplay meaning of `IMapType.TerrainData`; preserve source encoding information needed for later safe semantic re-encoding rather than retaining only derived values.
- [ ] 2.3 Reuse/factor the existing terrain-flag opcode logic so gameplay and render projections do not maintain conflicting copies of the same decode semantics.
- [ ] 2.4 Define and test deterministic far-edge/border heights suitable for a 64×64 region mesh, using an adjacent-region fixture to prevent seam-by-placeholder behavior.
- [ ] 2.5 Decode/project the minimum overlay and underlay semantic color fields required by the selected representative region; retain texture identity only as future-compatible data rather than expanding into texture rendering.

## 3. Cache-Service Region Contract

- [ ] 3.1 Select and record a representative region with non-flat terrain and multiple floor IDs/shapes/rotations.
- [ ] 3.2 Define one bounded render-region DTO/endpoint exposing four planes of real terrain input without raw cache bytes or an unbounded map operation.
- [ ] 3.3 Keep existing gameplay map endpoint semantics compatible unless a backward-compatible extension is demonstrably simpler than a separate render route.
- [ ] 3.4 Add API validation/contract tests for invalid region input, XTEA handling where applicable, complete terrain semantics and the absence of invented flat fallback data.
- [ ] 3.5 Measure and record the representative JSON payload size/latency; keep straightforward semantic JSON unless that measurement proves it inadequate.

## 4. Single-Region Web Vertical Slice

- [ ] 4.1 Add the dedicated 3D rendering dependency in the implementation commit that consumes it; Three.js is the recommended choice and the dependency justification must be recorded.
- [ ] 4.2 Add one map route/page plus only the render-data service, scene assembler, terrain builder and renderer boundary required by the vertical slice.
- [ ] 4.3 Implement one coordinate-conversion owner using a region-local scene origin and 512 legacy units per tile.
- [ ] 4.4 Build all four terrain planes from real decoded height data, including deterministic region-edge coordinates.
- [ ] 4.5 Apply overlay/underlay base colors plus overlay shape/rotation from decoded semantic data; do not generate placeholder height/material data.
- [ ] 4.6 Add diagnostic orbit/pan/zoom, focus/reset and per-plane visibility controls.
- [ ] 4.7 Add pure TypeScript tests for coordinate conversion, flat/sloped terrain, overlay rotations and edge/corner geometry without requiring a GPU.

## 5. Verification and Scope Gate

- [ ] 5.1 Verify targeted cache decoder tests and region-render API contract tests.
- [ ] 5.2 Verify focused web geometry tests and the relevant Angular build/tests.
- [ ] 5.3 Inspect the representative region in the browser and add screenshot regression only if the render output is deterministic enough to be useful.
- [ ] 5.4 Run the relevant solution/project build, strict OpenSpec validation and `git diff --check`.
- [ ] 5.5 If implementation requires object/model rendering, decoded textures/shaders, multi-region streaming, dynamic-map assembly, live entities or a second cache/state owner, stop and create a separate OpenSpec change instead of expanding this one.
- [ ] 5.6 Do not claim cache write/round-trip support from the existing reduced `MapCodec` tests. Any future mutation milestone must separately prove payload encode/decode parity, XTEA-aware persisted writes and read-back validation against a disposable cache fixture.
- [ ] 5.7 New Hagalaz server/web rendering types MUST use semantic domain names from the deobfuscation map and MUST NOT introduce generated GameClient identifiers into DTOs, APIs, services or TypeScript architecture.

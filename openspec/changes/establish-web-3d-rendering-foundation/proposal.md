## Why

Hagalaz cannot currently display an authoritative RuneScape terrain region in the web UI. `Hagalaz.Web.App` has no active 3D map feature, while the current cache map decoder retains only terrain flags and object placements. The verified game client additionally uses tile heights, overlays, underlays, floor definitions, tile shapes and rotations before handing terrain to its renderer-independent graphics toolkit.

Starting in Angular with placeholder geometry or a second TypeScript cache decoder would create the wrong ownership boundary. The smallest useful foundation is therefore one real 64×64 region whose terrain is decoded once by the existing cache stack, exposed through a bounded render-data contract, and rendered in the browser with a single coordinate-conversion owner.

The current `MapCodec` must also not be mistaken for a full cache round-trip implementation. `MapProvider` combines separate terrain/location payloads into an internal length-prefixed stream, terrain rendering data is discarded, bridge adjustment loses raw source-plane information, and cache writing has no XTEA-aware symmetric path for protected location containers. These format boundaries and encoding requirements must be documented before renderer or editor work builds on incorrect assumptions.

## What Changes

- Document the verified `Hagalaz.GameClient` terrain/scene/renderer pipeline and the broader object/model findings for future work.
- Document byte-level decode/encode rules for map cache containers, terrain payloads, location payloads, smart/huge-smart deltas, XTEA, canonical encoding and persisted-write verification.
- Add a render-focused terrain projection that reuses the existing Hagalaz cache access path while preserving height, overlay, underlay, shape, rotation and terrain flags.
- Expose one bounded cache-service region-render contract instead of sending raw cache bytes to the browser.
- Establish one region-local coordinate conversion and a small browser renderer boundary.
- Implement one real four-plane region terrain vertical slice with diagnostic camera and plane controls.
- Use deterministic client-parity decoder/geometry tests as the correctness gate.
- Record Three.js as the recommended dedicated renderer for the implementation slice, while adding no graphics dependency in this documentation-only foundation commit.

### Existing Mechanisms Reused

- `ICacheAPI`, `IMapProvider`, `MapProvider` and the current cache container/XTEA access path remain the only cache ownership mechanism.
- `Hagalaz.Services.Cache` keeps the existing Minimal API feature/DTO/error patterns for the new bounded render endpoint.
- `Hagalaz.Web.App` keeps its existing Angular routing/HTTP/application structure; the map feature is additive.

### In Scope

- Documentation of the verified game-client 3D pipeline and current Hagalaz gaps.
- Detailed implementation instructions for decoding and semantically encoding `mX_Y` terrain and `lX_Y` location data, including cache-container/XTEA boundaries and known current codec hazards.
- Static terrain for one 64×64 region across four planes.
- Terrain height, terrain flags, overlay ID, underlay ID, overlay shape/path and overlay rotation.
- Source encoding information needed to avoid making later cache re-encoding impossible, without adding a map mutation API in this change.
- Floor definition fields needed to render base colors for the chosen representative region.
- Deterministic region-border/corner-height semantics so the contract does not bake in a seam bug.
- A bounded render-region endpoint/DTO.
- One web map route/page, scene assembler, renderer boundary, orbit/pan/zoom diagnostics and plane visibility controls.
- Tests for cache decoding, API completeness and pure TypeScript terrain geometry/coordinate conversion.

### Non-goals

- A production map/cache mutation endpoint. Encoding rules are documented so data is not thrown away, but persisted write support requires a separate capability and destructive-write test boundary.
- Static object/model rendering. The client object/model analysis is documented now, but its API/renderer implementation is a follow-up change.
- Model geometry endpoints, object render-definition endpoints, animation or live GameWorld entities.
- A model binary encoder; Hagalaz does not yet have a verified server-side model codec and the web viewer does not need model writes.
- Multi-region streaming or dynamic 8×8 region-part rendering.
- Decoded floor textures, custom shaders, lighting, shadows, atmosphere, water, roof hiding or occlusion.
- Porting `GraphicsToolkit`, `Class356`, or another client renderer structure verbatim.
- Decoding RuneScape cache formats in Angular.
- Making MapLibre a custom-layer host for the RuneScape 3D scene.
- Browser persistent caching, worker pools, LOD or speculative performance mechanisms.
- Adding render-only fields indiscriminately to gameplay `IMapType` / `IObjectType` contracts.

### Acceptance Criteria

- `docs/3d-rendering` explains the verified client pipeline, terrain opcode semantics, coordinate units, broader object/model requirements, current Hagalaz gaps, and follow-up milestones.
- `docs/3d-rendering` contains a byte-level map codec guide that distinguishes real cache files from the internal `MapCodec` stream and gives deterministic instructions for terrain/location decode, semantic encode, smart/huge-smart coding, source/effective planes, XTEA container handling, corruption guards and persisted-write verification.
- Known limitations of the current reduced encoder are explicitly documented rather than described as cache-faithful round-trip support.
- The server can represent one renderable region with all terrain semantics needed for correct four-plane geometry and base floor coloration without changing the gameplay meaning of `IMapType.TerrainData`.
- A bounded cache-service endpoint returns that semantic region representation; the browser does not decode raw terrain cache bytes.
- The web viewer renders the chosen representative region from real decoded heights and floor shape/rotation/material data, with no generated placeholder height map.
- Coordinate conversion has exactly one owner, uses a region-local scene origin, and keeps terrain units internally consistent.
- Decoder/API/geometry tests cover the verified client semantics before screenshot comparison is used as a fidelity signal.
- No new queue, store, retry mechanism, persistent browser cache or unrelated graphics abstraction is introduced.

### Stop Conditions

Pause and create a follow-up change if the representative terrain slice requires static object/model rendering, live GameWorld data, texture/shader fidelity, multi-region streaming, dynamic-region assembly, gameplay contract redesign, a second cache-access mechanism, a new distributed state/cache owner, or production map mutation/write-back support.

## Capabilities

### New Capabilities

- `web-3d-map-rendering`: Hagalaz exposes authoritative render-focused terrain data and the web UI can assemble one real RuneScape map region into an inspectable 3D terrain scene without duplicating cache decoding.

### Modified Capabilities

- None.

## Impact

Expected implementation areas:

- `Hagalaz.Cache` / `Hagalaz.Cache.Abstractions` for the render-focused terrain projection reusing existing cache access.
- `Hagalaz.Services.Cache` for one bounded region-render endpoint/DTO.
- cache/service tests for client-parity coverage.
- `Hagalaz.Web.App` for the single-region map page, terrain assembly and renderer adapter.
- `docs/3d-rendering` for long-lived renderer and codec knowledge.

The current documentation-only foundation commit adds no runtime dependency, API, migration, configuration or distributed-topology change.

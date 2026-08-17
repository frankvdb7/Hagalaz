# Web 3D map renderer foundation

This document describes the current Hagalaz web/cache implementation gaps and the target foundation for displaying real RuneScape scenes in `Hagalaz.Web.App`.

Snapshot: `Hagalaz/main` at `ba134cbcd531a6cd3b35c19b20404084e926bbfc` and `Hagalaz.GameClient/main` at `6eac3762cc46cec484131369691b5221fd1277bf`.

## Executive summary

The current blocker is **data fidelity**, not browser drawing capability.

`Hagalaz.Web.App` contains `maplibre-gl`, but `main` has no map route or 3D scene feature and no MapLibre/Three.js/Babylon renderer implementation. More importantly, the cache service currently cannot return enough information to reconstruct a RuneScape scene:

- `MapCodec` discards explicit heights, overlay IDs/shapes/rotations, and underlay IDs while retaining only terrain flags.
- `MapTypeResponse` does not even return those terrain flags; it returns dimensions and object placements only.
- `IObjectType` is gameplay-oriented and does not expose shape-to-model IDs or model transforms/material overrides.
- Hagalaz has no decoded model-geometry API for the web UI.
- Hagalaz has no floor overlay/underlay rendering-definition projection or texture delivery path.

Building a more advanced Angular canvas before fixing this contract would create placeholders that later need to be replaced.

## Current implementation inventory

### Web UI

`Hagalaz.Web.App/package.json` includes `maplibre-gl`, Turf helpers, protobuf and IndexedDB support. On `main`:

- there is no `map` route in `main-routing.ts`;
- no Angular map/scene feature directory was found;
- there is no `maplibre-gl` source usage;
- there is no Three.js or Babylon dependency/usage;
- there is no renderer/camera/terrain/model abstraction.

Treat MapLibre as an unused dependency until a separate map-overview feature proves a need for it.

### Cache map decoding

`Hagalaz.Cache/Logic/Codecs/MapCodec.cs` currently models a map for game-world purposes:

```text
IMapType
  Id
  TerrainData: sbyte[4,64,64]   // terrain flags only
  Objects[]                     // id, shape, rotation, x, y, z
```

The decoder consumes the real terrain stream but intentionally throws away rendering data:

- opcode `1`: explicit height byte is consumed and discarded;
- opcodes `2..49`: overlay payload is consumed and discarded;
- opcodes `50..81`: terrain flag is retained;
- opcodes `82+`: underlay ID is discarded.

This is sufficient for parts of collision/bridge logic but cannot generate visible terrain.

### Cache service map response

`Hagalaz.Services.Cache/Features/Types/TypeEndpoints.cs` exposes:

- `GET /types/maps/{id}`;
- `POST /types/maps/{id}/decode` for XTEA-protected data.

The mapped response contains ID, terrain dimensions, object count, and placements. It does **not** serialize `TerrainData`. Even a debug flag renderer cannot be built from the current endpoint without changing the contract.

### Object definitions

`IObjectType` exposes gameplay fields such as name, size, solid/gateway/clipping data, varbit ID and actions. The client renderer needs additional render-definition data:

- model IDs grouped/selected by object shape;
- recolor/retexture pairs;
- inversion;
- scale and offsets;
- ground-contouring mode;
- transformation targets;
- animation metadata;
- render/occlusion/shadow flags.

Do not put all of this onto `IObjectType`. A rendering projection should be a separate cache-service concern.

### Models and materials

No decoded `ModelDefinition` equivalent or model endpoint was found in Hagalaz. No floor-overlay/underlay rendering types equivalent to the client `OverlayType` / `Class491` path were found either.

That leaves three missing asset families before object/terrain fidelity is possible:

1. floor definitions;
2. model geometry;
3. textures/material inputs.

## Target architecture

Use a server-decoded, renderer-neutral scene contract and a small web scene assembly layer.

```text
RuneScape cache
    |
    v
Hagalaz.Cache codecs/providers
    |  decode authoritative cache semantics once
    v
Render projections in Hagalaz.Services.Cache
    |
    +--> region render data
    +--> object render definitions
    +--> model meshes
    +--> floor/material/texture data
    |
    v
Angular render-data service
    |
    v
Scene assembler
  - region/chunk placement
  - coordinate conversion
  - terrain mesh generation
  - object model selection/transforms
    |
    v
Scene renderer adapter
    |
    v
Dedicated browser 3D engine / WebGL backend
```

### Ownership boundaries

**Cache layer owns:**

- decoding cache bytes;
- XTEA-aware archive access;
- legacy terrain opcode semantics;
- object definition/model/material decoding.

**Cache service owns:**

- stable HTTP DTOs suitable for rendering;
- validation and limits;
- avoiding transport of unexplained cache bytes to the browser.

**Web scene assembler owns:**

- selecting regions requested by the UI;
- converting legacy coordinates to renderer coordinates exactly once;
- creating terrain geometry from decoded tiles;
- selecting object models by placement shape;
- applying placement rotation + definition transforms;
- disposing/replacing region resources.

**Renderer adapter owns:**

- canvas/context lifecycle;
- camera matrices and resize;
- GPU resources/draw submission;
- picking hooks when introduced.

**Angular component owns:**

- user controls and presentation state only;
- it should not decode cache formats or build individual triangles directly.

## Rendering data contracts

The exact DTO names can be refined during implementation, but the data boundaries should look like the following. These are rendering projections, not replacements for existing gameplay types.

### Region render data

```text
MapRenderRegion
  regionId
  planes[4]
    vertexHeights[65,65] or equivalent corner-height representation
    tiles[64,64]
      terrainFlags
      underlayId
      overlayId
      overlayShape
      overlayRotation
  objects[]
    id
    shape
    rotation
    x
    y
    plane
```

Why a 65×65 height grid: a 64×64 tile surface needs the shared corner on the far X/Y edge. If the decoder internally retains 64×64 tile-origin heights, the projection/build step must deterministically derive the border from neighboring region data. Prefer a contract that makes seams explicit rather than guessing in the browser.

### Object render definition

Keep this focused on static object rendering initially:

```text
ObjectRenderDefinition
  id
  modelGroups[]
    shape
    modelIds[]
  inverted
  recolors[]
  retextures[]
  scaleX/Y/Z
  offsetX/Y/Z
  groundContourMode + parameters
  transform metadata when required
```

Animation can be added later. Shape-to-model selection is mandatory for the first object milestone.

### Model mesh

A first static mesh contract needs enough to recreate the client's `ModelDefinition` geometry without exposing the cache binary format:

```text
ModelMesh
  id
  positions[]
  triangleIndices[]
  faceColors[]
  faceAlpha[]? 
  textureIds[]?
  textureMapping data when required
```

Whether the server returns raw decoded model semantics or a more browser-ready interleaved/indexed mesh should be decided with representative measurements. Start with the smallest lossless projection that permits client-parity geometry/material tests. Do not prematurely bake engine-specific Three.js objects into the API contract.

### Floor/material definitions

Expose proven semantic fields from overlay/underlay decoders, beginning with:

- primary/secondary color;
- texture ID;
- whether an overlay hides the underlay;
- fields required by the terrain-shape/material tests.

Do not expose unnamed client `anInt####` fields merely because they exist. Deobfuscate or demonstrate their effect first.

### Texture delivery

Texture decoding needs its own focused investigation. The browser should receive a normal renderable asset or a documented decoded texture representation. It should not reproduce the proprietary cache texture decoder inside Angular.

## Coordinate system

Use legacy client units inside render DTOs and convert once during scene assembly.

Recommended first implementation:

- 512 units = one tile;
- scene origin = selected region/chunk origin, not absolute world origin;
- web X = legacy X;
- web Z = legacy map Y;
- web Y = negative legacy height.

This keeps decoded model vertex units aligned with terrain units and prevents subtle scale mismatches. Region-local origins also avoid large floating-point positions.

World-coordinate metadata can remain available separately for labels, links, or selecting neighboring regions.

## Terrain mesh strategy

The first terrain implementation should optimize for correctness and inspectability:

1. build one geometry per loaded region/plane or another coarse region-sized batch;
2. share tile corner vertices where the material/normal rules allow it;
3. reproduce overlay tile shape/path and rotation before attempting visual effects;
4. resolve underlay + overlay colors/textures using decoded definitions;
5. calculate normals after the correct height surface exists;
6. avoid one draw object per tile;
7. verify region seams with adjacent-region fixtures.

Do not begin with GPU instancing, LOD, worker pools, IndexedDB caches, or custom shaders. Add them only after profiling the representative multi-region scene.

## Static object strategy

For each placement:

1. fetch/cache its render definition;
2. select the model group matching the placement shape;
3. fetch/cache referenced model meshes;
4. combine models if the definition specifies multiple model IDs;
5. apply definition inversion/recolor/retexture/scale/offset rules;
6. apply shape/orientation rules and placement rotation;
7. contour to terrain when the definition requires it;
8. place in region-local scene coordinates.

Reuse immutable decoded mesh data. Engine-level instancing is valid only when two placements genuinely share the same final geometry/material state; do not let instancing erase per-definition transforms or contouring differences.

## Renderer choice

### MapLibre GL JS

**Do not use MapLibre as the core RuneScape 3D renderer.**

MapLibre is designed around geographic maps, Mercator/globe coordinates, map-owned camera/navigation and custom layers inside its rendering lifecycle. A RuneScape scene uses discrete 64×64 regions, four planes, shaped floor tiles, game models and its own world/camera semantics. A custom MapLibre layer would still require all terrain/model/material work while adding coordinate and context constraints.

Keep MapLibre only if Hagalaz later wants a separate world-overview/navigation experience.

### Raw WebGL2

Advantages: complete control and no rendering dependency.

Disadvantages: Hagalaz would own buffer/material/shader/camera/picking/resource-lifecycle infrastructure that does not differentiate the project. This is too much foundation work unless client-parity shader behavior later requires it.

### Three.js

**Recommended default for the first dedicated 3D scene implementation**, after the render-data contract is ready.

Why:

- `BufferGeometry` maps naturally to decoded model/terrain arrays;
- mature camera, frustum, material, texture, picking and GPU-resource lifecycle support;
- custom shaders remain available for later client-fidelity work;
- keeps the project focused on RuneScape cache/scene semantics instead of generic WebGL plumbing.

Per repository guardrails, do not add the dependency in this documentation change. Add it in the implementation OpenSpec/PR that proves the first terrain vertical slice and records the dependency justification.

## Web feature shape

Avoid an interface for every class. A compact feature can start as:

```text
Hagalaz.Web.App/src/app/main/map/
  map-page.component.*
  rendering/
    scene-renderer.ts          // real external-engine boundary
    scene-assembler.ts         // RuneScape -> renderer coordinates/resources
    terrain-mesh-builder.ts
    object-mesh-builder.ts
  data/
    map-render-data.service.ts // HTTP/DTO boundary
  model/
    render-scene.models.ts
```

This is a target shape, not a requirement to create empty files. Only create a type/service when the first vertical slice uses it.

## Loading and lifecycle

Start with one explicitly selected region. Then add neighboring-region streaming.

When streaming is introduced:

- use Angular request cancellation/abort semantics when selection changes;
- deduplicate model/definition/material requests in the data layer;
- release engine resources when a region leaves the active set;
- keep one owner for each cache/lifecycle responsibility;
- do not add a second IndexedDB cache merely because `idb` is already installed. Add persistent browser caching only after measuring network/decode costs and defining invalidation/versioning.

## Camera and interaction

The first camera should be a diagnostic orbit/pan/zoom camera. Its goal is to verify scene fidelity, not mimic gameplay.

Initial controls should support:

- orbit around a selected point;
- pan;
- zoom/dolly;
- reset/focus region;
- plane visibility toggles.

Later milestones can add:

- tile/object picking;
- coordinate display;
- object definition/model inspection;
- RuneScape-like camera constraints;
- roof/occlusion behavior.

For a developer/admin map viewer, inspection tools are more valuable initially than matching the in-game camera.

## Phased implementation

### Phase 0 — contract/decoder foundation

Goal: make the server capable of representing visible map data without changing gameplay contracts.

- Add render-focused terrain decoding that preserves heights, overlays, underlays, shapes, rotations and flags.
- Add floor overlay/underlay definition decoding required by representative regions.
- Add object render-definition projection with model IDs by shape and static transforms.
- Add model-definition decoding/projection for static meshes.
- Define bounded cache-service endpoints/DTOs.
- Add client-parity decoder tests.

Exit criterion: a test can request one known region and prove all data needed for terrain + selected static objects is present.

### Phase 1 — one real terrain region in the browser

Goal: display one 64×64 region from actual cache data.

- Add dedicated renderer dependency/adapter.
- Build all four planes from real heights.
- Render underlay/overlay shapes with correct rotations and base colors.
- Add orbit camera and plane toggles.
- Verify seams and known tile heights against fixtures/client observations.

Exit criterion: no generated placeholder height map or guessed floor colors are required for the representative region.

### Phase 2 — static objects

Goal: render the real static object scene.

- Decode/fetch object render definitions and model meshes.
- Select models by shape.
- Apply rotation, scale, offsets, recolor/retexture and required contouring.
- Add object picking/inspection.

Exit criterion: representative walls, decorations and standard objects appear in the right tiles/orientation with recognizable client-parity geometry.

### Phase 3 — multi-region scene

Goal: make the viewer useful beyond a single fixture.

- Load adjacent regions around a selected coordinate.
- Handle XTEA-protected regions through the server contract.
- Guarantee terrain seams and resource cleanup.
- Support dynamic 8×8 chunk assembly when required by the viewer use case.
- Profile and optimize batching/model reuse based on measurements.

### Phase 4 — visual fidelity

Only after geometry is correct:

- decoded textures and texture mapping;
- improved material rules;
- lighting/shadows;
- atmosphere/skybox/fog;
- roofs/occlusion;
- water/special effects.

### Phase 5 — animation/dynamic scene data

Optional, depending on viewer goals:

- transformed object definitions;
- object animations/skinning;
- dynamic world objects/NPCs/players;
- live GameWorld overlays.

Do not make Phase 5 a prerequisite for a useful static world viewer.

## Test strategy

### Decoder parity tests

Create small deterministic fixtures from representative cache regions/models and assert the semantics verified in `Hagalaz.GameClient`:

- opcode 0/1 heights;
- upper-plane `-960` default spacing;
- overlay ID/path/rotation;
- terrain flags;
- underlay IDs;
- bridge plane adjustment;
- object shape/rotation/location;
- dynamic 8×8 chunk rotation.

Prefer targeted byte fixtures for opcode tests plus at least one real region integration fixture.

### Object/model tests

- shape selects the expected model group;
- multi-model definitions are combined deterministically;
- scale/offset/rotation conversions are exact;
- recolor/retexture mappings survive the API projection;
- model vertex/triangle counts and selected known bounds match a decoded fixture.

### API contract tests

- invalid region/model IDs are bounded and handled consistently;
- XTEA endpoint requires exactly four keys where applicable;
- responses do not leak raw unexplained cache streams;
- response sizes are bounded for single-resource requests.

### Web geometry tests

Pure TypeScript tests should cover:

- coordinate conversion;
- one flat tile and one sloped tile;
- each overlay shape/rotation used by fixtures;
- neighboring-region seam coordinates;
- object placement rotation;
- plane visibility selection.

These do not require a browser GPU.

### Visual regression

Add screenshot/reference-image testing only once the renderer can deterministically render a representative region. Keep numeric decoder/geometry tests as the primary correctness signal; screenshots are a complementary fidelity signal.

## Performance guardrails

Do not optimize from assumptions. Once Phase 3 exists, measure:

- API payload sizes;
- decode/projection time;
- scene assembly time;
- GPU geometry/material counts;
- draw calls;
- frame time while orbiting representative dense regions;
- memory before/after unloading regions.

Likely optimizations, only after measurement, include:

- region-level terrain batching;
- immutable model mesh reuse;
- material grouping;
- selective instancing;
- background geometry assembly in a worker;
- compressed/binary transport for large mesh data;
- browser persistent caching with explicit cache-version invalidation.

## Risks and controls

| Risk | Control |
| --- | --- |
| Browser reimplements cache semantics and diverges from server/client | Decode once in Hagalaz.Cache and expose semantic DTOs. |
| Gameplay models become polluted by graphics concerns | Add render projections instead of expanding `IMapType`/`IObjectType` indiscriminately. |
| Placeholder renderer looks good but cannot reach client fidelity | Require real height/floor data in Phase 1 exit criteria. |
| Object positions work but models are wrong | Treat shape-to-model selection and transforms as first-class contract data. |
| Region seams crack | Represent/derive border heights explicitly and test adjacent fixtures. |
| Huge absolute world coordinates cause precision issues | Use region-local scene origins. |
| New 3D dependency becomes architecture | Hide only the true engine boundary; keep scene semantics in plain project code. |
| Optimization creates duplicate caches/lifecycles | Profile first and keep one owner per resource cache. |
| Obfuscated client fields become accidental API | Expose only deobfuscated/proven semantics. |

## Concrete missing-work checklist

### Backend/cache

- [ ] Preserve full visible floor-tile decode data instead of terrain flags only.
- [ ] Preserve/derive border heights needed for seamless 64×64 terrain meshes.
- [ ] Decode floor overlay definitions.
- [ ] Decode floor underlay definitions.
- [ ] Investigate/decode texture assets required by the chosen representative region.
- [ ] Decode static `ModelDefinition` geometry.
- [ ] Extend object decoding with a separate render projection containing model groups and transforms.
- [ ] Expose bounded render-focused endpoints/DTOs from `Hagalaz.Services.Cache`.
- [ ] Add client-parity tests before frontend consumption.

### Web

- [ ] Add a map route/page only when the first real-region API is available.
- [ ] Add a dedicated 3D renderer adapter; Three.js is the recommended default decision.
- [ ] Add a single coordinate-conversion owner.
- [ ] Build region/plane terrain meshes from real tile data.
- [ ] Implement floor shapes/rotations/material lookup.
- [ ] Add diagnostic camera and plane controls.
- [ ] Add static object mesh selection/transforms.
- [ ] Add multi-region loading only after the single-region path is correct.
- [ ] Add performance optimizations based on measured bottlenecks.

### Documentation/deobfuscation

- [ ] Keep `game-client-renderer.md` updated as client classes are renamed/deobfuscated.
- [ ] Record texture/material findings before exposing new unexplained fields.
- [ ] Document roof/occlusion/camera behavior when those fidelity phases begin.

## Definition of a solid foundation

The foundation is complete when a developer can answer these questions without reverse-engineering the entire client again:

1. Where do region tiles, objects, materials and models come from?
2. Which server type owns decoding each cache format?
3. What exact terrain opcode semantics must be preserved?
4. How are object shape and rotation used to choose/render a model?
5. What are the coordinate and unit conventions?
6. Which fields are verified and which are still partially obfuscated?
7. What is the renderer boundary in the Angular app?
8. What is the next smallest client-parity milestone and how is it tested?

The accompanying game-client reference and OpenSpec change are intended to keep those answers stable and discoverable.

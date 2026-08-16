## Why

Hagalaz cannot currently reproduce RuneScape 3D map scenes in the web UI from authoritative cache data. The Angular app has no active 3D map feature, while the cache map decoder retains only terrain flags and object placements. The real game client additionally uses tile heights, overlays, underlays, floor definitions, object shape-to-model mappings, model geometry, textures and object transforms before handing a scene to a renderer-independent graphics toolkit.

Without a documented rendering model and server-side render-data boundary, frontend work would either use placeholders or duplicate proprietary cache decoding in TypeScript. Both would create a second source of truth and make later fidelity work expensive.

## What Changes

- Document the verified `Hagalaz.GameClient` terrain, object, model, scene and graphics-backend pipeline in human-readable terms.
- Establish a render-focused cache/service contract separate from gameplay-oriented `IMapType` and `IObjectType`.
- Preserve the full visible terrain semantics required for rendering: height, overlay, underlay, shape, rotation and terrain flags.
- Define the object/model/material data required for static object rendering.
- Establish one coordinate conversion and region-local scene-origin convention for the web viewer.
- Define a small web renderer boundary and a phased implementation that proves one real region before multi-region streaming or visual effects.
- Record Three.js as the recommended first dedicated 3D engine and MapLibre as unsuitable for the core RuneScape scene renderer, while deferring any new dependency to the implementation change that proves the first vertical slice.
- Add client-parity decoder/geometry/API tests before relying on visual inspection.

### In Scope

- Static 64×64 map-region terrain across four planes.
- Dynamic 8×8 region-part semantics needed to keep the data model correct, even if dynamic-map UI support ships after the single-region slice.
- Floor overlay/underlay data required to render representative regions.
- Static object placement, shape-to-model selection and model transforms.
- Static model mesh geometry/material inputs.
- Cache-service render DTOs/endpoints.
- Angular scene assembly, diagnostic camera, plane controls and renderer boundary.
- Documentation and targeted client deobfuscation needed by these milestones.

### Non-goals

- Port the original `GraphicsToolkit` API or scene tile graph verbatim.
- Decode RuneScape cache formats in Angular.
- Make MapLibre a custom-layer host for the 3D game scene.
- Match the gameplay camera, roof hiding, occlusion, atmosphere, water, animated objects, NPCs or players in the first vertical slice.
- Add browser persistent caching, worker pools, LOD, instancing or custom shaders before profiling demonstrates a need.
- Add render-only fields indiscriminately to gameplay `IMapType` / `IObjectType` contracts.

### Acceptance Criteria

- The engine reference documents verified client class responsibilities, terrain opcode semantics, object/model construction, renderer backends, units and open deobfuscation areas.
- The web foundation document identifies the current Hagalaz data/API gaps and defines explicit ownership boundaries and phased milestones.
- A future implementation can request one representative region from the cache service with enough semantic data to build its real terrain without placeholder height/material data.
- Static object rendering can obtain a shape-appropriate model plus the definition transformations needed to place it correctly.
- The browser does not need raw map/model cache decoding logic.
- Coordinate conversion has exactly one owner and uses region-local scene origins.
- The first renderer implementation is tested numerically for decoder/geometry parity before visual-regression coverage is treated as a correctness signal.
- New graphics dependencies are introduced only in the implementation change that exercises them.

### Stop Conditions

Pause and split follow-up work if the first static-region vertical slice requires gameplay contract redesign, live GameWorld entity streaming, full animation/skinning, a new distributed cache, or broad client deobfuscation unrelated to the representative scene.

## Capabilities

### New Capabilities

- `web-3d-map-rendering`: Hagalaz exposes authoritative render-focused scene data and the web UI assembles it into an inspectable 3D RuneScape map scene without duplicating cache decoding.

### Modified Capabilities

- None.

## Impact

Expected implementation areas:

- `Hagalaz.Cache` / `Hagalaz.Cache.Abstractions` for render-focused decoders/projections that reuse the existing cache API.
- `Hagalaz.Services.Cache` for bounded rendering endpoints/DTOs.
- `Hagalaz.Cache.Tests` and cache-service tests for client-parity coverage.
- `Hagalaz.Web.App` for the map feature, scene assembly and renderer adapter.
- `docs/3d-rendering` for long-lived renderer knowledge.

The documentation-only foundation change itself adds no runtime dependency, API, migration, configuration or distributed-topology change.

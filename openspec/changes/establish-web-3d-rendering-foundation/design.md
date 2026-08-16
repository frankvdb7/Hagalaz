## Context

The current Hagalaz cache map path was built for GameWorld semantics, not rendering. `MapCodec` consumes map terrain bytes but preserves only terrain flags, while map object placement is reduced to ID/shape/rotation/local coordinates. `TypeEndpoints.MapMap(...)` further reduces the HTTP response to dimensions and object placements.

The game client uses a richer two-stage design. `RegionManager` loads/assembles regions, `Map`/`Class274` decode the full tile representation and construct terrain surfaces, `ObjectDefinition` selects and transforms `ModelDefinition` data, `Class356` owns the scene, and `GraphicsToolkit` hides OpenGL/Direct3D/software backends.

The target should preserve that separation of concerns without reproducing the original client's complexity.

## Goals / Non-Goals

**Goals:**

- Establish one authoritative server-side decode of cache semantics required for rendering.
- Keep gameplay types focused while exposing separate rendering projections.
- Make the first useful web milestone a real, inspectable cache region rather than a visual mock.
- Keep RuneScape scene assembly independent from the selected browser graphics engine.
- Preserve the verified 64×64 region, four-plane, 8×8 chunk, 512-unit tile and terrain-opcode semantics.
- Provide deterministic parity tests against documented client behavior.

**Non-Goals:**

- Port client renderer internals or obfuscated data structures directly.
- Implement live game rendering, animation, atmosphere, occlusion or gameplay camera behavior in the initial milestone.
- Add speculative caching/performance infrastructure.

## Decisions

### Decode render semantics on the server

Hagalaz.Cache remains the owner of RuneScape cache-format knowledge. The web app consumes semantic render data through `Hagalaz.Services.Cache` rather than raw cache containers.

This prevents two independently evolving decoders and lets tests compare server decode behavior directly with `Hagalaz.GameClient` semantics.

### Add render projections instead of expanding gameplay contracts

`IMapType` and `IObjectType` serve existing gameplay/cache consumers and intentionally omit most graphics data. Rendering needs separate projections for region tiles, object render definitions, model meshes and floor/material definitions.

The implementation SHOULD reuse existing cache providers and decoded values where they are sufficient. It MUST NOT create a parallel cache-access mechanism merely to support rendering.

### Preserve full terrain input before building meshes

The render path must retain:

- height data;
- terrain flags;
- overlay ID;
- underlay ID;
- overlay shape/path;
- overlay rotation.

A 64×64 region mesh also needs the far-edge corner heights. The server projection should make border/seam behavior deterministic, preferably with a 65×65 vertex-height representation or an equivalently explicit neighboring-border contract.

### Use legacy units at the transport boundary

Rendering DTOs keep cache/client units. One web scene assembler converts to browser-engine axes and establishes a region-local origin.

The first implementation keeps 512 scene units per tile so model vertices and terrain use the same scale. For a conventional Y-up engine, legacy map Y becomes web Z and legacy height is negated into web Y.

This conversion MUST have a single owner.

### Build one real region before streaming

The first vertical slice loads one explicitly selected 64×64 region, constructs real four-plane terrain, applies overlay/underlay shape/rotation/color data, and provides diagnostic orbit/plane controls.

Multi-region streaming, dynamic maps and persistent browser caching are deferred until this path is correct and measurable.

### Static object rendering is the second vertical slice

A placement is not enough to render an object. The render definition must select model IDs for the placement shape and preserve the static transforms/material substitutions needed by the client.

Static model decoding comes before animation. Animation/skinning fields should not inflate the first mesh API unless a representative static object genuinely requires them.

### Recommend Three.js for the dedicated scene renderer

MapLibre remains inappropriate as the core scene renderer because its geographic coordinate/camera lifecycle does not match RuneScape regions, planes, shaped floors and game models; a custom layer would still require the full scene pipeline.

Raw WebGL2 would force Hagalaz to own generic camera/buffer/material/picking/resource infrastructure.

Three.js is the recommended first implementation because its `BufferGeometry`, camera, texture, picking and resource abstractions fit the decoded data while allowing custom shaders later. This design does not add the dependency. The implementation PR must justify and add it when the first terrain slice uses it.

### Keep the web renderer boundary small

Do not port `GraphicsToolkit`. The web renderer adapter should own external-engine lifecycle only: initialization, scene attachment/update, camera rendering, resize and disposal.

Terrain/object builders and coordinate conversion remain project code and should be testable without a GPU or Angular component.

### Use correctness tests before visual snapshots

Client-parity decoder fixtures and pure geometry tests are the primary correctness signal. Screenshot regression is added only after deterministic rendering exists and should complement, not replace, numeric assertions.

## Detailed data flow

```text
Cache archives
  | mX_Y terrain; lX_Y objects; model/config/texture archives
  v
Hagalaz.Cache
  | decode/cache-format semantics
  +-- map render region projection
  +-- floor definitions
  +-- object render definitions
  +-- model mesh projection
  +-- decoded textures/material inputs
  v
Hagalaz.Services.Cache
  | bounded HTTP DTOs
  v
MapRenderDataService (Angular)
  | request/dedup/cancellation ownership
  v
SceneAssembler
  | region-local origin + one coordinate conversion
  | terrain mesh builder
  | object mesh builder
  v
SceneRenderer adapter
  v
Three.js (recommended) / selected dedicated browser renderer
```

## Representative-region strategy

Select at least one region that contains:

- non-flat terrain;
- multiple overlay shapes/rotations;
- textured and untextured floors if texture decoding is ready;
- walls, wall decorations, standard objects and floor decorations;
- multi-tile and contoured objects where practical.

Use small hand-built byte fixtures for exhaustive opcode tests and the representative real region for integration/fidelity tests. A second adjacent region should be introduced before Phase 3 to prove border-height seams.

## API shape constraints

Rendering endpoints MUST be bounded by an explicit resource such as one region, one object definition, one model or one material/texture. Do not expose an unbounded "all maps/models" operation.

Large mesh payloads may eventually justify binary transport or compression, but the first contract should optimize for semantic clarity and testability. Change transport representation only after recording payload/latency measurements.

## Failure and lifecycle behavior

- Invalid/missing cache resources should produce the cache service's normal mapped error behavior rather than partial invented geometry.
- XTEA-protected region object data remains server-decoded; the browser supplies/obtains only the API inputs required by the chosen viewer workflow.
- Switching region selection should cancel stale web requests when possible.
- Renderer resources created for an unloaded region must be disposed by one scene/resource owner.
- Model/material reuse should initially be in-memory within that owner. Persistent browser caching is a separate measured optimization.

## Risks / Trade-offs

- **Risk: render DTOs duplicate gameplay data.** → Accept small semantic duplication at the API boundary to keep gameplay contracts clean; share underlying decoders/providers.
- **Risk: a 65×65 height contract needs neighboring data.** → Define border derivation explicitly and cover adjacent-region seams; never silently repeat the last row/column in the browser.
- **Risk: texture behavior is more complex than color-only terrain.** → Ship a color-correct first terrain slice, then add decoded textures as a fidelity increment if representative acceptance criteria permit it.
- **Risk: model projection becomes engine-specific.** → Keep cache/model semantics renderer-neutral and translate to `BufferGeometry` in the web builder.
- **Risk: partially deobfuscated fields leak into stable DTOs.** → Expose only named/proven semantics; document open fields and investigate them when a failing fidelity case requires them.
- **Risk: scene abstraction grows into a second game client.** → Optimize for an inspection viewer: coarse region batches and static scene data first, live/dynamic behavior only when explicitly required.

## Migration Plan

There is no production migration for the documentation foundation.

Implementation should be additive:

1. add render-focused decoding/projections and tests without changing current gameplay consumers;
2. expose bounded cache-service endpoints;
3. add the one-region web feature behind its new route;
4. add static objects;
5. expand to neighboring regions and fidelity features.

Existing `/types/maps/{id}` consumers should not be broken merely to serve the viewer. A new render-focused endpoint or backward-compatible response evolution should be chosen based on existing API consumers during implementation.

Rollback each phase by removing its additive endpoint/feature while leaving existing gameplay cache types unchanged.

## Open Questions

These are intentionally deferred until the milestone that needs them:

- Which exact cache archive/provider should own overlay and underlay definitions in Hagalaz?
- What decoded texture representation is smallest while preserving the client material behavior needed by representative regions?
- Should model mesh transport remain JSON for the first slice or use a compact binary format after measurements?
- Which `Class491` and secondary `OverlayType` fields need human-readable names for visible client parity?
- Is dynamic 8×8 map assembly required by the initial web viewer product use case, or only by a later developer inspection mode?
- Which roof/occlusion rules should the viewer expose versus intentionally allowing users to inspect every plane?

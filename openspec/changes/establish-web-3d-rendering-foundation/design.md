## Context

The current Hagalaz cache map path was built for GameWorld semantics, not rendering. `MapCodec` consumes map terrain bytes but preserves only terrain flags, while `TypeEndpoints.MapMap(...)` reduces the HTTP map response further to dimensions and object placements.

The verified game client has a richer two-stage terrain design. `RegionManager` loads/assembles regions, `Map`/`Class274` decode full tile data and construct terrain surfaces, and `GraphicsToolkit` hides OpenGL/Direct3D/software backends. Object/model rendering exists beyond that path, but it is deliberately outside this change.

The concrete goal is one authoritative 64×64 four-plane terrain region rendered in the browser from server-decoded cache semantics.

## Goals / Non-Goals

**Goals:**

- Preserve the terrain data currently discarded by Hagalaz's map decoder without replacing the existing cache ownership path.
- Expose a bounded render-region projection separate from gameplay-oriented map contracts.
- Render one representative real region in the web UI through one coordinate-conversion owner and one small renderer boundary.
- Prove behavior with client-parity decoder/API/geometry tests.

**Non-Goals:**

- Static objects/models, model APIs, multi-region streaming, dynamic-map rendering, decoded textures, animation, atmosphere, occlusion, roof logic or live entities.
- New distributed state, queues, retry/reconciliation mechanisms or persistent browser caching.
- A port of the client's renderer/toolkit abstractions.

## Decisions

### Reuse the existing cache-access owner

`ICacheAPI`/`MapProvider` and their current container/XTEA mechanisms remain authoritative for obtaining map bytes. Render decoding is an additional semantic projection over that same source; it is not a second cache reader.

There is no new retry or reconciliation owner in this change. Region rendering is request/response data access; existing cache-service error handling remains responsible for failed reads.

### Add a render terrain projection instead of changing gameplay meaning

`IMapType.TerrainData` currently means tile flags to existing consumers. Reinterpreting it as a full graphics tile would silently broaden a gameplay contract.

Introduce a render-focused representation that can preserve, per plane/tile:

- height data;
- terrain flags;
- overlay ID;
- underlay ID;
- overlay shape/path;
- overlay rotation.

Reuse existing decoded flag/object logic where doing so keeps one authoritative implementation. Do not maintain two copies of the same opcode formula.

### Make region borders explicit

A 64×64 terrain surface needs the far X/Y corner heights as well as tile-origin heights. The render projection/build process must define border derivation deterministically. Prefer an explicit 65×65 vertex-height representation when constructing the API result; if the cache decoder retains a different internal representation, the projection owns the conversion.

The browser must not invent a border by repeating the last row/column.

### Preserve legacy units through the service boundary

Transport/domain data stays in legacy client units. The web scene assembler owns the only renderer-coordinate conversion.

For the first vertical slice:

- 512 legacy units = one tile;
- the selected region provides the local scene origin;
- web X maps from legacy X;
- web Z maps from legacy map Y;
- web Y negates legacy height for a conventional Y-up renderer.

Keeping the 512-unit scale also prepares the scene for future client model geometry without introducing another scale convention now.

### Base-color terrain before decoded textures

The first representative region proves height, floor ID, floor shape and rotation correctness. Overlay/underlay definitions expose the semantic color fields required to reproduce recognizable base floor coloration.

Texture decoding/material shaders are a separate fidelity change. Texture IDs may be retained if useful for future compatibility, but missing decoded textures do not expand this change.

### Recommend Three.js, reject MapLibre/raw WebGL for this slice

**MapLibre custom layer rejected:** it adds geographic camera/coordinate ownership without solving RuneScape terrain semantics and would couple the core viewer to the wrong scene model.

**Raw WebGL2 rejected:** it would make Hagalaz own generic buffer, camera, material, picking and lifecycle plumbing before any project-specific value is proven.

**Three.js recommended:** `BufferGeometry` and its camera/resource primitives map cleanly to the region geometry while leaving custom shaders possible later.

No dependency is added by this planning/documentation commit. The implementation task that first renders terrain must add and justify the package under repository dependency rules.

### Keep the web renderer boundary intentionally small

Do not mirror `GraphicsToolkit`. The external-engine boundary should cover only context/scene lifecycle such as initialize, resize, render and dispose. RuneScape coordinate conversion and terrain geometry generation remain plain project code and are testable without a GPU.

Angular page components own user-facing controls, not cache decoding or triangle generation.

### Use numeric correctness tests before screenshots

The cache decoder is tested with small deterministic byte fixtures plus one representative real-region integration fixture. Pure TypeScript tests verify coordinate conversion and terrain geometry. Screenshot comparison can be added after deterministic rendering exists, but it is not the primary proof of opcode/geometry correctness.

## Data Flow

```text
mX_Y terrain cache archive
        |
        v
ICacheAPI / MapProvider
        |
        v
render-focused terrain decode/projection
  - four planes
  - heights
  - flags
  - overlay/underlay
  - overlay shape/rotation
  - explicit border heights
        |
        v
Hagalaz.Services.Cache region-render endpoint
        |
        v
Angular render-data service
        |
        v
Scene assembler
  - region-local origin
  - one coordinate conversion
  - terrain geometry
        |
        v
small renderer adapter
        |
        v
Three.js (recommended implementation)
```

## Representative Region

Implementation must choose and record one region containing enough variation to prove the contract, preferably:

- non-flat terrain;
- more than one overlay/underlay;
- multiple overlay shapes/rotations;
- bridge/terrain flags if available without pulling object rendering into scope.

Use hand-built byte fixtures for exhaustive opcode cases. Add an adjacent-region fixture only to test border-height derivation; rendering multiple active regions is still a follow-up.

## API Shape Constraints

The endpoint is bounded to one explicit region (and whatever query/body inputs the existing XTEA workflow requires). It must not expose an unbounded "all maps" operation or raw cache container bytes.

The first transport should favor semantic clarity and testability. Do not add binary mesh transport or compression until an actual region response has been measured and the JSON representation is proven inadequate.

## Failure and Lifecycle Behavior

- Missing/invalid cache resources use existing cache-service error mapping; the renderer does not invent flat fallback terrain.
- Stale web requests should be cancellable when region selection changes using existing Angular/HTTP cancellation behavior.
- The scene owner disposes renderer resources when the page/region is replaced.
- No retry queue, reconciliation loop, IndexedDB cache or secondary server cache is introduced.

## Risks / Trade-offs

- **Risk: render projection duplicates flag decode logic.** → Factor/reuse the existing opcode semantics so gameplay flags and render flags have one source.
- **Risk: explicit border heights require neighboring data.** → Make border derivation a server projection responsibility and test it with an adjacent fixture before the browser consumes the contract.
- **Risk: color-only floors are less visually faithful than the client.** → Accept this bounded Phase-1 trade-off; preserve semantic IDs and add decoded textures in a later fidelity change.
- **Risk: Three.js leaks into domain contracts.** → Keep HTTP/render-domain data engine-neutral and convert only in the web renderer/geometry layer.
- **Risk: the change expands into objects/models because the client docs cover them.** → Treat object/model sections as documented follow-up knowledge; stop this change if terrain implementation requires them.

## Migration Plan

This foundation is additive:

1. add tested render-focused terrain decoding/projection while leaving current gameplay consumers unchanged;
2. add one bounded cache-service render-region endpoint;
3. add the single-region Angular viewer and renderer dependency;
4. verify the representative region and complete the change.

Existing `/types/maps/{id}` consumers should remain compatible. Prefer a distinct render-focused endpoint unless implementation proves a backward-compatible extension is simpler and does not change existing response meaning.

Rollback by removing the additive endpoint/viewer/projection together; no persisted data migration is involved.

## Open Questions

Resolve these during the implementation tasks without expanding scope:

- Which exact existing cache config archive/provider should own the minimum overlay/underlay color definitions?
- Which representative region provides the best small parity fixture?
- What endpoint shape best fits the existing cache-service route naming while clearly distinguishing gameplay map data from render terrain data?
- Does the first region JSON payload justify any compact height/tile encoding, or is straightforward JSON sufficient?

Object/model APIs, decoded textures, multi-region streaming, dynamic maps and client camera/occlusion behavior are follow-up changes, not open questions for this one.

## ADDED Requirements

### Requirement: Rendering uses authoritative decoded terrain data

Hagalaz MUST decode RuneScape terrain rendering semantics on the server and MUST expose semantic terrain data to the web UI without requiring the browser to decode raw cache containers.

#### Scenario: A region is requested for 3D terrain rendering
- **GIVEN** a cache region is available through the existing Hagalaz cache access path
- **WHEN** the web UI requests that region for 3D rendering
- **THEN** the response MUST contain enough semantic data to reconstruct all four terrain planes
- **AND** the browser MUST NOT need to interpret the original terrain cache bytecode

#### Scenario: Existing gameplay map consumers remain gameplay-focused
- **GIVEN** existing consumers use the current gameplay meaning of `IMapType.TerrainData`
- **WHEN** render-specific height and floor fields are introduced
- **THEN** they MUST be exposed through an explicitly render-focused projection or another backward-compatible boundary
- **AND** existing gameplay terrain-flag semantics MUST NOT silently change

### Requirement: Rendering architecture uses semantic domain names

Hagalaz MUST use evidence-backed semantic rendering names rather than generated GameClient/decompiler identifiers in new server and web architecture.

#### Scenario: A render-focused server or web type is introduced
- **GIVEN** the GameClient source still contains generated identifiers for part of the rendering subsystem
- **WHEN** a new Hagalaz DTO, API contract, service, TypeScript type, renderer abstraction or architecture document represents that concept
- **THEN** it MUST use the canonical semantic vocabulary from `docs/3d-rendering/client-rendering-deobfuscation.md`
- **AND** it MUST NOT expose a generated `Class###`, `Entity_Sub*`, `method####` or equivalent decompiler name as the public/domain concept

#### Scenario: A client field is not understood well enough to name
- **GIVEN** a GameClient render field or behavior remains ambiguous after source tracing
- **WHEN** the bounded terrain-render contract is designed
- **THEN** the implementation MUST either keep that detail internal/unresolved or perform further deobfuscation before exposing it
- **AND** MUST NOT invent a precise public name solely to remove an obfuscated identifier

### Requirement: Region terrain preserves client-parity floor semantics

A renderable map region MUST preserve the terrain information consumed by the verified game-client floor builder: heights, terrain flags, overlay ID, underlay ID, overlay shape/path and overlay rotation for four planes.

#### Scenario: Explicit and implicit height opcodes are decoded
- **GIVEN** a terrain tile contains opcode `0` or `1`
- **WHEN** the render terrain projection decodes the tile
- **THEN** its height MUST follow the verified client semantics
- **AND** upper-plane implicit height MUST preserve the verified spacing relative to the plane below

#### Scenario: Overlay tile data is decoded
- **GIVEN** a terrain tile contains an opcode from `2` through `49`
- **WHEN** the render terrain projection decodes the tile
- **THEN** the overlay ID MUST be preserved
- **AND** the overlay shape/path MUST be derived from `(opcode - 2) / 4`
- **AND** overlay rotation MUST use the same modulo-four behavior as the verified client decoder

#### Scenario: Terrain flag and underlay data are decoded
- **GIVEN** a terrain tile contains an opcode from `50` through `81`
- **WHEN** the render terrain projection decodes the tile
- **THEN** the terrain flag MUST be preserved as `opcode - 49`
- **GIVEN** a terrain tile contains an opcode above `81`
- **WHEN** the render terrain projection decodes the tile
- **THEN** the underlay ID MUST be preserved as `opcode - 81`

#### Scenario: A region surface needs its far-edge vertices
- **GIVEN** a 64×64 terrain region is projected for mesh construction
- **WHEN** the render contract supplies its corner heights
- **THEN** the far X and Y edges MUST be derived deterministically from authoritative terrain data
- **AND** the browser MUST NOT create missing edge heights by repeating a last row or column placeholder

### Requirement: Floor definitions expose the semantic base-color data used by the terrain slice

The render terrain contract MUST make the proven overlay/underlay semantic color inputs required by the representative region available without exposing unexplained obfuscated fields.

#### Scenario: A tile references an overlay or underlay
- **GIVEN** the selected representative region references a decoded floor definition
- **WHEN** that tile is assembled for the terrain scene
- **THEN** the renderer MUST obtain the definition's proven base color semantics from the server-side cache projection
- **AND** MUST NOT substitute a generated placeholder color for data that is available in the cache

### Requirement: The web scene has one coordinate conversion owner

The web implementation MUST convert RuneScape legacy scene coordinates to browser-renderer coordinates in one scene-assembly boundary.

#### Scenario: A region is assembled for the browser renderer
- **GIVEN** legacy terrain data is returned by the cache service
- **WHEN** the web scene assembler creates terrain geometry
- **THEN** it MUST use a region-local scene origin
- **AND** it MUST consistently map legacy map axes and height to the selected web axes
- **AND** the initial terrain scale MUST preserve the legacy 512 scene units per tile

### Requirement: The browser renderer is independent from terrain semantics

RuneScape terrain assembly MUST remain project code independent from the selected browser graphics engine, while engine-specific lifecycle is owned by a small renderer boundary.

#### Scenario: The dedicated browser renderer is introduced
- **GIVEN** the real-region vertical slice needs a 3D rendering library
- **WHEN** that dependency is added
- **THEN** Angular page components MUST NOT own cache decoding or individual triangle construction
- **AND** engine-specific initialization, resize, render and disposal behavior MUST be isolated from the server terrain contract and coordinate semantics

#### Scenario: MapLibre remains installed for other map use cases
- **GIVEN** MapLibre may be useful for a separate geographic-style overview
- **WHEN** the RuneScape 3D terrain scene is implemented
- **THEN** the core scene MUST NOT depend on MapLibre's geographic camera/custom-layer coordinate model

### Requirement: The first web milestone renders one real terrain region

The first 3D-map vertical slice MUST render one representative cache region from authoritative terrain data before multi-region streaming, object models or advanced visual effects are required.

#### Scenario: The representative region is displayed
- **GIVEN** the representative region render contract is available
- **WHEN** the web terrain viewer loads it
- **THEN** all four planes MUST be constructible from decoded real heights
- **AND** floor overlay/underlay identity, overlay shape/rotation and available base color data MUST come from decoded cache semantics
- **AND** no generated placeholder height map MUST be required
- **AND** the viewer MUST provide diagnostic camera controls and plane visibility controls sufficient to inspect the result

### Requirement: Rendering correctness is covered below the screenshot level

Decoder, API and scene-geometry behavior MUST be verifiable with deterministic automated tests independent from GPU screenshot comparison.

#### Scenario: Terrain decoding changes
- **GIVEN** the render-focused terrain decoder is modified
- **WHEN** its focused tests run
- **THEN** fixtures MUST verify height opcodes, overlay shape/rotation, terrain flags, underlays and plane spacing

#### Scenario: Web terrain geometry changes
- **GIVEN** coordinate conversion or terrain mesh generation is modified
- **WHEN** the pure TypeScript geometry tests run
- **THEN** they MUST verify coordinate conversion, representative flat/sloped tiles, overlay rotations and edge/corner positions without requiring a browser GPU

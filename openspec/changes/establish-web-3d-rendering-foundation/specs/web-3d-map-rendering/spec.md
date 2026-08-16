## ADDED Requirements

### Requirement: Rendering uses authoritative decoded map data

Hagalaz MUST decode RuneScape map rendering semantics on the server and MUST expose semantic rendering data to the web UI without requiring the browser to decode raw cache containers.

#### Scenario: A region is requested for 3D rendering
- **WHEN** the web UI requests a renderable map region
- **THEN** the response MUST contain or reference enough semantic data to reconstruct the visible terrain and placed static objects for that region
- **AND** the browser MUST NOT need to interpret the original terrain/object cache bytecode

#### Scenario: Existing gameplay map consumers continue to use gameplay contracts
- **WHEN** render-specific terrain, object, model or material fields are introduced
- **THEN** they MUST be exposed through a rendering projection or another explicitly render-focused boundary rather than making the existing gameplay contracts engine-specific

### Requirement: Region terrain preserves client-parity floor semantics

A renderable map region MUST preserve the terrain information consumed by the verified game-client floor builder: heights, terrain flags, overlay ID, underlay ID, overlay shape/path and overlay rotation for four planes.

#### Scenario: Explicit and implicit height opcodes are decoded
- **WHEN** a terrain tile contains opcode `0` or `1`
- **THEN** its render height MUST follow the verified client height semantics, including upper-plane default spacing relative to the plane below

#### Scenario: Overlay tile data is decoded
- **WHEN** a terrain tile contains an opcode from `2` through `49`
- **THEN** the overlay ID MUST be preserved
- **AND** the overlay shape/path MUST be derived from `(opcode - 2) / 4`
- **AND** the overlay rotation MUST include the region-part rotation with the same modulo-four behavior as the client

#### Scenario: Terrain flag and underlay data are decoded
- **WHEN** a terrain tile contains an opcode from `50` through `81`
- **THEN** the terrain flag MUST be preserved as `opcode - 49`
- **WHEN** a terrain tile contains an opcode above `81`
- **THEN** the underlay ID MUST be preserved as `opcode - 81`

#### Scenario: Adjacent regions are rendered together
- **WHEN** two neighboring 64×64 region surfaces share a border
- **THEN** the terrain height contract/build process MUST determine the shared edge without browser-side guessing or duplicated last-row/last-column placeholders

### Requirement: Static objects use their render definitions and placement shape

The renderer MUST use both object placement data and the object's render definition when constructing a static scene object.

#### Scenario: A placed object supports multiple shapes
- **WHEN** an object placement specifies a shape and rotation
- **THEN** the renderer MUST select the model group associated with that shape
- **AND** MUST apply the placement orientation using the documented client-parity rotation semantics

#### Scenario: An object definition transforms its model
- **WHEN** a selected object render definition contains required static inversion, recolor, retexture, scale, offset or ground-contour data
- **THEN** those transformations MUST be represented in the server rendering contract and applied before the final object is placed in the scene

### Requirement: Model geometry is exposed without engine coupling

Hagalaz MUST provide decoded model data sufficient for the web renderer to construct static object geometry without exposing browser-engine objects or requiring client cache-format decoding.

#### Scenario: A static model is requested
- **WHEN** a referenced model is fetched for rendering
- **THEN** the response MUST preserve the vertex and triangle geometry and the proven face/material inputs required by representative static models
- **AND** the response MUST NOT depend on Three.js, MapLibre, Babylon or another browser-engine object model

### Requirement: The web scene has one coordinate conversion owner

The web implementation MUST convert RuneScape legacy scene coordinates to browser-renderer coordinates in one scene-assembly boundary.

#### Scenario: A region is assembled for the browser renderer
- **WHEN** legacy terrain and model data are placed into a browser scene
- **THEN** the scene MUST use a region-local origin
- **AND** the conversion MUST consistently map legacy map axes and height to the chosen web axes
- **AND** terrain and model geometry MUST use one shared scale, initially preserving the legacy 512 scene units per tile

### Requirement: The browser renderer is independent from scene semantics

RuneScape terrain/object/model assembly MUST remain project code independent from the selected browser graphics engine, while the engine-specific lifecycle is owned by a small renderer adapter.

#### Scenario: The first dedicated renderer is introduced
- **WHEN** a browser graphics dependency is added for the real-region vertical slice
- **THEN** Angular components MUST NOT directly own cache decoding or triangle construction
- **AND** engine-specific initialization, resize, render and disposal behavior MUST be isolated from the render-data decoding contract

#### Scenario: MapLibre remains available for geographic overview use
- **WHEN** the RuneScape 3D scene renderer is implemented
- **THEN** it MUST NOT depend on MapLibre's geographic camera/custom-layer coordinate model as its core scene representation

### Requirement: The first web milestone renders real terrain before visual effects

The first 3D-map vertical slice MUST render one real cache region using authoritative terrain data before multi-region streaming or advanced visual effects are required.

#### Scenario: The representative region is displayed
- **WHEN** the Phase 1 viewer is complete
- **THEN** all four planes MUST be constructible from decoded real heights
- **AND** floor overlay/underlay shape, rotation and available base material data MUST come from decoded cache semantics rather than generated placeholder data
- **AND** the viewer MUST provide diagnostic camera controls and plane visibility controls sufficient to inspect the result

### Requirement: Rendering correctness is covered below the screenshot level

Decoder, API and scene geometry behavior MUST be verifiable with deterministic automated tests independent from GPU screenshot comparison.

#### Scenario: Terrain decoding changes
- **WHEN** terrain decoder behavior is modified
- **THEN** focused fixtures MUST cover height opcodes, overlay shape/rotation, terrain flags, underlays and plane spacing

#### Scenario: Object/model rendering changes
- **WHEN** object render definitions or model projection behavior is modified
- **THEN** tests MUST cover shape-to-model selection and the relevant geometry/transformation invariants before visual-regression screenshots are used as an acceptance signal

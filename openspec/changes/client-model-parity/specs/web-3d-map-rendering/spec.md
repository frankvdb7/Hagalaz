## MODIFIED Requirements

### Requirement: Landscape objects use placement-aware static appearance

The region scene MUST render available ordinary static landscape objects using their decoded placement shape, orientation, footprint and definition appearance, including applicable terrain contouring. Model data MUST be reusable across placements without baking one placement's transforms into another. Unsupported appearances MUST be identified rather than replaced by invented models.

#### Scenario: A static object is placed on terrain
- **GIVEN** a decoded object placement and its supported render appearance
- **WHEN** the region scene is assembled
- **THEN** its model selection, rotation, footprint center, scale, offsets, recolors, retextures and applicable contouring follow the verified revision-742 semantics
- **AND** its model geometry and textures come from the cache
- **AND** walls and wall decorations use their placement-specific assembly rules

#### Scenario: One appearance has multiple placements
- **GIVEN** the same object appearance occurs at different rotations or terrain heights
- **WHEN** those placements are assembled
- **THEN** each appears at its own position and orientation
- **AND** assembling one does not change the source geometry or another placement

#### Scenario: An appearance cannot be rendered
- **WHEN** a model is absent, an appearance is unsupported, or an asset request fails
- **THEN** the workspace identifies the affected object and reason
- **AND** other renderable terrain and objects remain visible
- **AND** it does not substitute a placeholder model

#### Scenario: Inverted placement
- **WHEN** an inverted landscape object is assembled
- **THEN** it uses the same source mirroring as its standalone preview and retains its placement rotation

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

#### Scenario: Tweened landscape model buffers
- **WHEN** an animated object advances between two frames of a tweened sequence
- **THEN** its placement buffers reflect the same client pose sample as the standalone viewer, with the placement transform applied

# Renderer-neutral Web 3D contracts

This document defines the **semantic contract vocabulary** Hagalaz should use when exposing revision-742 scene data to `Hagalaz.Web.App`.

It is the consumer-side companion to the evidence in `Hagalaz.GameClient/docs/rendering/` and the parity workflow in [game-client-parity-fixtures.md](game-client-parity-fixtures.md).

The rule is simple: Hagalaz should preserve RuneScape-specific rendering semantics, but it should **not inherit the Java client's class hierarchy, generated names, OpenGL/D3D/software backend plumbing, or shader implementation structure**.

## Design principles

1. **Server/cache semantics first.** Decode revision-specific cache data once in Hagalaz's cache layer; do not move cache bytecode/format logic into Angular.
2. **Gameplay and rendering projections are separate.** Existing gameplay-facing map/object interfaces should not become giant renderer contracts.
3. **Raw source identity remains traceable.** Region, object, model, material and definition IDs remain available for debugging/parity even when higher-level render projections are used.
4. **Derived render values are explicit.** A DTO should distinguish decoded/raw semantics from values derived for rendering such as smoothed floor colour or generated render-corner UVs.
5. **Placement-specific data is not shared model data.** Ground contouring, world orientation and placement translation must not be baked into a globally shared model asset.
6. **Render corners are first-class.** UVs/normals/material state may require one source vertex to become multiple final render vertices/corners.
7. **Backend-neutral before Three.js.** Three.js can consume the contract, but the transport/domain shape should not be a serialization of Three.js classes.
8. **Level-2 fidelity first.** Geometry, floor/material appearance, static object assembly and model surface semantics come before water/fog/point-light effects.
9. **Unknown means unknown.** Do not create clean public names for client fields whose semantics are still explicitly unresolved.

## Coordinate and unit contract

Revision/cache coordinates remain authoritative until one explicit scene conversion boundary.

Use these established semantics unless a future parity fixture proves otherwise:

```text
1 tile = 512 legacy scene units
region = 64 x 64 tiles
planes = 4
```

Recommended browser conversion:

```text
web X = RuneScape X
web Z = RuneScape map Y
web Y = -RuneScape height
```

Use a region-local origin for browser geometry to avoid unnecessarily large world-space floats.

Do not apply scale/sign swaps independently inside terrain, object and model code. One conversion utility owns them.

## Level 1 — geometry contracts

### `MapRenderRegion`

Represents one normal 64x64 map region using renderer-relevant decoded semantics.

Conceptual shape:

```text
MapRenderRegion
  revision
  regionId
  regionX
  regionY
  planes[4]
  objectPlacements[]
```

### `TerrainRenderPlane`

```text
TerrainRenderPlane
  cornerHeights[65,65]
  tiles[64,64]
```

A 65x65 corner-height representation is preferred because a 64x64 tile mesh requires its far shared X/Y edge explicitly. If the internal decoder retains a different representation, the projection layer should make the border deterministic before sending data to the browser.

### `TerrainRenderTile`

Minimum geometry/source semantics:

```text
TerrainRenderTile
  terrainFlags
  underlayId
  overlayId
  overlayShape
  overlayRotation
```

Do not replace these with prebuilt arbitrary triangles at the cache API boundary unless the raw tile semantics remain available. Shape/rotation are valuable revision knowledge and parity evidence.

### `StaticObjectPlacement`

```text
StaticObjectPlacement
  objectId
  shape
  rotation
  localX
  localY
  plane
```

This is source placement data. It does not contain the fully prepared object mesh.

## Level 2 — terrain appearance

Terrain appearance should be representable without exposing a legacy client floor-model object.

### `FloorRenderDefinition`

Conceptual ordinary floor definition:

```text
FloorRenderDefinition
  id
  sourceRgb?                 // where meaningful
  packedHsl / colour inputs
  materialId?
  textureScale
  proven floor flags
```

For underlays, Hagalaz may additionally expose/cache the decoded components used for neighborhood smoothing:

```text
weightedHue
hueWeight
saturation
lightness
```

For overlays, preserve separately where present:

```text
primaryPackedHsl
secondaryPackedHsl?
hideUnderlay
neighborBlend state          // only after fixture-backed semantics are stable
```

Do not flatten primary/secondary colour or `hideUnderlay` into a generic CSS-like `color`/`opacity` interpretation.

### Terrain appearance projection

The server may precompute client-compatible appearance for the first viewer while retaining source semantics.

A useful tile/mesh result can include:

```text
smoothedUnderlayPackedHsl
triangle ownership: underlay vs overlay
materialId
textureScale
terrain UV inputs / final UV
normal
objectShadow input
pre-palette lightness
optional final RGB
```

If final palette-derived RGB is exposed, keep packed-HSL/source values available as well. The GameClient palette has launch-variable gamma perturbation, so final RGB alone is a weak semantic contract.

### Terrain normals and shadowing

Treat these as separate semantic channels:

```text
terrain slope normal
object-cast shadow intensity/filter
packed-HSL lightness adjustment
scene directional-light response
```

Do not collapse them into one generic `lighting` number merely because a modern shader could combine them.

## Level 2 — material and texture contract

### `MaterialRenderDefinition`

Ordinary material semantics should cover at least:

```text
MaterialRenderDefinition
  id
  textureSize
  hasHdrTexture
  repeatS
  repeatT
  scrollSpeedU
  scrollSpeedV
  alphaMode
  effectId
  effectParameters
```

Keep `effectParameters` deliberately generic where the source material table uses effect-specific payload fields. Water-specific parameter names belong in an effect-specific projection only when the selected effect proves them.

### Alpha modes

Revision-742 behavior currently distinguishes:

```text
0 -> ordinary/default opaque handling
1 -> binary/cutout texture coverage
2 -> preserve/use source texture alpha
```

Public Hagalaz contracts may choose clearer enum member names after GameClient fixture coverage approves them. Until then, retain the raw alpha-mode ID alongside any semantic category.

### Material animation

Ordinary U/V scrolling is distinct from special effect animation.

Do not model every animated material as a water shader.

## Level 2 — model asset contract

### Source/raw model

The cache-facing model representation should preserve topology and definition-level values before renderer-specific preparation.

Conceptually:

```text
ModelDefinitionData
  id
  positions
  triangle vertex indices
  face colours
  face alpha/transparency
  face priorities
  face types/shading modes
  texture/material IDs
  texture-mapping source data
```

This may remain internal to the cache layer if the API exposes a more useful derived mesh.

### `ModelMesh`

The browser-facing renderer-neutral mesh should represent the final static surface inputs needed by a normal 3D engine.

```text
ModelMesh
  id
  renderVertices[]
  triangles[]
  material groups / per-face material state
  bounds
```

### `ModelRenderVertex`

Prefer final render vertices/corners rather than assuming one source vertex owns exactly one normal and UV:

```text
ModelRenderVertex
  position
  normal
  uv?
  colour / static-light colour input?
  sourceVertexIndex?          // useful for diagnostics/parity
```

A source vertex may be duplicated when two faces require different UVs, normals or material boundaries.

### Face surface semantics

Where final per-face metadata remains relevant:

```text
raw face alpha/transparency
shading mode: smooth / flat / special-unresolved
priority/order metadata when required
materialId
```

Ordinary face alpha is inverse opacity:

```text
opacity = (255 - rawAlpha) / 255
```

Raw 254/255 values have special client semantics and must not be converted to ordinary opacity until the fixture-backed interpretation is finalized.

## Level 2 — static object render contract

### `ObjectRenderDefinition`

Definition-level static render data:

```text
ObjectRenderDefinition
  id
  modelGroups[]
    shape
    orderedModelIds[]
  inversion/special preparation inputs
  recolours[]
  retextures[]
  scaleX/Y/Z
  offsetX/Y/Z
  groundContourMode
  groundContourParameter
  transform/morph metadata
```

Shape -> model selection is part of the definition contract. Do not return a flat model-ID list and force Angular to rediscover selection semantics.

### Transform ordering

The established client path must be preserved semantically. The important boundary is that definition preparation and placement-specific deformation are not one operation.

Conceptually:

```text
select ordered model group
combine model definitions
legacy normalization where applicable
special inversion/shape preparation
orientation
recolour
retexture
other proven definition adjustment
scale
definition offset

--- shared/prepared-model boundary ---

copy for placement/animation
terrain contouring
placement-specific translation
scene placement
```

Do not reorder operations because a matrix-based engine makes a different order more convenient.

### Ground contouring

Represent contouring using:

```text
mode
raw parameter
placement coordinates
primary floor height source
secondary floor height source?     // modes that require it
```

A service may return already-contoured placement geometry for the first viewer, but globally cached `ModelMesh` data must remain un-contoured/shared.

Mode 3 has unresolved final axis naming; preserve neutral values until asymmetric fixture coverage proves pitch/roll direction.

## Level 3 — special materials

Special effects are selected from decoded material metadata rather than hard-coded texture IDs.

Known effect categories include:

```text
environment map
ordinary water
underwater
lake water
light absorb
reflection
HD water
HD water wave
one still-unresolved effect category
```

A renderer-neutral special-material contract should expose:

```text
effectId
raw effect parameters
proven decoded effect parameters
fixed/current animation time inputs
capability fallback identity where relevant
```

The web implementation may approximate a historical effect, but it should know **which source effect it is approximating**.

Do not port legacy GLSL/ARB/OpenGL state as the DTO.

## Level 3 — scene point lights

### `ScenePointLight`

```text
ScenePointLight
  position
  radius
  rgb
  affectedTileSpans?
  flicker
```

### `PointLightFlicker`

```text
waveform
speed
amplitude
baseIntensity
phaseOffset
```

The revision-742 runtime intensity contract is documented in GameClient and parity fixtures. Hagalaz/Web can either compute intensity from these inputs or consume fixed-time derived intensity for diagnostic/parity scenarios.

The renderer-neutral contract is the light radius and intensity behavior, not the legacy OpenGL light-slot object.

If exact >4 overlapping-light selection remains unresolved, do not publish a false deterministic ordering contract.

## Level 3 — environment and fog

### `SceneEnvironment`

Conceptual state:

```text
SceneEnvironment
  source map/cell identity
  globalLightRgb
  globalLightIntensity
  directionalResponsePositive?    // keep neutral naming if not finalized
  directionalResponseNegative?
  lightDirection
  skyFogRgb
  rawFogValue
  fogSceneParameter
  hdr/bloom inputs
  environmentMapFaces?
  skybox?
  transition?
```

Environment selection has 8x8-tile granularity in the current client research.

Fog is a linear-fog-oriented client path. Do not rename the raw decoded value to `density` or impose an exponential fog model in the cache contract.

The web renderer is free to approximate client fog visually, but the raw/proven environment state should remain explicit.

## Raw vs derived values

For parity-sensitive fields, preserve both when useful:

```text
raw revision value
        ↓
proven semantic conversion
        ↓
derived renderer-neutral value
```

Examples:

```text
raw material scroll byte -> periods/second
raw object scale -> 128-based scale factor
raw face alpha -> inverse opacity (except sentinels)
raw packed HSL -> palette-dependent RGB
raw light definition -> fixed-time intensity
raw fog value -> scene fog parameter
```

Do not discard the raw value if the derived behavior is revision-specific or still evolving.

## DTO/API versioning guidance

Avoid a generic all-encompassing `SceneDto` that changes whenever a new effect is discovered.

Prefer bounded projections/endpoints whose contracts grow deliberately, for example:

```text
region terrain render projection
object render definition
model mesh
material definition / texture asset
optional environment state
```

When fields are Level-3/optional, make that boundary clear rather than requiring every caller to understand them from day one.

## Parity/testing boundary

GameClient parity JSON is **evidence**, not a production schema package.

Hagalaz tests should normalize Hagalaz's own DTO/domain output against the imported fixture subset described in [game-client-parity-fixtures.md](game-client-parity-fixtures.md).

This allows production contracts to remain idiomatic C#/TypeScript while still proving the same revision-742 semantics.

Recommended comparison order:

1. cache decode/source identity;
2. renderer-neutral semantic state;
3. pure TypeScript mesh/material inputs;
4. optional visual comparison only where it adds signal.

## Naming rules

New Hagalaz APIs must not contain generated GameClient names such as `ClassNNN`, `methodNNNN`, `anIntNNNN`, `Class_xa`, etc.

They also do not need to copy final Java deobfuscation names mechanically.

Prefer names that describe the renderer-neutral role:

```text
FloorModel in GameClient       -> terrain/floor render projection in Hagalaz
GlWaterEffect in GameClient    -> material effect semantics in Hagalaz
Light in GameClient            -> ScenePointLight in Hagalaz
Environment in GameClient      -> SceneEnvironment in Hagalaz
```

If a concept is still unresolved, keep it out of the public DTO or expose a clearly neutral raw field only when it is required for forward-compatible parity work.

## Implementation priority for #423 / #494

### #423 first slice

Require only the Level-1/early Level-2 terrain contract needed for a useful real-region viewer:

- four planes;
- heights;
- floor IDs/shapes/rotations/flags;
- meaningful base floor appearance;
- explicit coordinate conversion.

Do not block #423 on models, water, point lights or atmosphere.

### #494 follow-up

Use the GameClient parity fixture chain to progressively validate:

1. terrain shape/appearance;
2. model/render-corner data;
3. static object model selection/transforms/contouring;
4. ordinary material/alpha/shading behavior;
5. optional Level-3 material/light/environment effects.

This ordering keeps the renderer useful early while preventing later fidelity work from becoming a second reverse-engineering effort inside C#/TypeScript.

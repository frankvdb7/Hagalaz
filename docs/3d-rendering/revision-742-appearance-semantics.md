# Revision-742 appearance semantics for Web 3D

This document is the current renderer-neutral summary of the **Level-2 and Level-3 appearance behavior** established by the later Hagalaz.GameClient rendering research (#145-#150).

It complements [Game client renderer](game-client-renderer.md), which remains useful for region/terrain/scene/object geometry and renderer architecture, but was written before the detailed material, shading, floor-appearance, contouring, special-material and environment investigations were complete.

For production DTO vocabulary, use [Renderer-neutral contracts](renderer-neutral-contracts.md). For executable parity/provenance, use [GameClient parity fixtures](game-client-parity-fixtures.md).

## Fidelity levels

### Level 1 — geometry correct

Already covered primarily by the existing renderer/cache documents:

- four planes;
- terrain heights;
- underlay/overlay IDs;
- overlay shape/rotation;
- object placements;
- model topology;
- shape -> model selection;
- placement transforms/contouring.

### Level 2 — recognizably revision 742

The important appearance semantics now known are:

- decoded materials/textures and texture scale;
- terrain/model UV generation;
- floor packed-HSL preparation and smoothing;
- overlay/underlay shape ownership;
- model normal generation and flat/smooth shading;
- ordinary face transparency/alpha;
- face ordering/priority boundaries;
- static-object model transform ordering;
- terrain contouring;
- object-cast terrain shadows;
- legacy static/directional lighting inputs.

These are the primary semantics Hagalaz #494 should validate before trying to reproduce Level-3 effects.

### Level 3 — close parity effects

Now sufficiently understood to model deliberately, but not required for the first useful viewer:

- special/water material effect identity and parameters;
- material alpha modes;
- animated material phases;
- static/flickering map point lights;
- environment light state;
- linear-fog-oriented environment state;
- HDR/bloom/skybox inputs and transitions.

## Material and texture semantics

The GameClient material table is a real rendering-definition layer, not merely a map from ID to image pixels.

High-confidence ordinary material semantics include:

```text
texture size
HDR companion texture presence
repeat/wrap in S
repeat/wrap in T
U scroll speed
V scroll speed
alpha mode
special effect ID + effect-specific payload
```

Ordinary U/V scrolling uses:

```text
periodsPerSecond = rawByte / 64
```

This ordinary scrolling mechanism is separate from water/special-effect animation.

### Floor texture scale

Floor underlay/overlay texture scale uses legacy scene units and defaults to **512**, the same as one tile.

Terrain texture coordinates derive from scene coordinates:

```text
u = sceneX / textureScale
v = sceneZ / textureScale
```

A web terrain builder should therefore not assign arbitrary `0..1` UVs independently to every tile when client-compatible floor texture scale matters.

## Model texture mapping

Revision 742 supports four mapping families:

```text
0 -> explicit mapping triangle
1 -> cylindrical projection
2 -> box/cube projection selected from transformed face normal
3 -> spherical projection
```

The key public-contract consequence is **render-corner UVs**.

One decoded source model vertex may participate in multiple faces that require different UVs/material boundaries. A web-facing `ModelMesh` must therefore permit source-vertex duplication rather than assuming one UV pair per original vertex.

For initial Hagalaz implementation, returning final renderer-neutral render vertices/corners from the cache/service side is preferable to copying all legacy mapping helpers into Angular.

## Material alpha modes

Three material alpha categories are behaviorally distinguished:

```text
mode 0 -> ordinary/default opaque handling
mode 1 -> binary/cutout texture coverage
mode 2 -> preserve/use source texture's 8-bit alpha
```

Keep the raw mode ID available in parity data until final enum member names are fixture-backed.

This material alpha state is separate from per-model-face transparency.

## Model face alpha

For ordinary face alpha values, the stored byte is a **transparency amount**, not opacity:

```text
opacity = (255 - rawAlpha) / 255
```

Examples:

```text
raw 0   -> fully opaque
raw 64  -> 191/255 opacity
raw 128 -> 127/255 opacity
raw 253 -> 2/255 opacity
```

Raw 254/-2 and 255/-1 also act as special renderer sentinels and must not be flattened to ordinary opacity until representative fixture evidence finalizes their visible semantics.

## Model normals and shading

The client builds integer face normals from triangle cross products and normalizes them to approximately magnitude 256, down-shifting components first where needed to avoid overflow.

Face type behavior:

```text
0 -> accumulate face normal into vertices for smooth shading
1 -> store one flat face normal
2 -> suppressed/special face state in the model render path
```

Smooth faces derive corner lighting from accumulated vertex normals; flat faces use the face normal.

Adjacent model normal merging can add matching vertex-normal sums/counts and can suppress coincident shared faces. This is a separate semantic operation from generic GPU normal interpolation.

### Object ambient / contrast

Object model creation maps definition values into renderer model inputs as:

```text
modelAmbient  = definitionAmbient + 64
modelContrast = definitionContrast * 5 + 850
```

The legacy lighting path is not equivalent to blindly applying a modern `max(dot(N,L),0)` material.

For the first static web viewer, precomputing client-compatible static corner appearance server-side is acceptable if the source semantic inputs remain available for later refinement.

## Face priority and ordering

Model construction/rendering preserves more than triangle index order. Ordering/classification can depend on:

- face priority;
- transparency/special state;
- material/texture state;
- stable original face ordering.

The software path handles ordinary faces before transparency/special faces and, where per-face priorities exist, uses priority groups `0..11` for the special pass.

A representative priority-sensitive fixture is still required before Hagalaz treats every ordering detail as a final public contract.

## Terrain floor colour preparation

### Underlay RGB-derived values

Underlay RGB is converted using RGB components divided by **256.0**, then derives:

```text
saturation
lightness
hueWeight
weightedHue
```

`hueWeight` and `weightedHue` exist specifically for neighborhood colour smoothing.

### Exact smoothing window

At an interior tile, populated underlays contribute from:

```text
X: x - 4 .. x + 5
Y: y - 4 .. y + 5
```

This is an asymmetric **10x10** rolling window, not a conventional 11x11 centered blur.

The smoothed inputs are:

```text
hue        = sumWeightedHue * 256 / sumHueWeight
saturation = sumSaturation / underlayCount
lightness  = sumLightness / underlayCount
```

### Packed HSL

Bright colours reduce saturation before packing:

```text
lightness > 243 -> saturation >>= 4
lightness > 217 -> saturation >>= 3
lightness > 192 -> saturation >>= 2
lightness > 179 -> saturation >>= 1
```

Then:

```text
packedHsl = (((hue & 0xff) >> 2) << 10)
          + ((saturation >> 5) << 7)
          +  (lightness >> 1)
```

Effective layout:

```text
6-bit hue | 3-bit saturation | 7-bit lightness
```

Overlay primary and secondary decoded colours use the same packed-HSL representation; magic `0xFF00FF` maps to the client's transparent/no-colour sentinel.

## Shaped overlays and rotations

Terrain shape tables decide which generated portions belong to overlay vs underlay before rendering.

Tile-local rotation uses the 512-unit coordinate system:

```text
0: (x, y)
1: (y, 512 - x)
2: (512 - x, 512 - y)
3: (512 - y, x)
```

Overlay portions carry their packed HSL/material/texture scale. Underlay portions carry the smoothed underlay packed HSL/material/texture scale.

Some overlays participate in neighbor-aware edge/corner substitution. The behavioral role is strong, but final public names/edge rules should wait for selected parity fixtures rather than being reconstructed in Angular from incomplete prose.

`hideUnderlay` also participates in tile-layer/visibility semantics; it should not be reduced to a generic opacity flag.

## Terrain normals

For ordinary floor appearance, at an interior height-grid point:

```text
dx = height[x+1][y] - height[x-1][y]
dz = height[x][y+1] - height[x][y-1]

rawNormal = (dx, -1024, dz)
normal = normalize(rawNormal)
```

Generated vertices inside a shaped tile bilinearly interpolate corner normals.

A separate point-light floor helper uses a different vertical normal scale; do not assume all client lighting paths share exactly one normal-construction constant.

## Object-cast terrain shadow field

The terrain surface maintains a max-valued object shadow/darkening grid. Qualifying object placement can raise cells, for example with value `50` for some walls.

Before floor vertices are shaded, the shadow field is filtered using integer shifts:

```text
filtered = left  / 4
         + right / 8
         + down  / 4
         + up    / 8
         + center/ 2
```

The weights are intentionally asymmetric and not normalized.

The ordinary floor colour path then starts with:

```text
brightness = 74 - filteredShadow
```

and adjusts only packed-HSL lightness:

```text
lightness = (packedHsl & 0x7f) * brightness >> 7
lightness = clamp(lightness, 2, 126)
shadedPackedHsl = (packedHsl & 0xff80) | lightness
```

Object-shadow darkening and normal/directional lighting are separate stages.

## Packed-HSL palette determinism

The client uses a 65,536-entry packed-HSL -> RGB palette. Palette construction applies a small random gamma perturbation:

```text
gamma ~= random in [0.685, 0.715)
```

Therefore two client launches can produce slightly different final RGB even with identical semantic terrain state.

For parity:

- packed HSL should be asserted exactly;
- final RGB/screenshots require controlled/captured palette state;
- a deterministic gamma such as 0.7 may be visually close but is not automatically pixel-identical to arbitrary client launches.

## Terrain directional lighting

After palette conversion, the OpenGL terrain path can apply a normal/directional multiplier conceptually like:

```text
dot = lightDirection · terrainNormal
factor = baseLight + dot * (dot > 0 ? positiveResponse : negativeResponse)
```

The two response scalars are intentionally left physically neutral until stronger semantics are proven. A modern web implementation may structure shaders differently, but should not replace the client behavior with a generic positive-only Lambert rule while claiming parity.

## Static-object model assembly

For a placement, the definition chooses one shape-keyed ordered model group. Multiple model IDs in that group are decoded and combined in stored order.

The important high-level pipeline is:

```text
placement shape
    -> selected ordered model IDs
    -> decode/combine source models
    -> legacy normalization where needed
    -> definition-level model preparation
    -> placement-specific copy/deformation
    -> final scene placement
```

### Definition-level transform ordering

The established order is:

```text
ambient/contrast model inputs
special inversion/shape adjustment
orientation
recolour
retexture
one still-neutral definition adjustment
scale
definition translation
```

Scale identity is `128` per axis.

Definition offset opcodes decode signed shorts shifted left by two, then use those values as model/scene-unit translations.

Rotation uses a 16,384-unit full circle:

```text
4096 = 90 degrees
2048 = 45 degrees
```

Do not reorder these operations merely because another order is easier to express as matrices.

## Object morph/transform selection

For static object transforms:

- prefer the configured varbit selector when present;
- otherwise use the varp/config selector;
- a valid selector chooses the corresponding transform ID;
- out-of-range selector uses the final fallback entry;
- `-1` resolves to no transformed definition.

Hagalaz may expose this as render-definition metadata without requiring the browser to implement the complete gameplay state system for the first static viewer.

## Ground contouring

Ground contouring is **placement-specific** and must not be baked into a globally shared model mesh.

Five modes are structurally identified:

```text
mode 1 -> full sampled terrain delta applied per vertex
mode 2 -> deformation fades by normalized vertex height below threshold
mode 3 -> rigid four-corner terrain fit/tilt
mode 4 -> conform using secondary floor plus model vertical extent
mode 5 -> blend primary/secondary floor differences by vertex height
```

Ordinary contour paths use bilinear terrain-height sampling in scene coordinates.

Mode 3 final pitch/roll axis naming remains intentionally fixture-gated. A Hagalaz contract should preserve neutral raw angle/fit semantics until an asymmetric terrain fixture proves the direction convention.

## Special materials and water

Revision 742's material effect table has strong structural identities for these categories:

```text
1 -> environment map
2 -> ordinary water
3 -> unresolved effect category
4 -> underwater
5 -> lake water
6 -> light absorb
7 -> reflection
8 -> HD water
9 -> HD water wave
```

These are selected from decoded material metadata. Hagalaz should never identify water solely from a hard-coded texture ID list.

### Capability fallback

Advanced effects can degrade to simpler effects when a renderer lacks the required capability. This fallback identity should be treated separately from the source material's semantic effect ID.

### Animation families

Known examples include:

- ordinary water effect: 5-second noise cycle plus 4-second secondary phase;
- underwater/lake effects with generated-coordinate time behavior;
- HD water with longer animated normal/environment behavior;
- HD wave effect adding shore/break-wave parameters.

For tests/reference fixtures, time must be explicit. Do not wait/sleep until an approximate phase.

The web renderer may implement visually modern approximations, but the source effect identity/parameters should remain available.

## Scene point lights

A scene point light exposes renderer-neutral semantics:

```text
position
radius
RGB
runtime intensity
affected tile spans
```

The legacy OpenGL path uses diffuse RGB scaled by `intensity / 255` and quadratic attenuation coefficient `1 / radius²`.

Static point-light definitions also carry flicker state:

```text
waveform
speed
amplitude
baseIntensity
phaseOffset
```

Runtime phase:

```text
phase = (speed * timeOrTick / 50 + phaseOffset) & 0x7ff
```

Waveforms:

```text
0/default -> 2048
1 sine    -> (sin[phase << 3] >> 4) + 1024
2 ramp    -> phase
3 noise   -> noise[phase] >> 1
4 square  -> (phase >> 10) << 11
5 triangle-> (phase < 1024 ? phase : 2048 - phase) << 1
```

Final intensity:

```text
((amplitude * signal >> 11) + baseIntensity) / 2048.0
```

Map/entity paths gather nearby lights and the primary renderer path supports up to four dynamic point lights. Exact tie/order behavior when more than four overlap remains fixture-gated.

## Scene environment, fog and HDR

Map environment data is assigned at **8x8-tile granularity** within a normal 64x64 map square.

Proven environment inputs include:

```text
global light RGB
light intensity / response values
light direction
sky/fog RGB
raw fog value
environment-map faces
HDR/bloom inputs
skybox state
transition state
```

Known default direction:

```text
(-50, -60, -50)
```

Known default response values include:

```text
1.1523438
0.69921875
1.2
```

Global renderer brightness includes:

```text
(0.7 + brightnessPreference * 0.1 + worldAdjustment)
    * environmentLightIntensity
```

### Fog

The decoded environment fog input contributes the scene parameter:

```text
(fogValue + 256) << 2
```

The observed backend is **linear-fog oriented**. Do not rename this cache value to exponential `density` merely to fit a modern engine API.

### HDR inputs

Bloom/bright-pass/white-point-style bytes convert as:

```text
rawByte * 8 / 255
```

Environment transitions interpolate light RGB/response values, HDR floats, fog/sky RGB, fog value, environment sampler and skybox over an explicit duration.

## What Hagalaz should precompute vs preserve

For the first static viewer, it is reasonable for the cache/service layer to precompute some legacy appearance outputs, especially when this avoids porting proprietary/legacy math into Angular.

Good initial server-side candidates:

- underlay smoothing;
- shaped floor ownership/material grouping;
- packed-HSL floor preparation;
- terrain normals;
- object-shadow filtered input;
- model render-corner UVs;
- static model normal/colour inputs;
- selected object model IDs and definition transforms;
- placement-specific contoured geometry when requested as a placement projection.

But also preserve the semantic source values needed by parity/tests so these calculations are not irreversible black boxes.

## What should remain browser/renderer concerns

Generic engine responsibilities should stay in Three.js/WebGL rather than being copied from the Java client:

- GPU buffer ownership;
- draw-call submission;
- frustum culling implementation;
- texture object lifecycle;
- shader compilation;
- camera matrices;
- generic blending/depth API calls;
- picking implementation.

RuneScape-specific **inputs and ordering rules** remain constrained by the semantic contracts.

## Current unresolved boundaries

Keep these explicitly open instead of inventing clean Hagalaz names:

- visible semantics/names for model alpha sentinels raw 254/255;
- exact priority-sensitive ordering for selected real models;
- adjacent-overlay priority/tie behavior and complete `hideUnderlay` combinations;
- one unresolved object-definition model adjustment before scale;
- mode-3 contour pitch/roll direction names;
- material effect category 3;
- some material ground/boolean state;
- special material depth-write/depth-test/order cases;
- exact selection/tie order for >4 overlapping point lights;
- final physical names of two directional-light response coefficients;
- exact backend/view-distance mapping from environment fog value to linear fog start/end;
- several skybox raw parameters.

The GameClient #161/#163/#164/#165 fixture chain exists specifically to resolve or bound these questions.

## Documentation authority

Use these documents by concern:

- geometry/scene baseline: [Game client renderer](game-client-renderer.md);
- production Hagalaz DTO vocabulary: [Renderer-neutral contracts](renderer-neutral-contracts.md);
- GameClient reference import/provenance: [GameClient parity fixtures](game-client-parity-fixtures.md);
- source locators/revision name evidence: [Rendering deobfuscation map](client-rendering-deobfuscation.md) and [External revision-742 references](external-742-reference.md);
- detailed current GameClient behavior: `frankvdb7/Hagalaz.GameClient/docs/rendering/`.

When an older baseline statement conflicts with a later evidence-backed appearance finding in this document/GameClient research, use the newer evidence and update the relevant canonical contract rather than preserving the old wording for compatibility.
# RuneScape game-client 3D renderer reference

This document records the rendering behavior verified in `frankvdb7/Hagalaz.GameClient` and useful when implementing Hagalaz's web scene viewer.

Snapshot: `Hagalaz.GameClient/main` at `6eac3762cc46cec484131369691b5221fd1277bf`.

The canonical human-readable names used here are defined in [Rendering deobfuscation map](client-rendering-deobfuscation.md). Generated decompiler identifiers are intentionally excluded from the architecture narrative. Use the deobfuscation map only when a current source-file/method locator is needed.

## High-level pipeline

The client separates cache/scene semantics from the concrete graphics backend:

```text
region packet / visible region set
        |
        v
RegionManager
  - obtains terrain/object archives and XTEA keys
  - assembles normal or dynamic regions
  - owns collision data and load state
        |
        +----------------------------+
        |                            |
        v                            v
Map : TerrainBuilder            ObjectDefinitionManager
  - decode floor tiles            - decode object render recipes
  - compute heights               - choose models by object shape
  - overlay/underlay              - recolor/retexture/transform
  - dynamic chunk rotate                   |
        |                                   v
        |                             ModelDefinition
        |                             vertex/triangle/texture data
        |                                   |
        v                                   v
GraphicsToolkit.createTerrainSurface  GraphicsToolkit.createModel
        |                                   |
        +-----------------+-----------------+
                          v
                      SceneGraph
              SceneTile / SceneEntity
                          |
                          v
                   GraphicsToolkit
             OpenGL / Direct3D / software
```

The architectural lesson is **not** to port the client's large graphics API. The useful pattern is that RuneScape cache semantics and scene assembly are independent from the selected renderer.

## Stable concept map

| Stable concept | Verified responsibility |
| --- | --- |
| `RegionManager` | Resolves visible region IDs and XTEA keys, loads region bytes, handles normal and dynamic region assembly, creates `Map`, owns collision data and triggers terrain/object construction. |
| `Map` | Extends `TerrainBuilder`; adds object definitions, atmosphere, lights, camera adjustments, object placement/removal and region-part landscape handling. |
| `TerrainBuilder` | Decodes tile heights, overlays, underlays, tile shapes/rotations and terrain flags, transforms dynamic chunks and builds renderer terrain surfaces. |
| `TerrainData` | Stores terrain flags and resolves bridge/effective-plane behavior. It is not the visible terrain mesh or height map. |
| `TerrainSurface` | Renderer-backed terrain surface with the height grid, exact height lookup and bilinear height interpolation. |
| `SceneGraph` | Owns scene tiles, terrain surfaces, scene entities, map lights, visibility state and occlusion. |
| `SceneTile` | One per-plane tile cell holding walls, wall decorations, a floor decoration and links to standard/multi-tile scene objects. |
| `OcclusionManager` | Owns occluders, per-tile visibility results and occlusion tests. |
| `Occluder` | One oriented occlusion volume/polygon represented by plane/type and projected corner data. |
| `OcclusionRasterizer` | Rasterizes/tests projected occlusion geometry for the occlusion manager. |
| `FloorOverlayDefinition` | Floor-overlay material input with primary/secondary color, texture, underlay visibility and additional material parameters. |
| `FloorUnderlayDefinition` | Floor-underlay material input with RGB-derived HSL data, texture, texture scale and rendering flags. |
| `ObjectDefinition` | Object render recipe: model selection by shape plus recolor/retexture, scale, offsets, contouring, transform and animation-related state. |
| `ModelDefinition` | Decoded mesh source containing vertices, triangle indices, face colors/alpha/priorities, textures and optional skin/effect data. |
| `Model` | Backend renderable created from `ModelDefinition`. |
| `SceneEntity` | Abstract entity participating in scene visibility, bounds, nearby-light lookup and rendering. |
| `WallEntity` | One or two wall pieces attached to a scene tile. |
| `WallDecorationEntity` | Wall-attached decoration created for wall-decoration shape types. |
| `FloorDecorationEntity` | Ground/floor decoration occupying a scene tile. |
| `MultiTileSceneEntity` | Scene entity spanning an explicit rectangular tile footprint, used for standard/game objects. |
| `SceneLight` | Position-bearing scene light used by map flicker effects, terrain and scene entities. |
| `GraphicsToolkit` | Renderer abstraction that creates terrain surfaces/models and performs backend drawing/resource operations. |

## Region and chunk structure

A normal cache region is **64x64 tiles with four planes**. `RegionManager` maintains the surrounding region IDs and separate byte arrays for the region data it needs. Hagalaz's `MapProvider` independently confirms the cache naming convention:

- terrain archive name: `m{regionX}_{regionY}`;
- object/location archive name: `l{regionX}_{regionY}`;
- location archives may require four XTEA keys for the region.

Dynamic/constructed maps use **8x8 region parts**. `RegionManager` selects a source region, plane and chunk. `Map`/`TerrainBuilder` rotate source tile coordinates into the destination chunk. Rotation is not a presentation transform: terrain semantics and object placements must use the same source-to-destination rotation rules.

## Coordinate units

The scene uses **512 legacy scene units per tile**.

Verified evidence includes:

- terrain surfaces are created with tile scale `512`;
- map-light positions convert tile coordinates with `<< 9`;
- terrain shape geometry uses the same 512-unit tile-local coordinate space.

Region-local tile coordinates therefore map naturally to legacy scene X/Z by multiplying by 512. For a web renderer, keep a region-local scene origin so absolute RuneScape world coordinates do not turn into unnecessarily large floating-point render coordinates.

### Height sign and plane spacing

The client height convention frequently becomes more negative as visible terrain rises. Explicit plane-0 heights decode to negative values. If an upper plane ends with an implicit-height opcode, it defaults to the lower plane minus `960` legacy height units.

For a conventional Y-up browser engine, use one conversion owner such as:

```text
webX = legacyLocalX
webY = -legacyHeight
webZ = legacyLocalY
```

Do not scatter sign changes through terrain, model, object and camera code. Transport/domain data should preserve client/cache semantics and the scene assembler should own the conversion.

Keeping 512 units per tile in the first implementation also lets decoded object-model vertex units align naturally with terrain units.

## Terrain decoding

`TerrainBuilder.readLandscapeData(...)` is the authoritative floor-tile decoder for this client revision. The current decompiled source locator is recorded in the deobfuscation map, but the semantic operation is stable enough to use directly in design documents.

For every tile it consumes opcodes until a terminator is reached:

| Opcode | Meaning | Client behavior |
| --- | --- | --- |
| `0` | implicit/default height | Plane 0 receives deterministic generated terrain height; higher planes use previous plane minus `960`. |
| `1` | explicit height follows | Reads one byte. Encoded value `1` is normalized to `0`. Plane 0 derives absolute legacy height; upper planes are relative to the plane below. |
| `2..49` | overlay + tile shape/rotation | Reads overlay ID. Shape is `(opcode - 2) / 4`; rotation is `(opcode - 2 + partRotation) & 3`. |
| `50..81` | terrain flag | Stores `opcode - 49` in `TerrainData`. |
| `82+` | underlay ID | Stores `opcode - 81` as the floor underlay. |

A renderable terrain tile therefore needs more than Hagalaz's current terrain-flag byte. At minimum preserve:

- corner/vertex heights;
- overlay ID;
- underlay ID;
- overlay shape;
- overlay rotation;
- terrain flags;
- source height encoding when round-trip writing matters.

The exact plane-0 implicit-height algorithm is documented separately in [Implicit terrain height generation](terrain-height-generation.md).

## Terrain surface construction

After decoding, `TerrainBuilder.buildTerrainSurfaces(...)` creates/fills one `TerrainSurface` per active plane.

The build phase:

1. resolves floor underlay/overlay definitions;
2. computes smoothed underlay color inputs;
3. applies overlay shape and rotation;
4. combines the four tile-corner heights with material/shape data;
5. writes geometry/material data into the renderer-created `TerrainSurface`;
6. attaches those surfaces to `SceneGraph`.

Decoded `underlayIds`, `overlayIds`, overlay shapes and rotations are build input. The renderer surface is the scene representation after those semantic inputs have been resolved.

### Height queries

`TerrainSurface.getInterpolatedHeight(x, y)` performs bilinear interpolation over the four surrounding height-grid values. Ground contouring, camera/interaction placement and web picking/ground placement should use equivalent interpolation when a position lies inside a tile rather than snapping to one corner.

`TerrainSurface.getTileHeight(tileX, tileY)` performs direct height-grid lookup.

## Floor materials

### FloorOverlayDefinition

Verified fields include:

- primary RGB color;
- optional texture ID;
- whether the overlay hides the underlay;
- secondary RGB color;
- several additional ordering/scale/material flags used by terrain construction.

The secondary fields are not all semantically named yet. Hagalaz should expose only fields that are understood and required by a rendering milestone rather than copying opaque integers into a public API.

### FloorUnderlayDefinition

This definition is now substantially deobfuscated. Its RGB conversion matches RuneScape's familiar underlay HSL representation:

- `rgbColor`;
- `hue`;
- `saturation`;
- `lightness`;
- `hueMultiplier`;
- `texture`;
- `textureScale`;
- two additional rendering/occlusion-related flags whose exact final names are not yet frozen.

Its manager is a straightforward cache-backed `FloorUnderlayDefinitionManager`.

## Scene graph

`SceneGraph` owns the runtime spatial/render scene. The important structure is:

```text
SceneGraph
  +-- SceneTile[plane][x][y]
  |     +-- wallA / wallB
  |     +-- wallDecorationA / wallDecorationB
  |     +-- floorDecoration
  |     +-- linked multi-tile scene objects
  |     +-- optional other single-tile entity
  |
  +-- TerrainSurface[plane]
  +-- SceneLight[] / map flicker effects
  +-- OcclusionManager
```

`Map` determines the object scene layer/shape and delegates insertion/removal to the scene graph. Collision updates happen alongside those scene changes but are a separate concern.

The browser should reproduce the *conceptual* separation only. It does not need the client's exact tile-linked-list implementation unless measurement later shows that it solves a real browser bottleneck.

## Scene object categories

Object placement call sites remove much of the ambiguity in the generated entity hierarchy.

### Walls

Wall and corner shape types create `WallEntity` instances. A tile may contain two wall entities for compound/corner wall shapes.

### Wall decorations

All `ShapeType.wallDecoration*` paths create `WallDecorationEntity` instances and add them to the tile's two wall-decoration slots.

### Floor decorations

The floor-decoration layer creates `FloorDecorationEntity` and stores it in the tile's floor-decoration slot. Collision handling also identifies this as the decoration layer.

### Standard / multi-tile objects

Standard objects have explicit start/end tile bounds and are represented by `MultiTileSceneEntity`. Scene tiles reference these through pooled `SceneObjectLink` nodes so an object spanning multiple tiles need not be represented as unrelated duplicates.

Concrete `Sub1`/`Sub2` implementations inside each category appear to represent different model/animation lifecycles, but they should not be prematurely called `Static`/`Dynamic` until those constructors and update paths are fully compared.

## Object placement and model construction

Map object placement and object render definition are separate data.

A placement identifies:

- object ID;
- source/effective plane;
- local X/Y;
- shape type;
- rotation.

`ObjectDefinition` supplies the render recipe. Its model-selection opcode decodes parallel shape/node-type and model-ID arrays, allowing each object shape to select one or more models.

The client then:

1. selects the model set for the placement shape;
2. verifies model availability;
3. decodes `ModelDefinition` data;
4. combines multiple definitions when required;
5. asks `GraphicsToolkit` to create a renderer `Model`;
6. applies definition/placement transforms.

Relevant render transforms include:

- inversion/mirroring;
- recoloring;
- retexturing;
- per-axis scale (default 128);
- X/Y/Z offsets;
- orientation based on placement rotation and shape;
- ground contouring;
- animation IDs;
- varbit/config-driven transformations to another object definition;
- shadow/occlusion flags.

A web viewer therefore cannot render objects correctly from only `{ id, shape, rotation, x, y, plane }` plus gameplay `IObjectType` fields.

## Model definition data

`ModelDefinition` exposes the mesh data needed by a future web model projection:

- `vertexX`, `vertexY`, `vertexZ`, `vertexCount`;
- three triangle vertex-index arrays;
- per-face colors;
- face alpha and priority;
- texture IDs and texture mapping pointers;
- texture-triangle mapping indices/render types;
- optional vertex/triangle skin groups and effect metadata.

The current names `triangleViewSpaceX/Y/Z` are misleading: these arrays are **triangle vertex indices**, not transformed view-space coordinates. The canonical aliases are:

```text
triangleVertexA
triangleVertexB
triangleVertexC
```

The client supports an older model binary layout and a newer `0xFF 0xFF`-terminated layout. In the deobfuscation map their decoder methods are named conceptually `decodeLegacyFormat` and `decodeModernFormat`.

For the first static-object web milestone, project only geometry/material data actually required for static fidelity. Skinning/animation metadata can wait until animated scene entities are in scope.

## Occlusion

`OcclusionManager` is a dedicated subsystem rather than an incidental scene flag.

It owns:

- collections of oriented `Occluder` objects;
- a per-plane/per-tile visibility cache;
- visibility tests against terrain heights and scene bounds;
- an `OcclusionRasterizer` on the relevant hardware path.

Wall/object placement can create or remove occluders based on shape, rotation and object-definition occlusion flags.

This is deliberately **not** part of the first terrain-only web milestone. Correct geometry should come first. Once roof/visibility fidelity becomes a milestone, the client occlusion system provides a useful reference without requiring a direct port.

## Scene lights

Map flickering effects own/use `SceneLight` objects. A scene light has a mutable 3D position plus several light parameters. Scene entities can gather up to four nearby map lights from the scene graph.

The exact names of the remaining integer/scalar light parameters are not frozen yet. The class-level concept and position operations are sufficiently understood; color/radius/intensity names should be based on renderer call sites rather than guessed.

## Renderer abstraction and backends

`GraphicsToolkit` demonstrates that cache/scene representation is not tied to OpenGL or Direct3D.

Two particularly relevant conceptual operations are:

- `createTerrainSurface(...)`: create the renderer terrain surface from dimensions, height data, tile scale and render flags;
- `createModel(ModelDefinition, ...)`: create a renderer model from decoded mesh data.

The concrete client has OpenGL, Direct3D and software implementations. Their generated subclass names are not useful architecture and are intentionally omitted here; the deobfuscation map can be used to locate them when backend-specific behavior must be traced.

The web abstraction should be far smaller than `GraphicsToolkit`, for example:

```text
SceneRenderer
  initialize(canvas)
  setScene(scene)
  resize(width, height, devicePixelRatio)
  render(camera)
  dispose()
```

Terrain/model builders should produce renderer-neutral scene data or engine-native geometry behind this adapter, not depend on Angular components.

## Camera and visibility notes

Camera behavior remains spread across partially deobfuscated code. One verified convention is that camera yaw-like angles are 14-bit values (`0..16383`) with 16,384-entry sine/cosine lookup tables.

The scene separately handles occlusion and roof/plane visibility.

These details are deliberately not required for the first web milestone. A diagnostic free-orbit perspective camera is a better validation tool for correctly assembled terrain. Gameplay camera constraints, roof hiding and client occlusion belong in later fidelity work.

## What is verified enough to build against

- 64x64 region and four-plane structure;
- 8x8 dynamic chunk rotation model;
- 512 scene units per tile;
- exact terrain height/overlay/flag/underlay opcode categories;
- exact overlay shape and rotation formula;
- exact implicit upper-plane spacing of 960 legacy units;
- exact deterministic plane-0 implicit-height algorithm;
- separate terrain flags versus visible height/material data;
- floor underlay HSL/texture/scale representation;
- scene graph/tile responsibilities;
- wall, wall-decoration, floor-decoration and multi-tile object categories;
- object shape-to-model selection;
- object render transforms and recolor/retexture requirements;
- model vertex/triangle/texture data shape;
- occlusion as a dedicated subsystem;
- renderer-neutral scene assembly feeding multiple graphics backends.

## Remaining deobfuscation / fidelity work

- exact semantics of every secondary floor-overlay material field;
- final names for two floor-underlay boolean flags;
- precise identity of the remaining single-tile scene entity base;
- static/dynamic semantics of concrete scene-entity subclasses;
- exact purpose of the scene-entity list/entry helpers in rendering versus picking;
- occluder orientation constants and the remaining scene-tile occlusion shorts;
- renderer-specific `TerrainSurface` operations other than height lookup/interpolation;
- scene-light color/radius/intensity fields;
- complete roof-hiding rules;
- atmosphere/HDR/skybox/light blending details;
- exact gameplay camera projection/constraints;
- animated object/model skinning;
- water/special-material effects and shader-specific behavior.

These gaps should be deobfuscated when their corresponding rendering milestone needs them. They should not force generated class names into new Hagalaz architecture.

## Source location

For current decompiled filenames/method identifiers and confidence/evidence for each semantic name, use [Rendering deobfuscation map](client-rendering-deobfuscation.md). That is intentionally the only 3D-rendering document that treats generated identifiers as first-class source locators.

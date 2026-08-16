# RuneScape game-client 3D renderer reference

This document records the rendering behavior that has been verified in `frankvdb7/Hagalaz.GameClient` and is useful when implementing Hagalaz's web scene viewer. It intentionally translates partially deobfuscated classes into stable concepts instead of treating temporary `Class###` names as architecture.

Snapshot: `Hagalaz.GameClient/main` at `6eac3762cc46cec484131369691b5221fd1277bf`.

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
        +----------------------+
        |                      |
        v                      v
Map / Class274            ObjectDefinitionManager
  - decode floor tiles      - decode object render recipes
  - compute heights         - choose models by object shape
  - overlay/underlay        - recolor/retexture/transform
  - dynamic chunk rotate             |
        |                             v
        |                       ModelDefinition
        |                       vertex/triangle/texture data
        |                             |
        v                             v
GraphicsToolkit.cn(...)        GraphicsToolkit.cb(...)
  -> terrain surface              -> render Model
        |                             |
        +-------------+---------------+
                      v
                  Class356
             tile scene / entities
                      |
                      v
               GraphicsToolkit
          OpenGL / Direct3D / software
```

The important architectural lesson is not to port `GraphicsToolkit`. The useful pattern is to keep RuneScape scene assembly independent from the selected renderer.

## Stable concept map

| Client class | Useful interpretation | Verified responsibility |
| --- | --- | --- |
| `RegionManager` | Region/scene loader | Resolves visible region IDs and XTEA keys, loads region bytes, handles normal and dynamic region assembly, creates `Map`, owns `CollisionData[]`, and triggers terrain/object construction. |
| `Map` | Map scene builder | Extends `Class274`, adds object definitions, atmosphere, lights, camera adjustments, object placement/removal, and region-part landscape handling. |
| `Class274` | Terrain/floor decoder and builder | Decodes tile heights, overlays, underlays, tile shapes/rotations and terrain flags, then builds backend terrain surfaces. |
| `TerrainData` | Tile flags / effective-plane metadata | Stores decoded terrain flags and resolves bridge/plane behavior. It is not the visible terrain mesh or height map. |
| `Class_xa` | Terrain surface | Owns a height grid, supports height interpolation, and exposes terrain drawing/build operations implemented per backend. |
| `Class356` | Scene/tile graph | Owns tile cells, terrain surfaces, scene entities, occlusion-related state and map lights. |
| `ObjectDefinition` | Object render recipe | Selects model IDs by shape and applies render-time scale, offsets, recolor/retexture, contouring, transforms, animation-related state and visibility properties. |
| `ModelDefinition` | Decoded mesh source | Contains vertex positions, triangle indices, colors, alpha, texture references/mapping and skin/animation grouping data. |
| `Model` | Backend render model | Backend-specific renderable produced from a `ModelDefinition`. |
| `GraphicsToolkit` | Renderer abstraction | Creates terrain surfaces and models, manages transforms/render targets/material-related resources, and exposes drawing operations. |
| `GraphicsToolkit_Sub1` | OpenGL backend | Uses `jaggl.OpenGL` and native buffers. |
| `GraphicsToolkit_Sub2_Sub1` | OpenGL backend | OpenGL implementation of the shared `GraphicsToolkit_Sub2` hardware path. |
| `GraphicsToolkit_Sub2_Sub2` | Direct3D backend | Uses `jagdx` Direct3D device/surface/capability APIs. |
| `GraphicsToolkit_Sub3` | Software renderer | CPU/software rasterization path with its own buffers and matrices. |
| `OverlayType` | Floor-overlay material definition | Includes primary/secondary RGB, texture ID, underlay visibility and additional material/blending properties. |
| `Class491` | Floor-underlay definition | Decodes RGB-derived HSL data and other underlay rendering properties. The class still needs a human-readable deobfuscation name. |

## Region and chunk structure

A normal cache region is 64×64 tiles with four planes. `RegionManager` maintains the surrounding region IDs and separate byte arrays for the region data it needs. The server-side Hagalaz `MapProvider` independently confirms the cache convention:

- terrain archive name: `m{regionX}_{regionY}`;
- object archive name: `l{regionX}_{regionY}`;
- object archives may require the four XTEA keys supplied for the region.

Dynamic/constructed maps use 8×8 region parts. `RegionManager` selects a source region/plane/chunk and `Map.readRegionPartLandscape(...)` / `Class274` rotate the source coordinates into their destination chunk. The rotation is not a presentation-only transform: the decoded tile data and object placement must use the same rotation semantics.

### Coordinate units

The scene uses **512 legacy scene units per tile**. This is visible in several places:

- terrain surfaces are created with a tile scale of `512` through `GraphicsToolkit.cn(...)`;
- map light positions convert tile coordinates with `<< 9`;
- terrain code uses 512-sized tile-local geometry coordinates.

Region-local tile coordinates therefore map naturally to scene X/Z positions by multiplying by 512. World locations need a local scene origin before this multiplication to avoid enormous render coordinates.

### Height sign and plane spacing

The legacy client stores visible terrain heights with a convention that frequently becomes more negative as terrain rises. Explicit plane-0 height opcodes are converted to negative values, while an upper plane with no explicit height defaults to the lower plane minus `960` scene units.

For a conventional Y-up browser engine, use one conversion boundary such as:

```text
webX = legacyLocalX
webY = -legacyHeight
webZ = legacyLocalY
```

Do not scatter sign changes through terrain, model, object and camera code. The transport DTO should preserve legacy semantics; the scene assembler should own the conversion.

Keeping 512 units per tile for the first vertical slice has an important advantage: decoded object-model vertex units align with terrain units without another scale factor. Use a region-local origin (and later origin rebasing if needed) rather than normalizing tiles to 1.0 in multiple places.

## Terrain decoding

`Class274.readLandscapeData(...)` is the authoritative verified floor-tile decoder for this client revision.

For every tile it consumes opcodes until a terminator is reached:

| Opcode | Meaning | Client behavior |
| --- | --- | --- |
| `0` | implicit/default height | Plane 0 receives a generated/default height; higher planes use the previous plane minus `960`. |
| `1` | explicit height follows | Reads one byte. Value `1` is treated as `0`. Plane 0 derives its height from that byte; higher planes are relative to the plane below. |
| `2..49` | overlay + tile shape/rotation | Reads overlay ID. Shape/path is `(opcode - 2) / 4`; rotation is `(opcode - 2 + partRotation) & 3`. |
| `50..81` | terrain flag | Stores `opcode - 49` in `TerrainData.decodedTerrainData`. |
| `82+` | underlay ID | Stores `opcode - 81` as the floor underlay. |

The tile therefore needs more than the current Hagalaz `sbyte TerrainData` flag value. At minimum, visible terrain reconstruction requires:

- vertex/corner heights;
- overlay ID;
- underlay ID;
- overlay shape/path;
- overlay rotation;
- terrain flags.

### Terrain build

After decoding, `Class274.method2692(...)` builds one terrain surface per plane. It calls `GraphicsToolkit.cn(...)` with the height grid and a tile scale of 512, then attaches the returned `Class_xa` to `Class356`.

The build phase also resolves `OverlayType` and underlay definitions to colors/textures and creates the shaped tile geometry. The temporary arrays `underlayIds`, `overlayIds`, `overlayPaths`, and `overlayRotations` are released after the surfaces are built. This is an important lifecycle distinction: decoded cache tiles are build input; renderable terrain surfaces are the scene representation.

### Terrain height queries

`Class_xa.method6416(x, y, ...)` performs bilinear interpolation between four neighboring height-grid values. Object contouring, camera/interaction placement, and any web picking/ground placement should use equivalent interpolation instead of nearest-tile height when a position can lie inside a tile.

## Floor materials

### Overlay

`OverlayType` decodes, among other fields:

- primary RGB color;
- optional texture ID;
- `hideUnderlay`;
- secondary RGB color;
- additional material/order/scale-like flags used by the terrain builder.

### Underlay

`Class491` decodes a base RGB value and converts it into HSL-derived values used by the floor renderer. It also contains optional IDs/flags used during terrain construction.

The exact semantic names of every secondary material field remain partially obfuscated. The web foundation should initially expose only fields proven necessary by the first representative-region tests, then extend the rendering projection as fidelity tests reveal a need. Do not copy unexplained `anInt####` fields into a public API.

## Object placement and model construction

Map object placement and object render definition are separate pieces of data.

A placement identifies:

- object ID;
- local X/Y/plane;
- shape type;
- rotation.

`ObjectDefinition` supplies the render recipe for that ID. Opcode `1` decodes parallel `nodeTypes` and `modelIDs` arrays: each supported object shape can select one or more model IDs. The client checks availability for the requested shape, decodes those model files, combines multiple `ModelDefinition` instances when needed, and calls `GraphicsToolkit.cb(...)` to create the backend `Model`.

The definition can then alter that model through properties including:

- inversion/mirroring;
- recoloring;
- retexturing;
- per-axis scale (default 128);
- X/Y/Z offsets;
- orientation according to placement rotation and shape;
- ground contouring modes;
- animation IDs;
- varbit/config-driven transformations to another object definition;
- shadow/occlusion-related flags.

That means a web viewer cannot render a placed object correctly from `{ id, shape, rotation, x, y, z }` plus gameplay `IObjectType` fields alone.

## Model definition data

`ModelDefinition` already exposes the core mesh information that a web renderer eventually needs to decode server-side:

- `vertexX`, `vertexY`, `vertexZ`, `vertexCount`;
- three triangle-index arrays (`triangleViewSpaceX/Y/Z`) and `triangleCount`;
- per-face colors;
- alpha and priority data;
- texture IDs and texture pointers;
- texture-triangle mapping indices and render types;
- vertex/triangle skin groups and particle/effect-related metadata.

For the first web static-object milestone, the rendering API should project only static mesh data needed for geometry/material fidelity. Skinning/animation metadata can be deferred until animated scene entities are in scope.

## Scene graph

`Class356` is the client-side scene owner. It contains:

- a 3D array of tile cells (`Class340[][][]`);
- terrain surfaces (`Class_xa[]`) for the active scene mode;
- scene entity collections/lists;
- map flickering lights;
- occlusion/visibility-related arrays and an occlusion helper (`Class358`);
- methods for adding/removing wall objects, wall decorations, standard objects and ground decorations.

`Map` resolves an object's layer/shape and delegates insertion/removal to this scene graph. Collision changes happen alongside those scene changes but remain a different concern.

For the browser this should become a much smaller conceptual structure: loaded render regions/planes with terrain batches plus object instances. Do not reproduce the client's tile-linked-list implementation unless a measured browser bottleneck demonstrates a need.

## Renderer abstraction and backends

`GraphicsToolkit` demonstrates that the cache/scene representation is not tied to OpenGL or Direct3D. Two particularly relevant factory methods are:

- `cn(...)`: create a terrain surface from dimensions, height data, tile scale, and rendering flags;
- `cb(ModelDefinition, ...)`: create a backend-specific model from decoded model data.

The concrete client supports OpenGL, Direct3D and software rendering. This validates the separation between scene semantics and renderer, but the browser abstraction should be intentionally tiny. A useful boundary is closer to:

```text
SceneRenderer
  initialize(canvas)
  setScene(scene)
  resize(width, height, devicePixelRatio)
  render(camera)
  dispose()
```

Terrain/model builders should produce renderer-neutral scene data or engine-native geometry behind that one adapter. They should not depend on Angular components.

## Camera and visibility notes

Camera code remains spread across partially deobfuscated client classes. One verified convention is that yaw-like values such as `Client.cameraMoveY` are masked with `0x3fff` and use 14-bit sine/cosine lookup tables. The scene also has dedicated occlusion state and separate roof/plane decisions.

These details are deliberately **not** required for the first web milestone. A free-orbit perspective camera that can inspect the correctly assembled region is a better validation tool. Matching RuneScape gameplay camera constraints, roof hiding and client occlusion should be separate fidelity work after the geometry pipeline is proven.

## What is verified versus still open

### Verified enough to build against

- 64×64 region and four-plane structure.
- 8×8 dynamic chunk rotation model.
- 512 scene units per tile.
- tile height/overlay/flag/underlay opcode categories.
- overlay shape and rotation formula.
- upper-plane default spacing of 960 legacy height units.
- separate terrain flags versus visible height/material data.
- object shape-to-model selection.
- object render transforms and recolor/retexture requirements.
- model vertex/triangle/texture data shape.
- renderer-neutral scene assembly feeding multiple graphics backends.

### Follow-up deobfuscation / fidelity work

- Human-readable name and every secondary field of `Class491`.
- Exact semantics of all secondary `OverlayType` material fields.
- Texture cache format and client texture sampling/material rules.
- Complete roof hiding and occlusion rules (`Class356`/`Class358`).
- Atmosphere/HDR/skybox/light blending details.
- Exact gameplay camera projection/constraints.
- Animated object/model skinning path.
- Water/special-material effects and any shader-specific behavior.

These should be investigated only when the corresponding web milestone needs them.

## Reference files

Primary client files used for this document:

- `src/main/java/RegionManager.java`
- `src/main/java/Map.java`
- `src/main/java/Class274.java`
- `src/main/java/TerrainData.java`
- `src/main/java/Class_xa.java`
- `src/main/java/Class356.java`
- `src/main/java/ObjectDefinition.java`
- `src/main/java/ModelDefinition.java`
- `src/main/java/GraphicsToolkit.java`
- `src/main/java/GraphicsToolkit_Sub1.java`
- `src/main/java/GraphicsToolkit_Sub2_Sub1.java`
- `src/main/java/GraphicsToolkit_Sub2_Sub2.java`
- `src/main/java/GraphicsToolkit_Sub3.java`
- `src/main/java/OverlayType.java`
- `src/main/java/Class491.java`

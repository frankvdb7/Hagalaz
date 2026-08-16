# Game-client rendering deobfuscation map

This document defines the **canonical human-readable names** used by Hagalaz documentation for the RuneScape client rendering subsystem.

The source snapshot is `frankvdb7/Hagalaz.GameClient@6eac3762cc46cec484131369691b5221fd1277bf`.

## Naming policy

The decompiled client still contains many generated identifiers such as `Class274`, `Class356`, `Class_xa` and `method6416`. Those names are source-location artifacts, not architecture.

For Hagalaz 3D work:

1. **Use the semantic name in architecture, specifications, DTO discussions, tests and implementation notes.**
2. **Keep the obfuscated identifier only in this source-locator document** until the GameClient source itself has been mechanically renamed.
3. **Rename only when responsibility is supported by call sites and data flow.** Do not guess a precise name from one method or one opcode.
4. **Separate class confidence from member confidence.** A class can have a clear role even when some of its fields remain unresolved.
5. **Do not rename a miscellaneous class solely because one static helper in it has been understood.** Several Jagex classes contain unrelated static utility methods because of obfuscation. In those cases the method receives a semantic alias while the containing class remains only a source locator.
6. **Prefer domain names over renderer-specific names.** `TerrainSurface`, `SceneGraph`, `Occluder` and `SceneLight` describe the RuneScape scene independently of OpenGL, Direct3D or Three.js.

Confidence levels:

- **Verified**: the class/member role is demonstrated directly by construction, fields and call sites.
- **High**: behavior is strongly constrained by multiple call sites; the chosen name is unlikely to change materially.
- **Medium**: the broad role is clear, but a more precise RuneScape/client term may emerge later.
- **Unresolved**: keep the current source identifier in the source repository until more evidence exists; do not expose it in Hagalaz public contracts.

## Core rendering class names

| Canonical name | Current source locator | Confidence | Evidence / responsibility |
| --- | --- | --- | --- |
| `TerrainBuilder` | `Class274` | Verified | Owns tile heights, overlays, underlays, overlay shape/rotation, terrain flags and camera-adjustment grids; decodes normal/dynamic landscape tiles and builds renderer-backed terrain surfaces. `Map` extends it. |
| `SceneGraph` | `Class356` | Verified | Owns per-plane scene tiles, terrain surfaces, scene entities, map lights, visibility state and the occlusion subsystem; inserts/removes walls, decorations and standard objects. |
| `SceneTile` | `Class340` | Verified | One tile/plane cell in the scene graph. Holds wall entities, wall decorations, floor decoration, linked multi-tile objects and a link to the tile below. |
| `TerrainSurface` | `Class_xa` | Verified | Abstract renderer-backed terrain height/surface object. Owns the height grid and tile scale and supports exact/interpolated height queries plus backend terrain operations. |
| `OcclusionManager` | `Class358` | Verified | Owns occluder collections, tile visibility cache and occlusion tests; created by and bound to the scene graph. |
| `Occluder` | `Class385` | High | Stores occluder type/plane and polygon/corner coordinates; created, indexed and queried exclusively by the occlusion subsystem. |
| `OcclusionRasterizer` | `Class347` | High | Rasterizes projected occlusion triangles/scanlines into the occlusion manager's depth/visibility buffer. |
| `FloorUnderlayDefinition` | `Class491` | Verified | Decodes underlay RGB, texture, texture scale and rendering flags and derives HSL/hue-multiplier data used by terrain color/material construction. |
| `FloorUnderlayDefinitionManager` | `Class499` | Verified | Cache-backed manager for floor-underlay definitions with an ID-keyed `NodeCache`; constructs/decodes/caches `FloorUnderlayDefinition`. |
| `FloorOverlayDefinitionManager` | `Class418` | Verified | Cache-backed manager for overlay definitions; loads the overlay config archive and constructs/caches `OverlayType`. |
| `FloorOverlayDefinition` | `OverlayType` | Verified concept | Source class is already partially named. It is the floor-overlay definition containing primary/secondary color, texture, hide-underlay and additional material parameters. |
| `SceneEntity` | `Entity_Sub1` | High | Common abstract scene-renderable base. Owns scene/plane state, participates in visibility/bounds/render operations, and resolves nearby map lights. |
| `MultiTileSceneEntity` | `Entity_Sub1_Sub1` | High | Scene entity with explicit start/end X/Y tile bounds and active state; represents entities spanning a tile rectangle. |
| `SingleTileSceneEntity` | `Entity_Sub1_Sub2` | High | Position-based scene entity whose scene/light/visibility queries resolve from one current tile. Precise gameplay subtype remains intentionally generic. |
| `WallDecorationEntity` | `Entity_Sub1_Sub3` | Verified | Constructed by `Map` for `ShapeType.wallDecoration*` placements and stored in the scene tile's wall-decoration slots. |
| `FloorDecorationEntity` | `Entity_Sub1_Sub4` | Verified | Constructed for the floor-decoration scene layer and stored in the scene tile's floor-decoration slot. |
| `WallEntity` | `Entity_Sub1_Sub5` | Verified | Constructed for wall/corner/unfinished-wall shapes and stored as one or two wall entities per scene tile. |
| `SceneObjectLink` | `Class352` | High | Pooled linked node containing a `MultiTileSceneEntity`; used by scene tiles to reference multi-tile/standard objects without duplicating the object. |
| `SceneLight` | `Node_Sub14` | High | Position-bearing light object used by map flickering effects, terrain and scene entities; exposes X/Y/Z, scalar/int light parameters and position mutation. |
| `SceneEntityList` | `Class338` | Medium | Maintains ordered scene-entity entries, removes duplicates in one mode and releases pooled entries. Exact role in the render/picking pipeline needs more deobfuscation before a source rename. |
| `SceneEntityEntry` | `Class353` | Medium | Pooled wrapper around a `SceneEntity` with bounds/hit-test behavior and ownership by `SceneEntityList`. |

`Map`, `RegionManager`, `ObjectDefinition`, `ObjectDefinitionManager`, `ModelDefinition`, `Model`, `GraphicsToolkit`, `TerrainData`, `Atmosphere`, `AtmosphereManager`, `MapFlickeringEffect` and `ShapeType` are already sufficiently human-readable and remain canonical.

## Core inheritance / ownership model

Use this model in documentation instead of the decompiled names:

```text
RegionManager
    |
    v
Map : TerrainBuilder
    |
    +--> FloorOverlayDefinitionManager --> FloorOverlayDefinition
    |
    +--> FloorUnderlayDefinitionManager --> FloorUnderlayDefinition
    |
    +--> ObjectDefinitionManager --> ObjectDefinition --> ModelDefinition
    |
    v
SceneGraph
    |
    +--> SceneTile[plane][x][y]
    |      +--> WallEntity (0..2)
    |      +--> WallDecorationEntity (0..2)
    |      +--> FloorDecorationEntity (0..1)
    |      +--> SceneObjectLink --> MultiTileSceneEntity
    |      +--> other single-tile scene entity
    |
    +--> TerrainSurface[plane]
    +--> SceneLight[]
    +--> OcclusionManager
               +--> Occluder[]
               +--> OcclusionRasterizer

GraphicsToolkit
    +--> creates TerrainSurface
    +--> creates Model from ModelDefinition
    +--> concrete OpenGL / Direct3D / software backends
```

This is the vocabulary that should be copied into Hagalaz code and API design. Do not introduce `Class###`-inspired names into the server or web renderer.

## TerrainBuilder member names

The following members are sufficiently understood for documentation and future GameClient source renaming.

| Canonical member | Current source member | Confidence | Meaning |
| --- | --- | --- | --- |
| `sceneGraph` | `aClass356_2767` | Verified | Scene graph receiving built terrain and scene objects. |
| `floorOverlayDefinitions` | `aClass418_2765` | Verified | Floor overlay definition manager. |
| `floorUnderlayDefinitions` | `aClass499_2819` | Verified | Floor underlay definition manager. |
| `terrainData` | `aTerrainData_2811` | Verified | Terrain flags / effective-plane metadata. |
| `tileHeights` | `tileHeights` | Verified | Per-plane corner/height grid. |
| `overlayIds` | `overlayIds` | Verified | Per-tile overlay IDs. |
| `underlayIds` | `underlayIds` | Verified | Per-tile underlay IDs. |
| `overlayShapes` | `overlayPaths` | High | Per-tile floor-overlay shape/path value decoded from `(opcode - 2) / 4`. `shape` is clearer for renderer documentation. |
| `overlayRotations` | `overlayRotations` | Verified | Per-tile overlay rotation `0..3`. |
| `cameraAdjustments` | `cameraAdjustments` | Verified field name | Per-plane map-provided adjustment grid; exact camera/roof semantics remain a later fidelity topic. |
| `setShadingDelayed` | `method2684` | High | Enables delayed terrain shading/shadow-related accumulation. |
| `releaseTerrainBuildScratch` | `method2685` | High | Clears terrain-color/material accumulation arrays and ends delayed-build scratch state. |
| `initializeHeightArea` | `method2687` | High | Initializes/fills a rectangular height area and repairs boundaries. |
| `readCollisionData` | `readCollisionData` | Verified | Decodes terrain flags for collision across a normal 64x64 region. |
| `readDynamicChunkCollisionData` | `method2689` | High | Reads/rotates one dynamic 8x8 region part into destination collision/height space. |
| `createTerrainSurfaces` | `readGroundMapData` | High | Allocates renderer terrain surfaces for the active planes through `GraphicsToolkit`. |
| `buildTerrainSurfaces` | `method2692` | Verified responsibility | Resolves floor definitions, colors/material inputs and shaped tiles into the terrain surfaces. |
| `readLandscapeData` | `readLandscapeData` | Verified | Decodes one tile's terrain opcode list including height, overlay, flags and underlay. |

Some large terrain-build scratch arrays are still intentionally unnamed. Their meaning should be derived from the particular material/shape algorithm before they are renamed, rather than receiving names such as `temp1` or `colors2`.

## TerrainSurface member names

| Canonical member | Current source member | Confidence | Meaning |
| --- | --- | --- | --- |
| `heightGrid` | `anIntArrayArray6394` | Verified | Corner/grid heights used by exact and interpolated height queries. |
| `tileCountX` | `anInt6397` | Verified | Terrain width in tiles. |
| `tileCountY` | `anInt6393` | Verified | Terrain height/depth in tiles. |
| `tileSize` | `anInt6395` | Verified | Scene units per tile; normally 512 for map terrain. |
| `tileShift` | `anInt6396` | Verified | `log2(tileSize)` used for tile lookup and interpolation. |
| `getInterpolatedHeight` | `method6416` | Verified | Bilinear interpolation of four height-grid corners for a world/scene position. |
| `getTileHeight` | `method6417` | Verified | Direct grid height lookup. |

The remaining abstract methods cover backend-specific terrain building, drawing, shadow/mask and resource operations. They should be renamed per behavior after comparing their implementations across the renderer backends; a one-backend guess is not sufficient.

## FloorUnderlayDefinition member names

The underlay definition is one of the strongest deobfuscation targets because its decode and color conversion are self-contained.

| Canonical member | Current source member | Confidence | Evidence |
| --- | --- | --- | --- |
| `rgbColor` | `anInt5860` | Verified | Opcode `1` reads a 24-bit RGB value and immediately derives HSL data from it. |
| `hue` | `anInt5855` | Verified | Computed from RGB hue multiplied by the hue multiplier, matching the classic RuneScape underlay-color representation. |
| `saturation` | `anInt5861` | Verified | RGB-to-HSL saturation clamped to `0..255`. |
| `lightness` | `anInt5862` | Verified | RGB-to-HSL lightness clamped to `0..255`. |
| `hueMultiplier` | `anInt5863` | Verified | Saturation/lightness-derived multiplier, minimum `1`, used to scale hue. |
| `texture` | `anInt5856` | High | Opcode `2` reads an ID with `65535 -> -1`; terrain construction passes it in the same material slot as overlay textures. |
| `textureScale` | `anInt5857` | High | Opcode `3` reads `ushort << 2` and terrain construction carries it with the underlay texture. |
| `decode` | `method6072` | Verified | Reads opcodes until `0`. |
| `decodeOpcode` | `method6073` | Verified | Decodes one underlay-definition opcode. |
| `calculateHsl` | `method6074` | Verified | Converts the decoded RGB color into hue/saturation/lightness/hue multiplier. |

Two booleans remain deliberately semantic-but-not-overprecise:

- opcode `4`: default `true`, becomes `false`; participates in shaped-terrain build behavior;
- opcode `5`: default `true`, becomes `false`; participates in whether an upper-plane flat tile may contribute to the client's occlusion/visibility flags.

Until every use is traced, name these in documentation as `renderFlag4` / `allowsOcclusion` only with an explicit confidence note. Do **not** put guessed names into a public Hagalaz API.

## Floor-definition manager names

### FloorUnderlayDefinitionManager

| Canonical member | Current source member | Confidence |
| --- | --- | --- |
| `definitions` | `aNodeCache_5891` | Verified |
| `configContainer` | `configContainer` | Verified |
| `getFloorUnderlayDefinition` | `method6111` | Verified |

The remaining instance methods are cache maintenance operations and can be named once their corresponding `NodeCache` methods are deobfuscated consistently.

### FloorOverlayDefinitionManager

| Canonical member | Current source member | Confidence |
| --- | --- | --- |
| `definitions` | `aNodeCache_4296` | Verified |
| `configContainer` | `configContainer` | Verified |
| `getFloorOverlayDefinition` | `getOverlayType` | Verified |
| `definitionCount` | `anInt4295` | High |

`anInt4294` is modified by overlay opcode `8`; its exact special-overlay/default role is not sufficiently proven yet.

## SceneGraph / SceneTile names

### SceneGraph

The scene graph has enough evidence to rename its structural fields now, while leaving frame-specific visibility scratch fields for later.

| Canonical member | Current source member | Confidence |
| --- | --- | --- |
| `graphicsToolkit` | `aGraphicsToolkit3645` | Verified |
| `occlusionManager` | `aClass358_3649` | Verified |
| `activeTiles` | `aClass340ArrayArrayArray3653` | Verified |
| `primaryTiles` | `aClass340ArrayArrayArray3655` | High |
| `alternateTiles` | `aClass340ArrayArrayArray3657` | High |
| `activeTerrainSurfaces` | `aClass_xaArray3676` | Verified |
| `primaryTerrainSurfaces` | `aClass_xaArray3701` | High |
| `alternateTerrainSurfaces` | `aClass_xaArray3658` | High |
| `mapLights` | `aMapFlickeringEffectArray3679` | Verified |
| `tilesRangeX` | `tilesRangeX` | Verified |
| `tilesRangeY` | `tilesRangeY` | Verified |
| `maxPlanes` | `maxZ` | High |
| `getOrCreateTile` | `method4136` | Verified responsibility |
| `ensureTileStack` | `method4137` | High |
| `addFloorDecoration` | `method4142` | Verified from `Map` layer/shape call sites |
| `addWallDecoration` | `method4144` | Verified from `ShapeType.wallDecoration*` call sites |
| `addWalls` | `method4180` | Verified from wall/corner shape call sites |

The exact names of the two scene modes represented by the primary/alternate tile/surface arrays are not yet proven. `primary`/`alternate` is intentionally less specific than guessing that one is always underwater, roof or bridge state.

### SceneTile

| Canonical member | Current source member | Confidence |
| --- | --- | --- |
| `tileBelow` | `aClass340_3380` | Verified |
| `plane` | `aByte3381` | High |
| `wallA` | `aClass432_Sub1_Sub5_3382` | Verified |
| `wallB` | `aClass432_Sub1_Sub5_3383` | Verified |
| `wallDecorationA` | `aClass432_Sub1_Sub3_3384` | Verified |
| `wallDecorationB` | `aClass432_Sub1_Sub3_3385` | Verified |
| `floorDecoration` | `aClass432_Sub1_Sub4_3386` | Verified |
| `objectLinks` | `aClass352_3388` | High |
| `singleTileEntity` | `aClass432_Sub1_Sub2_3391` | High; precise subtype unresolved |

The remaining shorts are tied to occlusion boundaries/metadata and should be renamed together with the corresponding `OcclusionManager` methods.

## Occlusion names

`OcclusionManager` is not merely a generic visibility helper. It creates oriented occluders, stores them by category, maintains a per-plane tile visibility cache and asks `OcclusionRasterizer` to rasterize/test projected occlusion geometry.

Useful source aliases:

| Canonical member | Current source member | Confidence |
| --- | --- | --- |
| `sceneGraph` | `aClass356_3710` | Verified |
| `rasterizer` | `aClass347_3711` | Verified |
| `tileVisibilityCache` | `anIntArrayArrayArray3713` | High |
| `addOccluder` | `method4216` | Verified responsibility |
| `removeOccluder` | `method4217` | Verified responsibility |
| `invalidate/rebuildOccluders` | `method4221` | High; exact lifecycle name should be verified before source rename |

`Occluder` fields should be renamed as a group after each type (`1`, `2`, `4`, `8`, `16`) is mapped to its axis/orientation. The class name itself is already sufficiently certain.

## Scene entity hierarchy

The scene-object layer can be understood without retaining the generated inheritance names:

```text
SceneEntity
  |
  +-- MultiTileSceneEntity
  |     +-- concrete standard/game-object implementations
  |
  +-- SingleTileSceneEntity
  |     +-- precise subtype still unresolved
  |
  +-- WallDecorationEntity
  |     +-- static/dynamic concrete implementations
  |
  +-- FloorDecorationEntity
  |     +-- static/dynamic concrete implementations
  |
  +-- WallEntity
        +-- static/dynamic concrete implementations
```

Do not rename `Sub1` / `Sub2` concrete implementations to `Static` / `Dynamic` until their animation/model lifecycle has been compared. The base category names above are verified from the `Map` placement call sites; the concrete split needs its own evidence.

## SceneLight names

`SceneLight` currently exposes enough behavior to remove `Node_Sub14` from architecture prose:

| Canonical member | Current source member | Confidence |
| --- | --- | --- |
| `position` | `aVector3f_7608` | Verified |
| `getX` | `method3318` | Verified |
| `getY` | `method3311` | Verified |
| `getZ` | `method3312` | Verified |
| `setPosition` | `method3315` | Verified |
| scalar light parameter | `aFloat7605` / `method3317` | Medium; likely intensity/brightness but not named until usage is fully traced |
| integer light parameter A | `anInt7606` / `method3316` | Unresolved |
| integer light parameter B | `anInt7607` / `method3313` | Unresolved |

A source rename may safely rename the class and position methods now while leaving the three material/light parameters neutral until their renderer uses are traced.

## Terrain-height helper names

Several exact terrain-height helpers live as unrelated static methods on otherwise miscellaneous classes. **Rename the methods, not the containing classes**, unless the rest of each class is independently understood.

| Canonical helper | Current source locator | Confidence | Behavior |
| --- | --- | --- | --- |
| `generateBaseTerrainHeight` | `Class156.calculateHeight` | Verified | Combines three noise octaves, scales the result and clamps to `10..60`. The method already has a partial readable name. |
| `interpolatedNoise` | `TextureLoader.method652` | Verified | Samples four smoothed lattice values and performs 2D cosine interpolation. |
| `smoothedNoise` | `Class170.method2039` | Verified | Weighted corner/cardinal/center smoothing over deterministic raw noise. |
| `rawTerrainNoise` | `AbstractQueue_Sub1.method6486` | Verified | Deterministic integer hash/noise returning `0..255`. |
| `cosineInterpolate` | `Class20.method466` | Verified | Fixed-point interpolation using the client's cosine lookup table. |
| `cosineTable` | `Class257.anIntArray2684` | Verified | 16,384-entry fixed-point cosine lookup. |
| `sineTable` | `Class257.anIntArray2683` | Verified | 16,384-entry fixed-point sine lookup. |
| `angleToRadians` | `Class257.method2541` | Verified | Converts a masked 14-bit client angle to radians. |

The stable algorithm is documented in `terrain-height-generation.md`; that document should use the canonical helper names and keep these generated source locators only in a reference table.

## ModelDefinition member/method names

`ModelDefinition` is already well enough named to keep. Several members still need cleanup.

High-confidence aliases:

| Canonical member | Current source member | Confidence |
| --- | --- | --- |
| `vertexX` | `vertexX` | Verified |
| `vertexY` | `vertexY` | Verified |
| `vertexZ` | `vertexZ` | Verified |
| `triangleVertexA` | `triangleViewSpaceX` | Verified | Despite the current name, this is the first triangle vertex index, not a transformed view-space coordinate. |
| `triangleVertexB` | `triangleViewSpaceY` | Verified |
| `triangleVertexC` | `triangleViewSpaceZ` | Verified |
| `faceColors` | `colors` | Verified |
| `faceAlpha` | `alpha` | Verified |
| `facePriorities` | `priorities` | Verified |
| `faceTextures` | `textures` | Verified |
| `decodeLegacyFormat` | `method1199` | Verified responsibility | Reads the older 18-byte-footer model layout. |
| `decodeModernFormat` | `method1201` | Verified responsibility | Reads the newer `0xFF 0xFF`-terminated layout with the 23-byte base footer and optional sections. |
| `translate` | `method1194` | Verified |
| `rotate` | `method1195` | High; method applies rotations around the three model axes. |
| `scaleByPowerOfTwo` | `method1196` | Verified |
| `recolor` | `method1185` | Verified |
| `retexture` | `method1200` | Verified |

The misleading `triangleViewSpaceX/Y/Z` names should be a priority rename because they can easily produce the wrong web mesh abstraction.

## GraphicsToolkit aliases

Do not port or expose the obfuscated graphics API. Only a handful of operations are needed to understand the scene pipeline:

| Canonical operation | Current source member | Confidence |
| --- | --- | --- |
| `createTerrainSurface` | `GraphicsToolkit.cn(...)` | Verified |
| `createModel` | `GraphicsToolkit.cb(ModelDefinition, ...)` | Verified |

Concrete subclasses should be described in architecture as **OpenGL backend**, **Direct3D backend**, and **software backend**. Their generated subclass names belong only in a source-tracing note, not in Hagalaz APIs or the web-renderer design.

## Names deliberately not frozen yet

The following areas still need focused tracing before source renaming:

- the exact meaning of all secondary `FloorOverlayDefinition` fields/opcodes;
- the two unresolved `FloorUnderlayDefinition` boolean flags beyond their known rendering/occlusion effects;
- the precise identity of `SingleTileSceneEntity`;
- static-versus-dynamic naming for concrete wall/wall-decoration/floor-decoration/object subclasses;
- the exact role/name of `SceneEntityList` and `SceneEntityEntry` in rendering versus picking;
- all occluder orientation/type constants and associated short fields on `SceneTile`;
- the renderer-specific abstract operations on `TerrainSurface` other than height queries;
- light color/radius/intensity parameter names on `SceneLight`;
- secondary atmosphere, roof-hiding and visibility classes.

These are **deobfuscation tasks**, not reasons to leak generated names into new Hagalaz code. New server/web code should name data by its observed semantic purpose and keep unresolved client-only details behind the render projection.

## Recommended source-rename order

A source-level GameClient cleanup should be mechanical and behavior-preserving. Use this order so each step reduces the number of generated names in the next diff:

1. Floor definitions/managers: `FloorUnderlayDefinition`, `FloorUnderlayDefinitionManager`, `FloorOverlayDefinitionManager`.
2. Terrain core: `TerrainSurface`, then `TerrainBuilder`.
3. Scene core: `SceneTile`, `SceneObjectLink`, `SceneGraph`.
4. Occlusion: `Occluder`, `OcclusionRasterizer`, `OcclusionManager`.
5. Scene entity bases: `SceneEntity`, `MultiTileSceneEntity`, `SingleTileSceneEntity`, `WallEntity`, `WallDecorationEntity`, `FloorDecorationEntity`.
6. `SceneLight`.
7. High-confidence method/field names in the classes above.
8. `ModelDefinition` triangle/member cleanup.
9. Only then investigate lower-confidence render queues, concrete entity variants, atmosphere and backend-specific methods.

Each source-rename commit should compile before the next group is renamed. Do not combine semantic behavior changes with the deobfuscation commits.

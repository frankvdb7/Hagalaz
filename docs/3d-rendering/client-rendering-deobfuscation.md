# Game-client rendering deobfuscation map

This document defines the **canonical human-readable names** used by Hagalaz documentation for the RuneScape client rendering subsystem.

Source snapshot: `frankvdb7/Hagalaz.GameClient@6eac3762cc46cec484131369691b5221fd1277bf`.

Source-level mechanical renaming is tracked in `frankvdb7/Hagalaz.GameClient#141`.

## Naming policy

The decompiled client still contains generated identifiers such as `Class274`, `Class356`, `Class_xa`, `Entity_Sub1_Sub5` and `method6416`. These are source-location artifacts, not architecture.

For Hagalaz 3D work:

1. Use semantic names in architecture, specifications, DTO discussions, tests and implementation notes.
2. Keep generated identifiers only in this source-locator document until the GameClient source itself has been renamed.
3. Rename only when responsibility is supported by construction, fields, call sites and data flow.
4. Separate class confidence from member confidence. A class can be understood while several fields remain unresolved.
5. Do not rename a miscellaneous class solely because one unrelated static helper inside it is understood.
6. Prefer domain names independent of renderer backend: `TerrainSurface`, `SceneGraph`, `Occluder`, `SceneLight`, etc.
7. If a concept cannot yet be named accurately, keep it internal/unresolved instead of exporting an obfuscated or guessed name.

Confidence:

- **Verified**: directly demonstrated by construction, data flow and call sites.
- **High**: strongly constrained by multiple independent uses; terminology might be refined but the concept will not materially change.
- **Medium**: broad role is understood but a more precise client/RuneScape term may still emerge.
- **Unresolved**: do not freeze a semantic source/API name yet.

## Canonical rendering vocabulary

| Canonical name | Current GameClient source | Confidence | Responsibility |
| --- | --- | --- | --- |
| `TerrainBuilder` | `Class274` | Verified | Base used by `Map`; decodes heights, overlays, underlays, shape/rotation and flags, handles dynamic chunks and builds terrain surfaces. |
| `SceneGraph` | `Class356` | Verified | Owns scene tiles, terrain surfaces, entities, lights, visibility and occlusion; inserts/removes scene objects. |
| `SceneTile` | `Class340` | Verified | One per-plane tile cell containing wall slots, wall decorations, floor decoration and object links. |
| `TerrainSurface` | `Class_xa` | Verified | Renderer-backed terrain surface/height grid with exact and bilinear height queries. |
| `FloorUnderlayDefinition` | `Class491` | Verified | Underlay RGB/HSL, texture, scale and rendering flags. |
| `FloorUnderlayDefinitionManager` | `Class499` | Verified | Cache-backed underlay-definition lookup/cache. |
| `FloorOverlayDefinitionManager` | `Class418` | Verified | Cache-backed manager for overlay definitions. |
| `FloorOverlayDefinition` | `OverlayType` | Verified concept | Overlay colors, texture, hide-underlay and additional material parameters. |
| `OcclusionManager` | `Class358` | Verified | Occluder storage, tile-visibility cache and occlusion tests. |
| `Occluder` | `Class385` | High | Oriented occlusion geometry carrying plane/type and corner/projection data. |
| `OcclusionRasterizer` | `Class347` | High | Rasterizes projected occlusion triangles/scanlines into the visibility/depth representation. |
| `SceneEntity` | `Entity_Sub1` | High | Common abstract scene renderable; owns plane/scene state, bounds, light lookup and render/visibility behavior. |
| `MultiTileSceneEntity` | `Entity_Sub1_Sub1` | High | Scene entity with explicit start/end tile X/Y footprint. |
| `SingleTileSceneEntity` | `Entity_Sub1_Sub2` | High | Position-based scene entity resolved from one current scene tile; exact gameplay subtype remains intentionally broad. |
| `WallDecorationEntity` | `Entity_Sub1_Sub3` | Verified | Created by all wall-decoration shape placement paths. |
| `FloorDecorationEntity` | `Entity_Sub1_Sub4` | Verified | Created/stored for the floor-decoration scene layer. |
| `WallEntity` | `Entity_Sub1_Sub5` | Verified | Created for wall/corner/unfinished-wall shapes; one tile can hold two wall pieces. |
| `SceneObjectLink` | `Class352` | High | Pooled linked node referencing a multi-tile scene entity from scene tiles. |
| `ScreenSpaceBounds` | `Class80` | Verified role | Active 2D capsule/pick bound with two projected endpoints and radius; supports screen-point hit testing. |
| `SceneEntityBounds` | `Class348` | High | 3D entity bound with min/max axes plus horizontal radial test. |
| `SceneEntityPickList` | `Class338` | High | Ordered list of scene pick entries built from rendered entities; optional duplicate suppression. |
| `SceneEntityPickEntry` | `Class353` | High | Pooled entity picking entry that tests `ScreenSpaceBounds` and delegates detailed hit testing to the entity/model. |
| `SceneLight` | `Node_Sub14` | Verified | Position, radius, color and intensity used by terrain/entities/map flicker effects. |
| `FlickeringLightDefinition` | `Class512` | Verified role | Reusable waveform/speed/amplitude/base-intensity definition used by special map flicker type 31. |
| `FlickeringLightDefinitionManager` | `Class519` | Verified role | Cache-backed manager for flickering-light definitions. |
| `HighResolutionClock` | `Class509` instance role | High | Instance responsibility is a `System.nanoTime()` clock; class also contains unrelated static methods from obfuscation bundling. |

Already-readable canonical types include `Map`, `RegionManager`, `ObjectDefinition`, `ObjectDefinitionManager`, `ModelDefinition`, `Model`, `GraphicsToolkit`, `TerrainData`, `Atmosphere`, `AtmosphereManager`, `MapFlickeringEffect` and `ShapeType`.

## Core ownership model

```text
RegionManager
    |
    v
Map : TerrainBuilder
    |
    +--> FloorOverlayDefinitionManager --> FloorOverlayDefinition
    +--> FloorUnderlayDefinitionManager --> FloorUnderlayDefinition
    +--> ObjectDefinitionManager --> ObjectDefinition --> ModelDefinition
    +--> FlickeringLightDefinitionManager --> FlickeringLightDefinition
    |
    v
SceneGraph
    |
    +--> SceneTile[plane][x][y]
    |      +--> WallEntity (0..2)
    |      +--> WallDecorationEntity (0..2)
    |      +--> FloorDecorationEntity (0..1)
    |      +--> SceneObjectLink --> MultiTileSceneEntity
    |      +--> optional SingleTileSceneEntity
    |
    +--> TerrainSurface[plane]
    +--> SceneLight[] / MapFlickeringEffect[]
    +--> SceneEntityPickList --> SceneEntityPickEntry --> ScreenSpaceBounds
    +--> OcclusionManager
               +--> Occluder[]
               +--> OcclusionRasterizer

GraphicsToolkit
    +--> createTerrainSurface
    +--> createModel(ModelDefinition)
    +--> create SceneLight
    +--> OpenGL / Direct3D / software backends
```

New Hagalaz server/web code should use this vocabulary, never the generated source identifiers.

## TerrainBuilder members

| Canonical member | Current member | Confidence |
| --- | --- | --- |
| `sceneGraph` | `aClass356_2767` | Verified |
| `floorOverlayDefinitions` | `aClass418_2765` | Verified |
| `floorUnderlayDefinitions` | `aClass499_2819` | Verified |
| `terrainData` | `aTerrainData_2811` | Verified |
| `tileHeights` | `tileHeights` | Verified |
| `overlayIds` | `overlayIds` | Verified |
| `underlayIds` | `underlayIds` | Verified |
| `overlayShapes` | `overlayPaths` | High |
| `overlayRotations` | `overlayRotations` | Verified |
| `cameraAdjustments` | `cameraAdjustments` | Existing readable field; exact higher-level semantics remain later work |
| `setShadingDelayed` | `method2684` | High |
| `releaseTerrainBuildScratch` | `method2685` | High |
| `initializeHeightArea` | `method2687` | High |
| `readCollisionData` | `readCollisionData` | Verified |
| `readDynamicChunkCollisionData` | `method2689` | High |
| `createTerrainSurfaces` | `readGroundMapData` | High |
| `buildTerrainSurfaces` | `method2692` | Verified responsibility |
| `readLandscapeData` | `readLandscapeData` | Verified |

Large scratch arrays inside terrain material/shape construction remain intentionally unnamed until each role is proven.

## TerrainSurface members

| Canonical member | Current member | Confidence |
| --- | --- | --- |
| `heightGrid` | `anIntArrayArray6394` | Verified |
| `tileCountX` | `anInt6397` | Verified |
| `tileCountY` | `anInt6393` | Verified |
| `tileSize` | `anInt6395` | Verified |
| `tileShift` | `anInt6396` | Verified |
| `getInterpolatedHeight` | `method6416` | Verified |
| `getTileHeight` | `method6417` | Verified |

Remaining abstract methods should be named only after comparing equivalent operations across multiple renderer backends.

## FloorUnderlayDefinition members

The underlay is one of the clearest definition formats.

| Canonical member | Current member | Confidence |
| --- | --- | --- |
| `rgbColor` | `anInt5860` | Verified |
| `hue` | `anInt5855` | Verified |
| `saturation` | `anInt5861` | Verified |
| `lightness` | `anInt5862` | Verified |
| `hueMultiplier` | `anInt5863` | Verified |
| `texture` | `anInt5856` | High |
| `textureScale` | `anInt5857` | High |
| `decode` | `method6072` | Verified |
| `decodeOpcode` | `method6073` | Verified |
| `calculateHsl` | `method6074` | Verified |

Opcode `4` and opcode `5` boolean fields are known to affect rendering/occlusion behavior, but their final semantic names should not be frozen until every use is traced.

## Floor-definition managers

### FloorUnderlayDefinitionManager

- `aNodeCache_5891` -> `definitions`
- `method6111` -> `getFloorUnderlayDefinition`

### FloorOverlayDefinitionManager

- `aNodeCache_4296` -> `definitions`
- `anInt4295` -> `definitionCount`
- `getOverlayType` -> `getFloorOverlayDefinition`

The overlay-manager field changed by overlay opcode `8` is still unresolved; do not call it a default/special overlay until proven.

## SceneGraph members

| Canonical member | Current member | Confidence |
| --- | --- | --- |
| `graphicsToolkit` | `aGraphicsToolkit3645` | Verified |
| `occlusionManager` | `aClass358_3649` | Verified |
| `activeTiles` | `aClass340ArrayArrayArray3653` | Verified |
| `primaryTiles` | `aClass340ArrayArrayArray3655` | High |
| `alternateTiles` | `aClass340ArrayArrayArray3657` | High |
| `activeTerrainSurfaces` | `aClass_xaArray3676` | Verified |
| `primaryTerrainSurfaces` | `aClass_xaArray3701` | High |
| `alternateTerrainSurfaces` | `aClass_xaArray3658` | High |
| `mapFlickeringEffects` | `aMapFlickeringEffectArray3679` | Verified |
| `entityPickList` | `aClass338_3697` | High |
| `tilesRangeX` | `tilesRangeX` | Verified |
| `tilesRangeY` | `tilesRangeY` | Verified |
| `maxPlanes` | `maxZ` | High |
| `getOrCreateTile` | `method4136` | Verified responsibility |
| `ensureTileStack` | `method4137` | High |
| `addFloorDecoration` | `method4142` | Verified from placement call sites |
| `addWallDecoration` | `method4144` | Verified from placement call sites |
| `addWalls` | `method4180` | Verified from placement call sites |

The exact semantics of the primary/alternate scene modes are not yet proven. Neutral names are preferable to guessing underwater/roof/etc.

## SceneTile members

| Canonical member | Current member | Confidence |
| --- | --- | --- |
| `tileBelow` | `aClass340_3380` | Verified |
| `plane` | `aByte3381` | High |
| `wallA` | `aClass432_Sub1_Sub5_3382` | Verified |
| `wallB` | `aClass432_Sub1_Sub5_3383` | Verified |
| `wallDecorationA` | `aClass432_Sub1_Sub3_3384` | Verified |
| `wallDecorationB` | `aClass432_Sub1_Sub3_3385` | Verified |
| `floorDecoration` | `aClass432_Sub1_Sub4_3386` | Verified |
| `objectLinks` | `aClass352_3388` | High |
| `singleTileEntity` | `aClass432_Sub1_Sub2_3391` | High; exact subtype unresolved |

Remaining short fields appear tied to visibility/occlusion metadata and should be renamed with the corresponding occlusion logic.

## Entity picking / bounds

### ScreenSpaceBounds

The current pick-bound class is mathematically a 2D capsule:

- two endpoints;
- a radius;
- an active flag;
- `hitTest(x, y)` computes distance to the segment or either endpoint and compares with radius.

Suggested members:

- first endpoint fields -> `startX`, `startY`;
- second endpoint fields -> `endX`, `endY`;
- `anInt673` -> `radius`;
- `aBoolean671` -> `active`;
- `method944` -> `hitTest` / `containsPoint`.

Use exact start/end field mapping only after tracing renderer population code, since the containment math is symmetric.

### SceneEntityBounds

The 3D bound stores center X/Y/Z, a horizontal radius squared and min/max limits for all axes. `method4019` rejects outside the axis-aligned limits and then tests horizontal X/Z radial distance.

- `method4019` -> `contains`
- `method4020` -> `setBounds`

Field-by-field min/max naming can be done mechanically from constructor assignments when the source rename is implemented.

### SceneEntityPickList / SceneEntityPickEntry

`SceneGraph` asks a rendered entity for a pick entry, associates the entity, then inserts it into the ordered pick list. The entry hit-test iterates the entity's `ScreenSpaceBounds` and delegates the detailed model/entity hit test.

This evidence is strong enough to prefer `SceneEntityPickList` / `SceneEntityPickEntry` over the earlier generic `SceneEntityList` / `SceneEntityEntry` names.

## Occlusion members

### OcclusionManager

- `aClass356_3710` -> `sceneGraph`
- `aClass347_3711` -> `rasterizer`
- `anIntArrayArrayArray3713` -> `tileVisibilityCache`
- `method4216` -> `addOccluder`
- `method4217` -> `removeOccluder`

Investigate `method4221` completely before selecting a lifecycle name such as `rebuildOccluders` or `invalidateOcclusion`.

`Occluder` field names should be renamed as one group after types `1`, `2`, `4`, `8`, `16` are mapped to exact orientations.

## Scene entity hierarchy

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
  |     +-- concrete model/animation variants
  |
  +-- FloorDecorationEntity
  |     +-- concrete model/animation variants
  |
  +-- WallEntity
        +-- concrete model/animation variants
```

Do not rename generated concrete `Sub1`/`Sub2` implementations to `Static`/`Dynamic` until their model cache and animation/update lifecycles have been compared.

## SceneLight members

The light is now fully interpretable at the primary-field level because `MapFlickeringEffect` creates it as `(x, y, z, radius, color, 1.0F)` and updates only the float during flicker evaluation.

| Canonical member | Current member | Confidence |
| --- | --- | --- |
| `position` | `aVector3f_7608` | Verified |
| `radius` | `anInt7606` | Verified |
| `color` | `anInt7607` | Verified |
| `intensity` | `aFloat7605` | Verified |
| `getX` | `method3318` | Verified |
| `getY` | `method3311` | Verified |
| `getZ` | `method3312` | Verified |
| `getRadius` | `method3316` | Verified |
| `getColor` | `method3313` | Verified |
| `getIntensity` | `method3317` | Verified |
| `setIntensity` | `method3314` | Verified |
| `setPosition` | `method3315` | Verified |

## FlickeringLightDefinition members

A map flicker with type `31` resolves this external definition and copies its values into `MapFlickeringEffect`.

| Canonical member | Current member | Confidence |
| --- | --- | --- |
| `waveform` | `anInt5959` | Verified from receiving field/switch |
| `speed` | `anInt5956` | High; used as time multiplier/flicker progression speed |
| `amplitude` | `anInt5958` | Verified from intensity equation |
| `baseIntensity` | `anInt5957` | Verified from intensity equation |
| `decode` | `method6187` | Verified |
| `decodeOpcode` | `method6188` | Verified |

`FlickeringLightDefinitionManager.method6217` -> `getFlickeringLightDefinition`.

Useful `MapFlickeringEffect` renames:

- `aClass330_Sub14_3467` -> `light`
- `anInt3473` -> `waveform`
- `anInt3474` -> `speed`
- `anInt3466` -> `amplitude`
- `anInt3452` -> `baseIntensity`
- `method4021` -> `createLight`
- `method4022` -> `setFlickerDefinition`
- `method4023` -> `useBuiltInFlickerPreset`
- `method4024` -> `updateIntensity`

## Terrain-height helper names

Several exact terrain-height helpers live as unrelated static methods on otherwise miscellaneous classes. Rename the methods, not the whole classes, unless the containing class is independently understood.

| Canonical helper | Current source locator | Confidence |
| --- | --- | --- |
| `generateBaseTerrainHeight` | `Class156.calculateHeight` | Verified |
| `interpolatedNoise` | `TextureLoader.method652` | Verified |
| `smoothedNoise` | `Class170.method2039` | Verified |
| `rawTerrainNoise` | `AbstractQueue_Sub1.method6486` | Verified |
| `cosineInterpolate` | `Class20.method466` | Verified |
| `sineTable` | `Class257.anIntArray2683` | Verified |
| `cosineTable` | `Class257.anIntArray2684` | Verified |
| `angleToRadians` | `Class257.method2541` | Verified |

See `terrain-height-generation.md` for the full algorithm using the semantic helper names.

## ModelDefinition cleanup

`ModelDefinition` is well named, but several members are misleading.

| Canonical member | Current member | Confidence |
| --- | --- | --- |
| `triangleVertexA` | `triangleViewSpaceX` | Verified |
| `triangleVertexB` | `triangleViewSpaceY` | Verified |
| `triangleVertexC` | `triangleViewSpaceZ` | Verified |
| `faceColors` | `colors` | Verified |
| `faceAlpha` | `alpha` | Verified |
| `facePriorities` | `priorities` | Verified |
| `faceTextures` | `textures` | Verified |
| `decodeLegacyFormat` | `method1199` | Verified |
| `decodeModernFormat` | `method1201` | Verified |
| `translate` | `method1194` | Verified |
| `rotate` | `method1195` | High |
| `scaleByPowerOfTwo` | `method1196` | Verified |
| `recolor` | `method1185` | Verified |
| `retexture` | `method1200` | Verified |

The `triangleViewSpace*` names are actively misleading: they are triangle topology vertex indices, not view-space positions.

## GraphicsToolkit aliases

Do not port the obfuscated graphics API. The high-value semantic operations needed for architecture are:

- `GraphicsToolkit.cn(...)` -> `createTerrainSurface(...)`;
- `GraphicsToolkit.cb(ModelDefinition, ...)` -> `createModel(...)`;
- the map-light factory used by `MapFlickeringEffect` -> `createSceneLight(...)`.

Concrete subclasses should be described by backend: OpenGL, Direct3D and software. Generated subclass names should not enter new Hagalaz architecture.

## HighResolutionClock caveat

The `Class509` **instance** responsibility is a nanosecond clock (`System.nanoTime()`). However the class also contains unrelated static utility methods due decompiler/obfuscator bundling. If the source is renamed, verify all construction/instance call sites and avoid accidentally implying that the unrelated static methods are clock behavior. A later cleanup may be better served by extracting the clock responsibility instead of globally renaming every static reference.

## Deliberately unresolved names

Continue tracing before freezing names for:

- all secondary `FloorOverlayDefinition` fields/opcodes;
- the two secondary underlay booleans;
- exact identity of `SingleTileSceneEntity`;
- concrete entity model/animation variants;
- exact primary/alternate SceneGraph mode semantics;
- occluder orientation constants and SceneTile occlusion shorts;
- renderer-specific `TerrainSurface` operations beyond height queries;
- atmosphere, roof-hiding and advanced visibility classes;
- model skinning/animation/effect metadata not needed by static rendering.

These are deobfuscation tasks, not reasons to leak generated names into Hagalaz.

## Recommended source-rename order

Use behavior-preserving, compilable rename groups:

1. floor definitions/managers;
2. `TerrainSurface` and `TerrainBuilder`;
3. `SceneTile`, `SceneObjectLink`, bounds/picking helpers and `SceneGraph`;
4. occluder/rasterizer/occlusion manager;
5. scene entity bases/categories;
6. `SceneLight`, flicker definition/manager and `MapFlickeringEffect` members;
7. high-confidence methods/fields in those classes;
8. `ModelDefinition` topology/member cleanup;
9. lower-confidence concrete entity variants, scene modes, atmosphere and backend-specific methods.

Run `./gradlew build` after every group. Do not mix behavior changes or broad formatting with the rename commits.

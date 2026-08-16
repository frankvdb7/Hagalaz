# External revision-742 rendering references

This document records public sources that can be used to triangulate the decompiled `Hagalaz.GameClient` rendering/map subsystem.

The local client source and its actual call graph remain authoritative. External sources are evidence for naming and behavior only when their structure matches the local revision.

## Primary reference: LostCityRS/RS742

Repository: `LostCityRS/RS742`.

This is a heavily refactored RuneScape 742 client and is the strongest public naming reference found for the scene/map subsystem. `Avexiis/RSPSi-742` explicitly credits it as the deobfuscated 742 client used while adding 742 map-editor support.

The LostCity README contains an important revision warning:

- its source is **742.1**;
- the historically circulated client was **742.2**;
- packet IDs are the same;
- packet read/write alternative methods differ.

Therefore:

> Use LostCityRS/RS742 as a semantic and structural naming reference after comparing the local class, fields, inheritance and call sites. Do not transplant its protocol read/write methods into Hagalaz.GameClient without local byte-level verification.

## Evidence hierarchy

For deobfuscation work in this subsystem, use evidence in this order:

1. local `Hagalaz.GameClient` construction, fields, inheritance, data flow and call sites;
2. structurally matching `LostCityRS/RS742` classes;
3. older/adjacent RuneTek clients with stronger canonical/Jagex terminology, such as 2011Scape;
4. map editors and RSPS implementations as secondary behavioral evidence only.

A public name is not sufficient by itself. The local class must match the responsibility.

## Revision-specific class vocabulary

The public 742 source resolves a number of classes more precisely than the first-pass names in `client-rendering-deobfuscation.md`.

| Current Hagalaz.GameClient | First-pass semantic alias | Preferred 742 reference name | Evidence |
| --- | --- | --- | --- |
| `Class274` | `TerrainBuilder` | `MapLoader` | Same ownership of floor type lists, scene, tile flags, height/overlay/underlay arrays and tile-shape lookup tables. |
| `Map` | `Map` | `ClientMapLoader` | Both extend the map loader and add loc/object definitions, environment/atmosphere data and static point-light decode. |
| `Class356` | `SceneGraph` | `Scene` | Same renderer, tile arrays, floor models, entities, lights and occlusion ownership. |
| `Class340` | `SceneTile` | `Tile` | Same tile-below, wall, wall-decor, ground-decor, object-layer, primary-layer-list and short metadata slots. |
| `Class_xa` | `TerrainSurface` | `FloorModel` | Exact obfuscated base name `xa`; same height grid, tile dimensions/scale and bilinear fine-height query. |
| `GraphicsToolkit` | `GraphicsToolkit` | `RendererToolkit` | Local decompiler header is `Class_ra`; public class is annotated obfuscated name `ra`; responsibilities match. |
| `Entity_Sub1` | `SceneEntity` | `GraphEntity` | Same scene reference, level state, screen bounds, light lookup, entity bounds and pick/render contract. |
| `Entity_Sub1_Sub1` | `MultiTileSceneEntity` | `PrimaryLayerEntity` | Same rectangular tile footprint and multi-tile primary scene layer role. |
| `Entity_Sub1_Sub2` | `SingleTileSceneEntity` | `ObjLayerEntity` | Same single-position object-layer visibility/light/occlusion behavior. |
| `Entity_Sub1_Sub3` | `WallDecorationEntity` | `WallDecorLayerEntity` | Matches the wall-decoration layer. |
| `Entity_Sub1_Sub4` | `FloorDecorationEntity` | `GroundDecorLayerEntity` | Matches the ground-decoration layer. |
| `Entity_Sub1_Sub5` | `WallEntity` | `WallLayerEntity` | Matches wall layer placement and light/occlusion behavior. |
| `Class352` | `SceneObjectLink` | `PrimaryLayerEntityList` | Pooled linked-list node containing a primary-layer entity. |
| `Class80` | `ScreenSpaceBounds` | `ScreenBoundingBox` | Same valid flag, two projected endpoints, radius and capsule point test. |
| `Class348` | `SceneEntityBounds` | `EntityBounds` | Same origin/radius/min/max construction and containment test. |
| `Class338` | `SceneEntityPickList` | `PickableEntityList` | Same ordered list and optional duplicate suppression. |
| `Class353` | `SceneEntityPickEntry` | `PickableEntity` | Same pooled entity wrapper and screen-bound + detailed hit-test path. |
| `Node_Sub14` | `SceneLight` | `Light` | Same position, radius/color-like integer parameters and intensity float with position/intensity mutation. |
| `MapFlickeringEffect` | `MapFlickeringEffect` | `StaticPointLight` | Same map-decoded range masks, levels, light object and flicker configuration. |
| `Class491` | `FloorUnderlayDefinition` | `FloorUnderlayType` | Same opcodes and HSL conversion. |
| `Class499` | `FloorUnderlayDefinitionManager` | `FloorUnderlayTypeList` | Same cache-backed floor-underlay lookup role. |
| `OverlayType` | `FloorOverlayDefinition` | `FloorOverlayType` | Same overlay config role. |
| `Class418` | `FloorOverlayDefinitionManager` | `FloorOverlayTypeList` | Same cache-backed overlay list role. |
| `Class512` | `FlickeringLightDefinition` | `LightType` | Same four opcode-decoded parameters used for special static point-light type 31. |
| `Class519` | `FlickeringLightDefinitionManager` | `LightTypeList` | Same cached light-type lookup. |
| `ModelDefinition` | `ModelDefinition` | `ModelUnlit` (pending final structural rename decision) | Same raw vertex/face/texture ownership and `FF FF` modern-format discriminator before creation of backend `Model`. |

The preferred reference name should replace the first-pass alias in future source-level GameClient refactors once the local structural match has been verified.

## Entity hierarchy exposed by the reference

The public client provides concrete names for layers that the first-pass investigation intentionally left broad:

```text
GraphEntity
├── PrimaryLayerEntity
│   ├── PathingEntity
│   │   ├── PlayerEntity
│   │   └── NpcEntity
│   ├── StaticSceneryEntity
│   └── DynamicSceneryEntity
├── ObjLayerEntity
├── WallLayerEntity
│   ├── StaticWallEntity
│   └── DynamicWallEntity
├── WallDecorLayerEntity
│   ├── StaticWallDecorLayerEntity
│   └── DynamicWallDecorEntity
└── GroundDecorLayerEntity
    ├── StaticGroundDecorEntity
    └── DynamicGroundDecorEntity
```

Use this hierarchy as a comparison target, not as a blind rename table. For example, the local `Entity_Sub1_Sub5_Sub1` has already been compared with `StaticWallEntity` and exhibits the same object-definition list, model, shadow, bounds, object ID, shape/orientation and shadow/activity state.

## Renderer toolkit vocabulary

The public 742 `RendererToolkit` factory exposes five backend modes and a shared GPU abstraction:

- `PureJavaToolkit`;
- `SoftwareToolkit`;
- `GlToolkit`;
- `GlxToolkit`;
- `DxToolkit`;
- shared `GpuToolkit` base where applicable.

This is more precise than documenting only “OpenGL / Direct3D / software”. Match each local `GraphicsToolkit_Sub*` by imports, constructor behavior and owned resources before renaming it.

The local `GraphicsToolkit_Sub1` imports `jaggl.OpenGL` and native memory helpers in the same way as the public `GlToolkit`, making that a strong concrete comparison target.

## Floor model terminology

The public `FloorModel` is especially strong evidence because its obfuscated name is literally `xa`, matching the local original `Class_xa` name.

It owns:

- X/Y tile counts;
- tile size;
- tile shift;
- the integer height grid;
- a bilinear scene-coordinate height query named `getFineHeight`.

This supports using `FloorModel` and `getFineHeight` rather than the more generic `TerrainSurface` / `getInterpolatedHeight` vocabulary when deobfuscating the Java client.

Hagalaz server/web architecture should still use renderer-neutral terminology where that is clearer. Source deobfuscation vocabulary and public HTTP DTO vocabulary do not have to be identical.

## Floor type findings

The public `FloorUnderlayType` confirms the locally derived opcode meanings:

- opcode `1`: RGB and HSL derivation;
- opcode `2`: texture ID, with `65535 -> -1`;
- opcode `3`: texture scale (`ushort << 2`);
- opcodes `4` and `5`: boolean flags.

The public reference also leaves the opcode-4/5 booleans unresolved. They should remain unnamed in Hagalaz.GameClient until their uses prove exact semantics.

This is useful negative evidence: a plausible material name is not justified merely because another refactored 742 client exists.

## Model terminology

The public `ModelUnlit` is the decoded cache-model representation before the renderer produces a `Model`. Its constructor chooses between the same two binary layouts observed locally:

```text
last two bytes == FF FF -> newer format
otherwise              -> legacy format
```

This reinforces the architectural distinction:

```text
cache model bytes
    -> ModelUnlit / local ModelDefinition
    -> renderer toolkit
    -> Model
```

The Hagalaz web API should expose semantic mesh data rather than either Java class directly.

## Secondary reference: 2011Scape

`2011Scape/2011scape-client` targets revision 667 rather than 742, but its maintainers state that they use Jagex's canonical naming where possible, sourced from partially unobfuscated Jagex clients/debug symbols and other reverse-engineering work.

Use it when choosing terminology that LostCity still leaves generic, but only after accounting for revision differences.

## Secondary reference: RSPSi-742

`Avexiis/RSPSi-742` is useful for practical 742 map/model/cache editor behavior and explicitly builds on the LostCity reference. Its own README describes the implementation as experimental and notes model/texture problems and incomplete under-map landscape handling.

Use it to discover edge cases and editor workflows, not as the authoritative renderer/cache format specification.

## Repeatable deobfuscation tooling

LostCity publishes a RuneScape-specific Java deobfuscator, and the RuneWiki `rs-deob` project maintains deobfuscation profiles/remaps across many client revisions.

For future GameClient work outside rendering, check those sources before manually re-deriving every class from scratch. Imported names still require verification against the local source revision.

## Source links

- `https://github.com/LostCityRS/RS742`
- `https://github.com/LostCityRS/Deobfuscator`
- `https://github.com/RuneWiki/rs-deob`
- `https://github.com/2011Scape/2011scape-client`
- `https://github.com/Avexiis/RSPSi-742`

# 3D rendering

This directory records the verified RuneScape scene-rendering behavior that is relevant to Hagalaz and the target architecture for rendering those scenes in the web UI.

The analysis was performed against:

- `frankvdb7/Hagalaz` `main` at `ba134cbcd531a6cd3b35c19b20404084e926bbfc`.
- `frankvdb7/Hagalaz.GameClient` `main` at `6eac3762cc46cec484131369691b5221fd1277bf`.

## Documents

- [Rendering deobfuscation map](client-rendering-deobfuscation.md) defines the canonical human-readable names for the terrain, scene, floor-material, occlusion, entity, light and model concepts found in the decompiled client, including confidence and source locators.
- [Game client renderer](game-client-renderer.md) records the verified terrain, object, model, scene, coordinate and renderer-backend behavior using those semantic names.
- [Map cache decode/encode guide](cache-map-codecs.md) gives byte-level implementation instructions for cache containers, `mX_Y` terrain, `lX_Y` object placements, smart/huge-smart delta coding, XTEA, canonical writing, corruption guards, round-trip tests, and the current Hagalaz codec gaps.
- [Implicit terrain height generation](terrain-height-generation.md) records the exact opcode-`0` plane-0 height algorithm, including deterministic raw noise, smoothing, cosine interpolation, client lookup-table generation, coordinate offsets, scaling, encoder implications, and required parity tests.
- [Web renderer foundation](web-renderer-foundation.md) describes the current Hagalaz gaps, target data contracts, web architecture, rendering-engine choice, phased implementation, and verification strategy.

## Naming rule

The GameClient is only partially deobfuscated, but generated names such as `Class274`, `Class356`, `Class491` and `method6416` are **not architecture**.

The documents in this directory use stable semantic names such as `TerrainBuilder`, `SceneGraph`, `FloorUnderlayDefinition`, `OcclusionManager`, `TerrainSurface` and `SceneLight`. The generated identifiers belong only in the [deobfuscation source-locator map](client-rendering-deobfuscation.md) so somebody can find the current decompiled source until the source-level rename is completed.

Do not copy a generated GameClient identifier into a new Hagalaz API, DTO, service, TypeScript type or renderer abstraction. If a client concept is not sufficiently understood to name, keep it internal and document the uncertainty instead of exporting an obfuscated name.

## Current status

Hagalaz does not currently have a working 3D RuneScape scene renderer in `Hagalaz.Web.App` on `main`. `maplibre-gl` is installed, but there is no map route, no renderer feature, and no MapLibre/Three.js/Babylon scene implementation in the Angular source.

The cache service is also not yet able to supply enough rendering data. The current map model is intentionally gameplay-oriented: it preserves terrain flags and object placements, but not the height, overlay, underlay, floor-material, object render-definition, model-geometry, or texture information needed to reproduce the client scene.

This means the first implementation work belongs at the cache/render-data boundary rather than in a more sophisticated Angular canvas component.

## Foundation rules

1. **Decode RuneScape cache semantics once on the server.** The browser should consume a documented rendering contract rather than implement a second cache decoder.
2. **Keep gameplay and rendering contracts separate.** `IMapType` and `IObjectType` should not grow into rendering-engine DTOs. Rendering projections can reuse the same underlying cache providers/codecs while exposing the extra scene data deliberately.
3. **Keep scene assembly independent from the graphics backend.** The original client follows this principle through `GraphicsToolkit`. The web implementation only needs a small renderer boundary, not a port of that large API.
4. **Convert coordinate systems exactly once.** RuneScape cache/client coordinates remain authoritative in transport/domain data. A scene assembler converts them to the selected web-engine axes/units at one boundary.
5. **Preserve region and chunk semantics.** A normal map region is 64×64 tiles, has four planes, and dynamic map parts are assembled from rotated 8×8 chunks.
6. **Fidelity before effects.** Correct heights, floor shapes/materials, object models, shape selection, rotation, and transforms come before shadows, atmosphere, animation, water, or post-processing.
7. **Do not make MapLibre the core scene renderer.** It is useful for geographic-style overview/navigation surfaces, but its Mercator/globe camera and custom-layer model do not solve RuneScape scene decoding, model assembly, or plane semantics.
8. **Keep cache payload and container encoding separate.** Terrain and location payload codecs must not know about compression, XTEA, CRC, Whirlpool, reference-table mutation, or Hagalaz's internal synthetic `MapCodec` stream.
9. **Preserve source semantics needed for writing.** Runtime/effective values such as bridge-adjusted planes must not overwrite the raw source plane or original height encoding if later cache encoding depends on it.
10. **Use semantic names, never generated decompiler names, in new architecture.** Generated source identifiers exist only as temporary source locators and must not become permanent server/web vocabulary.

## Terminology

- **Region**: 64×64 map tiles addressed by a region ID.
- **Plane**: one of the four RuneScape height/scene levels.
- **Chunk / region part**: an 8×8 subsection of a region used when assembling dynamic maps.
- **Tile scale**: the legacy client uses 512 scene units per tile (`1 << 9`).
- **Terrain flags**: collision/bridge/plane-related tile metadata; these are not the terrain height or floor material.
- **Overlay / underlay**: floor definitions used together with tile shape/rotation and height data to construct visible terrain.
- **Object placement**: object ID + shape + rotation + local X/Y/Z.
- **Source plane**: plane encoded in the raw location payload before bridge/effective-plane adjustment.
- **Effective plane**: runtime plane after terrain/bridge semantics have been applied.
- **Object render definition**: model IDs selected by shape plus recolor/retexture, scale, offset, contouring, transform, animation, and related render metadata.
- **Model definition**: decoded vertex/triangle/texture data from the cache before conversion to a backend-specific render model.

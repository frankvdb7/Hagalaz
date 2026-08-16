# 3D rendering

This directory records the verified RuneScape scene-rendering behavior that is relevant to Hagalaz and the target architecture for rendering those scenes in the web UI.

The analysis was performed against:

- `frankvdb7/Hagalaz` `main` at `ba134cbcd531a6cd3b35c19b20404084e926bbfc`.
- `frankvdb7/Hagalaz.GameClient` `main` at `6eac3762cc46cec484131369691b5221fd1277bf`.

## Documents

- [Game client renderer](game-client-renderer.md) maps the partially deobfuscated client classes to stable rendering concepts and records verified terrain, object, model, scene, coordinate, and backend behavior.
- [Web renderer foundation](web-renderer-foundation.md) describes the current Hagalaz gaps, target data contracts, web architecture, rendering-engine choice, phased implementation, and verification strategy.

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

## Terminology

- **Region**: 64×64 map tiles addressed by a region ID.
- **Plane**: one of the four RuneScape height/scene levels.
- **Chunk / region part**: an 8×8 subsection of a region used when assembling dynamic maps.
- **Tile scale**: the legacy client uses 512 scene units per tile (`1 << 9`).
- **Terrain flags**: collision/bridge/plane-related tile metadata; these are not the terrain height or floor material.
- **Overlay / underlay**: floor definitions used together with tile shape/rotation and height data to construct visible terrain.
- **Object placement**: object ID + shape + rotation + local X/Y/Z.
- **Object render definition**: model IDs selected by shape plus recolor/retexture, scale, offset, contouring, transform, animation, and related render metadata.
- **Model definition**: decoded vertex/triangle/texture data from the cache before conversion to a backend-specific render model.

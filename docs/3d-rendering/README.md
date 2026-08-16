# 3D rendering

This directory records the verified RuneScape scene-rendering behavior that is relevant to Hagalaz and the target architecture for rendering those scenes in the web UI.

The primary local analysis was performed against:

- `frankvdb7/Hagalaz` `main` at `ba134cbcd531a6cd3b35c19b20404084e926bbfc`.
- `frankvdb7/Hagalaz.GameClient` `main` at `6eac3762cc46cec484131369691b5221fd1277bf`.

Public revision-adjacent sources are used only as additional evidence. In particular, `LostCityRS/RS742` is a strong structural/naming reference but documents itself as 742.1, while the historically circulated client was 742.2 and has different packet read/write alternative methods.

## Documents

- [Rendering deobfuscation map](client-rendering-deobfuscation.md) records the local source locators, first-pass semantic mappings, confidence and evidence for the terrain, scene, floor-material, occlusion, entity, light and model concepts.
- [External revision-742 references](external-742-reference.md) records the public 742 cross-check, revision caveats, stronger revision-specific names, renderer backend vocabulary and the evidence hierarchy used when those names supersede a first-pass alias.
- [Game client renderer](game-client-renderer.md) records the verified terrain, object, model, scene, coordinate and renderer-backend behavior.
- [Map cache decode/encode guide](cache-map-codecs.md) gives byte-level implementation instructions for cache containers, `mX_Y` terrain, `lX_Y` object placements, smart/huge-smart delta coding, XTEA, canonical writing, corruption guards, round-trip tests, and the current Hagalaz codec gaps.
- [Implicit terrain height generation](terrain-height-generation.md) records the exact opcode-`0` plane-0 height algorithm, including deterministic raw noise, smoothing, cosine interpolation, client lookup-table generation, coordinate offsets, scaling, encoder implications, and required parity tests.
- [Web renderer foundation](web-renderer-foundation.md) describes the current Hagalaz gaps, target data contracts, web architecture, rendering-engine choice, phased implementation, and verification strategy.

## Naming rule

The GameClient is only partially deobfuscated, but generated names such as `Class274`, `Class356`, `Class491` and `method6416` are **not architecture**.

Use the local source mapping together with the public 742 cross-check. Several initial generic names now have stronger revision-specific equivalents, for example:

- `Class274` -> `MapLoader`;
- current `Map` -> `ClientMapLoader`;
- `Class356` -> `Scene`;
- `Class340` -> `Tile`;
- `Class_xa` -> `FloorModel`;
- `GraphicsToolkit` -> `RendererToolkit`;
- `Entity_Sub1` -> `GraphEntity`;
- `Class80` -> `ScreenBoundingBox`;
- `Class348` -> `EntityBounds`;
- `Class338` -> `PickableEntityList`;
- `Class353` -> `PickableEntity`;
- `Node_Sub14` -> `Light`;
- `MapFlickeringEffect` -> `StaticPointLight`;
- floor cache records follow `FloorUnderlayType` / `FloorOverlayType` and `*TypeList` terminology in the public 742 reference.

The exact mapping, evidence and revision caveats are in [External revision-742 references](external-742-reference.md). Generated identifiers belong only in source-locator material so somebody can find the current decompiled source until the source-level rename is completed.

Do not copy a generated GameClient identifier into a new Hagalaz API, DTO, service, TypeScript type or renderer abstraction. If a client concept is not sufficiently understood to name, keep it internal and document the uncertainty instead of exporting an obfuscated name.

Source-level GameClient terminology and public Hagalaz API terminology do not have to be identical. A name such as `FloorModel` is useful for matching the original client subsystem, while a server/web rendering DTO may still use clearer renderer-neutral terrain terminology.

## Evidence rule

When deobfuscating or documenting a GameClient concept, use evidence in this order:

1. local `Hagalaz.GameClient` construction, fields, inheritance, algorithms and call sites;
2. a structurally matching class in `LostCityRS/RS742`;
3. adjacent RuneTek clients with stronger canonical/Jagex naming evidence, such as 2011Scape;
4. editors/RSPS implementations only as secondary triangulation.

Never replace a local codec or protocol operation solely because an external project does it differently. The 742.1/742.2 packet-read/write distinction is a concrete example of why structural naming evidence and byte-level protocol evidence must remain separate.

## Current status

Hagalaz does not currently have a working 3D RuneScape scene renderer in `Hagalaz.Web.App` on `main`. `maplibre-gl` is installed, but there is no map route, no renderer feature, and no MapLibre/Three.js/Babylon scene implementation in the Angular source.

The cache service is also not yet able to supply enough rendering data. The current map model is intentionally gameplay-oriented: it preserves terrain flags and object placements, but not the height, overlay, underlay, floor-material, object render-definition, model-geometry, or texture information needed to reproduce the client scene.

This means the first implementation work belongs at the cache/render-data boundary rather than in a more sophisticated Angular canvas component.

## Foundation rules

1. **Decode RuneScape cache semantics once on the server.** The browser should consume a documented rendering contract rather than implement a second cache decoder.
2. **Keep gameplay and rendering contracts separate.** `IMapType` and `IObjectType` should not grow into rendering-engine DTOs. Rendering projections can reuse the same underlying cache providers/codecs while exposing the extra scene data deliberately.
3. **Keep scene assembly independent from the graphics backend.** The original client follows this principle through its renderer toolkit abstraction. The web implementation only needs a small renderer boundary, not a port of that large API.
4. **Convert coordinate systems exactly once.** RuneScape cache/client coordinates remain authoritative in transport/domain data. A scene assembler converts them to the selected web-engine axes/units at one boundary.
5. **Preserve region and chunk semantics.** A normal map region is 64×64 tiles, has four planes, and dynamic map parts are assembled from rotated 8×8 chunks.
6. **Fidelity before effects.** Correct heights, floor shapes/materials, object models, shape selection, rotation, and transforms come before shadows, atmosphere, animation, water, or post-processing.
7. **Do not make MapLibre the core scene renderer.** It is useful for geographic-style overview/navigation surfaces, but its Mercator/globe camera and custom-layer model do not solve RuneScape scene decoding, model assembly, or plane semantics.
8. **Keep cache payload and container encoding separate.** Terrain and location payload codecs must not know about compression, XTEA, CRC, Whirlpool, reference-table mutation, or Hagalaz's internal synthetic `MapCodec` stream.
9. **Preserve source semantics needed for writing.** Runtime/effective values such as bridge-adjusted planes must not overwrite the raw source plane or original height encoding if later cache encoding depends on it.
10. **Use semantic names, never generated decompiler names, in new architecture.** Generated source identifiers exist only as temporary source locators and must not become permanent server/web vocabulary.
11. **External names require a local structural match.** Public deobfuscated clients are evidence, not authority over a different subrevision.

## Terminology

- **Region**: 64×64 map tiles addressed by a region ID.
- **Plane**: one of the four RuneScape height/scene levels.
- **Chunk / region part**: an 8×8 subsection of a region used when assembling dynamic maps.
- **Tile scale**: the legacy client uses 512 scene units per tile (`1 << 9`).
- **Terrain flags**: collision/bridge/plane-related tile metadata; these are not the terrain height or floor material.
- **Overlay / underlay**: floor types used together with tile shape/rotation and height data to construct visible terrain.
- **Object placement**: object ID + shape + rotation + local X/Y/Z.
- **Source plane**: plane encoded in the raw location payload before bridge/effective-plane adjustment.
- **Effective plane**: runtime plane after terrain/bridge semantics have been applied.
- **Object render definition**: model IDs selected by shape plus recolor/retexture, scale, offset, contouring, transform, animation, and related render metadata.
- **Raw/unlit model**: decoded vertex/triangle/texture data from the cache before conversion to a backend-specific render `Model`; the public 742 reference names this concept `ModelUnlit`.

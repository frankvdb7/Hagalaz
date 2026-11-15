# Design

## Context

The GameClient writes object shadows into a per-plane byte grid while placing locations (`Map.addToClip`, `Class_xa.LA`), filters it, and multiplies floor lightness by `74 - shadow` before palette conversion. Details are in `Hagalaz.GameClient/docs/rendering/terrain-color-blending-and-lighting.md`.

## Decisions

- The Cache API owns `ShadowStrength`, because it needs the base definition's animation state and the assembled model radius. Zero means no shadow. The client uses the base definition, not a transform variant.
- `map-object-shadows.ts` owns the grid and filter as pure functions. It reads placements the same way the object builder does, including draft transforms, additions and hidden filters.
- The floor shading path applies darkening per corner, so plain, layered and spread floors all share it. Darkening is relative to unshadowed ground because the web floors do not apply the client's base brightness of 74. The ratio is raised to 0.7 for the palette gamma and 2.2 for linear output.
- `MapScenePreparation` owns the settled field and the terrain cache includes its signature. While assets load it keeps the previous field, so a plane toggle or progressive load neither drops shadows nor rebuilds the terrain.

## Risks

- An object edit that changes the field rebuilds the terrain once.
- Shadows stop at the edge of the selection, because neighbouring placements are not loaded.
- Existing appearances from an older Cache service have no strength and cast nothing until it restarts.

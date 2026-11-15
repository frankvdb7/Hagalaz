# Design

## Context

The client gives each corner of a floor triangle its own texture and draws the triangle once per texture, fading by vertex alpha (`Class_xa_Sub1.method6419`, `Node_Sub16`). It does not check material effects there, and each pass applies its own material, so an effect 7 floor keeps its reflection while it blends.

## Decisions

- `TerrainLayer.reflective` is set by `addLayeredFloor` from the loaded material. Terrain already rebuilds when a texture's effect becomes known, so the flag needs no separate invalidation. The program key and the scene's material key include it.
- The blend shader takes one cube sample and uses the sum of `weight * texture alpha` over reflective layers as the reflection amount. This equals the client's per-layer result when one layer is fully weighted, and differs only slightly inside a transition, where the alpha is low.
- The reflection GLSL lives in one function in `material-effects.ts`, with the normal, view position and amount as inputs, so the plain and blended paths cannot drift apart.
- `MeshBasicMaterial` has no view position or normal, so the terrain shader passes its own varyings only when a layer reflects. Non-reflective blends keep their existing shader.
- Only effect 7 is admitted. The other effects need animation or noise that the blend shader does not have.

## Risks

- The blended result between layers is an approximation of the client's separate passes. A side-by-side check with the client is still open.

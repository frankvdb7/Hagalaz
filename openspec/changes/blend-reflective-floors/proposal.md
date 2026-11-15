# Proposal

## Why

Region 3943 mixes an ordinary sand underlay with a reflective effect 7 underlay. The map editor skips floor blending for any texture with a material effect, so every boundary between them is a hard tile edge. The client blends them.

## What Changes

Goal: blend effect 7 underlays with neighbouring underlays as the client does, while keeping their reflection.

In scope:

- Allowing opaque effect 7 textures into the terrain blend.
- Applying the client's reflection in the blend shader, weighted by each reflective layer's contribution and texture alpha.

Non-goals:

- Blending water effects (2, 4, 5, 8, 9), effect 1, or non-opaque textures.
- Overlay blending or changes to the plain effect 7 path.

Existing mechanisms reused: `TerrainMaterial`, the terrain weights, the region cube uniforms owned by `ModelScene`, and the effect 7 reflection shader.

Acceptance criteria:

- An effect 7 underlay next to an ordinary underlay renders through the terrain blend, with no hard tile edge.
- A fully weighted effect 7 layer looks the same as the plain effect 7 path.
- Blends with no reflective layer keep their existing shader.

## Impact

Admin map editor terrain only: `map-scene-geometry.ts` and the shared model viewer shaders.

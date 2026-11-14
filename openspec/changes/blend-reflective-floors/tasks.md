# Tasks

## 1. Blending

- [x] 1.1 Share the effect 7 reflection shader between the plain and blended paths, verifying by scene tests that the plain path is unchanged.
- [x] 1.2 Apply the reflection in the terrain blend for reflective layers, verifying by material tests the weighted amount, the shared cube uniforms and unchanged non-reflective shaders.
- [x] 1.3 Admit opaque effect 7 layers in `addLayeredFloor`, verifying by geometry tests that effect 7 blends and other effects do not.

## 2. Verification

- [x] 2.1 Run admin and public tests, `lint:biome`, the admin build and strict OpenSpec validation.
- [x] 2.2 Check region 3943 in the map editor for soft sand edges and a working shader.
- [ ] 2.3 Compare region 3943 against the client.

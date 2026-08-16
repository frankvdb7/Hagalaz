## 1. Foundation and Client-Parity Reference

- [x] 1.1 Inventory the current `Hagalaz.Web.App` map/rendering implementation and verify whether the installed MapLibre dependency is actively used.
- [x] 1.2 Trace the authoritative `Hagalaz.GameClient` region, terrain, object, model, scene and graphics-backend paths and separate verified behavior from still-obfuscated details.
- [x] 1.3 Document region/chunk structure, terrain opcodes, legacy units, height conventions, floor material inputs, object shape-to-model selection and model geometry.
- [x] 1.4 Document the current Hagalaz cache/service gaps and the target web ownership boundaries, renderer decision and phased milestones.

## 2. Render-Focused Cache Decoding

- [ ] 2.1 Add focused failing tests for terrain opcode 0/1 height semantics, overlay ID/path/rotation, terrain flags, underlay IDs and upper-plane spacing using small deterministic byte fixtures.
- [ ] 2.2 Introduce a render-focused terrain representation that preserves visible height/floor data without changing the existing gameplay meaning of `IMapType.TerrainData`.
- [ ] 2.3 Define and test deterministic region-border height handling so adjacent 64×64 terrain meshes can share the correct seam.
- [ ] 2.4 Add floor overlay and underlay decoders/projections for the semantic fields required by a selected representative region.
- [ ] 2.5 Add a static model-definition decoder/projection covering vertices, triangle indices, colors/alpha and the texture mapping data required by representative models.
- [ ] 2.6 Add a separate object render-definition projection containing shape-to-model groups plus required static recolor/retexture/scale/offset/contour metadata.
- [ ] 2.7 Add client-parity tests for object shape selection, transforms, representative model bounds/counts and dynamic 8×8 chunk rotations.

## 3. Cache-Service Rendering Contracts

- [ ] 3.1 Define bounded region-render DTOs/endpoints that expose four planes of terrain input plus object placements without requiring raw cache decoding in the browser.
- [ ] 3.2 Define bounded object-render-definition and model-mesh endpoints/DTOs.
- [ ] 3.3 Define floor material/texture delivery endpoints required by the representative region; do not expose unexplained obfuscated fields.
- [ ] 3.4 Add API validation/contract tests, including invalid IDs, XTEA requirements, response bounds and representative payload completeness.
- [ ] 3.5 Record representative response sizes and latency before selecting JSON versus a more compact mesh transport.

## 4. Single-Region Web Vertical Slice

- [ ] 4.1 Add the dedicated 3D rendering dependency in the implementation change that consumes it; Three.js is the current recommended choice and the dependency justification must be recorded there.
- [ ] 4.2 Add a map route/page plus a minimal render-data service, scene assembler and renderer adapter; avoid empty speculative abstractions.
- [ ] 4.3 Implement one coordinate conversion owner using a region-local scene origin and 512 legacy units per tile.
- [ ] 4.4 Build real four-plane terrain meshes from the render-region contract, including correct heights and overlay shape/rotation.
- [ ] 4.5 Resolve decoded floor colors/material inputs for the representative region without placeholder height/material generation.
- [ ] 4.6 Add diagnostic orbit/pan/zoom controls, focus/reset and per-plane visibility controls.
- [ ] 4.7 Add pure TypeScript geometry tests for coordinate conversion, flat/sloped tiles, overlay rotations and region-edge coordinates.

## 5. Static Object Rendering

- [ ] 5.1 Select model groups from the object placement shape and fetch/reuse required model meshes.
- [ ] 5.2 Apply model combination, inversion, placement rotation, definition scale/offset, recolor/retexture and required ground contouring.
- [ ] 5.3 Render representative walls, decorations, standard objects and floor decorations at verified locations/orientations.
- [ ] 5.4 Add object picking/inspection that exposes object ID, shape, rotation and local/world coordinates for debugging.
- [ ] 5.5 Add deterministic object geometry/placement tests before adding screenshot regression coverage.

## 6. Multi-Region and Fidelity Follow-ups

- [ ] 6.1 Add adjacent-region loading and verify height/material seams and resource cleanup.
- [ ] 6.2 Add dynamic 8×8 region-part assembly if required by the viewer workflow, using the same tested rotation semantics as the cache/client path.
- [ ] 6.3 Profile representative dense scenes and record payload, assembly, draw-call, frame-time and unload-memory measurements before adding batching/instancing/workers/persistent browser caching.
- [ ] 6.4 Investigate and add decoded textures, then only the material/shader behavior proven necessary by fidelity tests.
- [ ] 6.5 Investigate roof hiding, occlusion, atmosphere/skybox/fog, lighting/shadows and water as independent fidelity increments.
- [ ] 6.6 Treat animation/transformed live entities as optional later scope rather than a prerequisite for the static map viewer.

## 7. Documentation and Verification

- [ ] 7.1 Keep `docs/3d-rendering/game-client-renderer.md` updated whenever relevant GameClient classes are deobfuscated or a documented inference becomes verified.
- [ ] 7.2 Record texture/material and roof/occlusion/camera findings before adding their semantics to stable render DTOs.
- [ ] 7.3 For each implementation phase, run focused decoder/API/web tests, relevant project tests/builds, strict OpenSpec validation and `git diff --check`.

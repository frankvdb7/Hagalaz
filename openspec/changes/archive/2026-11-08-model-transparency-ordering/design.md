# Design

## Context

See proposal.md for the goal and acceptance criteria. `ModelGeometryDecoder` locates priority streams in both formats but currently discards them. The newer-format decoder projects topology into the legacy decoder and writes a zero priority header. `ModelEndpoints.AssembleModel` concatenates face arrays, so it must preserve the effective priorities of each part. `GetModelDto` and `CacheModelGeometry` have no priority field.

`ModelScene.assignSurfaceGroups` currently groups adjacent source faces by material. Three.js sorts render items, not triangles within these groups. Its installed `WebGLRenderLists.js` uses the same object depth for every group of a mesh, which cannot resolve overlapping faces.

The client reference is `Model_Sub3.method4542` and the constructor sort key, plus `Model_Sub1` constructor ordering, in the sibling GameClient project. The software explicit transparent pass visits priorities 0 through 11 in ascending order. Constructor keys also include material metadata and source order. `docs/rendering/model-shading-alpha-and-order.md` explicitly cautions against claiming that a global priority sort reproduces the entire renderer.

## Goals / Non-Goals

Keep ordering in the existing shared viewer. Implement the verified priority precedence plus depth ordering within a priority for inspection. Preserve cache source identity independently of draw order. Complete client constructor/material/billboard sorting and pixel-identical rendering remain outside this change.

## Decisions

### Normalize priority data once

The decoder owns effective face priorities. Add an optional appended priority array to its geometry record and the response DTO, using face-aligned values for uniform and explicit priority formats. Preserve original unsigned byte values rather than inventing or clamping values. Populate all newly decoded faces, including a default of zero where that is the encoded uniform priority. Assembly concatenates these arrays; the TypeScript DTO and `prepareCacheModel` retain them unchanged by axis conversion or reversed corner winding. Older API payloads without priorities remain usable with default zero.

Rejected: exposing separate default and per-face fields across every layer. The renderer needs one effective value per face, and assembly already uses face-aligned arrays.

### Keep source geometry separate from ordered submissions

`ModelScene` owns classification, camera-dependent ordering and disposal. Keep source indices and attributes immutable for identity comparisons, animation updates, normals and particle attachments. Build draw submissions using source face offsets. Do not rewrite `currentModel.indices` or reorder attributes in place.

Opaque and cutout submissions retain depth writes. Blended submissions use ascending effective priority, descending camera-space depth, then original face index. Calculate depth from the current triangle centroid transformed into view space. Texture alpha mode 2 participates even when all corner opacities are one; face-alpha blending remains independent of texture cutout mode.

Use ordered geometry groups with the existing stable Three.js transparent render-list comparator, preserving ordering for other objects. The installed comparator already preserves insertion ties for a single object; explicitly test the resulting render submissions so material grouping cannot silently override face ordering. Coalesce only adjacent faces that are contiguous in the source index buffer and share material. Reuse existing materials and geometry resources.

Rejected: whole-mesh sorting, which leaves triangle order unchanged; one mesh and material per face, which adds objects and resource overhead; and an index rewrite, which breaks the current animation topology equality check and source face attachments unless a second identity mapping is maintained.

### Update in the existing render loop

Update camera matrices after controls and rebuild ordering before rendering when camera or geometry/pass membership changes. `setGeometry` invalidates ordering even when mesh resources are reused, because positions and animated alpha can change. Reuse scratch vectors and face metadata. Clear ordering with the mesh. The current resource loading and retry owner remains unchanged; no extra animation loop or retry mechanism is needed.

### Validate semantics before removing warnings

Compare a priority-sensitive fixture against the explicit client priority path, including 10 and 11. Verify equal-priority depth ordering separately as the web inspection behavior. Keep warnings explicit about remaining client lighting and renderer parity limits. Do not remove the general limitation text until evidence supports its narrower replacement.

## Risks / Trade-offs

- More transparent draw groups increase draw calls. Coalesce safe adjacent groups and measure a representative model; do not introduce a general batching layer.
- Triangle centroid ordering cannot solve intersecting surfaces. Document this limit without claiming per-pixel transparency correctness.
- The shared viewer serves both applications. Build both and retain backwards compatibility for geometry without priorities.
- Client constructor order differs from the explicit software priority pass. The supported contract matches priority precedence, not every material-key/backend detail; record which client path the comparison uses.

## Migration Plan

Ship additive API fields with optional frontend consumption. No cache rewrite or database migration is needed. Rollback restores the existing source-order submission while leaving additive fields harmless.

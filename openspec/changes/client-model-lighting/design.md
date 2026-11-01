# Design

## Context

See proposal.md for motivation and acceptance criteria. The decoder currently drops face shading types; prepareCacheModel expands corners with one neutral packed-HSL colour per face, while ModelScene adds hemisphere/directional lights and MeshStandardMaterial lighting. This loses source smooth-normal sharing and applies a modern material response.

The owning references are sibling GameClient Model_Sub3.method4551 for normals and method4549 for software shading, its packed-colour helpers, and the model construction callers. The research documents model-shading-alpha-and-order.md and scene-lighting-fog-and-atmosphere.md explain those boundaries. The latter records direction (-50,-60,-50) and default directional response values, whose fixed-point renderer conversions must be characterized from source.

## Goals / Non-Goals

Implement the isolated software-model lighting contract and keep backend-specific rendering concerns separate. Do not turn the model viewer into a scene renderer or introduce UI settings to resolve unknown formulas.

## Decisions

### Preserve semantic inputs in the existing pipeline

ModelGeometryDecoder owns face shading-mode decode for legacy and newer formats. Append optional semantic fields to geometry and DTOs; ModelEndpoints owns definition ambient/contrast projection and multipart concatenation. Item contrast is already multiplied by five in ItemTypeCodec, so do not repeat that conversion. Trace NPC LightModifier and its other lighting field to actual client opcodes before naming response fields. Raw models use a documented default model-lighting preset because they have no definition context.

Rejected: adjusting Three.js light intensity using definition modifiers. Client ambient changes packed lightness and contrast scales response coefficients, so neither is equivalent to a PBR setting.

### Compute colours before corner expansion

The shared renderer-neutral model preparation owns source-topology integer face normals, smooth normal sums with contribution counts, effective shading modes, and per-corner lit colours. Compute using transformed/scaled current pose coordinates in the client coordinate system, then apply the existing axis/winding conversion once. Retain face slots for suppressed faces so particles, priorities and face-skin labels remain aligned.

Reproduce source integer shifts, clamps, signed-alpha sentinel overrides and packed-colour helpers in a small pure module used by prepareCacheModel. Characterize both directional branches and textured intensity, including the existing material colour blend/brightness order. Use fixed client-derived scene inputs for inspection; record exact source constants and fixture outputs rather than relying on decompiler field names.

Rejected: standard normalized averaged vertex normals or generic Lambert lighting, which lose the client's normal contribution count and negative response. Server-side final colours would require a second lighting implementation for browser-posed animation.

### Render prepared lighting without double illumination

ModelScene owns GPU material application and cleanup. Prepared ordinary model colours use an unlit rendering path so Three.js does not light them again. Retain the existing material-effect behavior where it requires normals or procedural reflection, integrating prepared base lighting once and keeping effect 6 explicitly unlit. A preparation flag distinguishes these colours from renderer-neutral callers still expecting the existing material path. Keep transparency grouping and source indices unchanged.

The existing computed pose/geometry update is the sole animation update owner; no new timer, resource fetch, retry or worker is needed.

## Risks / Trade-offs

- Client palette/gamma and fixed-point rounding affect exact colours. Capture source-derived numeric fixtures before replacing material behavior.
- Per-frame CPU lighting adds preparation work. Reuse topology data where useful only after measuring representative animated models.
- Material effects currently use MeshStandardMaterial shader hooks. Test their shader compilation and output instead of replacing every material with MeshBasicMaterial indiscriminately.
- Isolated multipart normal accumulation cannot reproduce scene seam merging. Disclose this limitation rather than claiming full scene parity.

## Migration Plan

Use additive optional API fields and explicit prepared-lighting identification. Build both consumers of the shared viewer. After validation, rebuild the cache resource and restart the admin server through Aspire, preserving the user's running app. No cache or database migration is required.

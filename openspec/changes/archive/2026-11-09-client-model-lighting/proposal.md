# Proposal

## Why

Admin model previews use generic Three.js lights and PBR materials rather than the GameClient's model shading. This changes the appearance of textured walls, smooth surfaces and definition-specific brightness.

## What Changes

Implement client-compatible model lighting for standalone and definition-backed admin previews, using a deterministic client-derived inspection light preset.

- Preserve decoded face shading types and effective definition ambient/contrast inputs through the existing model API and assembly.
- Reproduce the client software model's integer normals, smooth/flat shading, packed-colour lighting and textured intensity rules in shared renderer-neutral geometry preparation.
- Recompute lighting for posed geometry and preserve transparent-face ordering, UVs and source face attachments.
- Render prepared colours without applying generic PBR illumination a second time. Keep explicitly unlit material effects unlit.

Acceptance criteria:

1. AC1: Legacy and newer fixtures preserve shading types; raw, item, NPC and object APIs expose the correct effective lighting inputs without multiplying decoded contrast twice.
2. AC2: Source-derived fixtures verify smooth/flat normals, positive/negative directional response, ambient/contrast, packed-colour and texture brightness clamps, and alpha sentinels.
3. AC3: Animation updates shading without changing source-face identity, transparency ordering, textures or particle attachments; unlit effects receive no directional light.
4. AC4: Affected tests and builds pass; a browser check of model #666 and representative smooth/flat fixtures establishes the supported appearance, with warnings for remaining limits.

Non-goals: map atmosphere integration, fog, point lights, shadows, bloom, scene-wide seam-normal merging, and complete parity across all client GPU backends. No new editor controls are required.

Stop conditions: do not substitute generic Lambert shading for uncharacterized client formulas, invent definition parameters, or claim full lighting parity without fixture evidence.

## Capabilities

### New Capabilities

- `client-model-lighting`: Client-compatible lighting and shading for isolated model inspection.

### Modified Capabilities

None. Published map-rendering requirements concern terrain, not isolated model previews.

## Impact

Reuse ModelGeometryDecoder, existing model assembly and DTOs, prepareCacheModel, and ModelScene. Update focused decoder/API/viewer tests and preview warnings. The shared viewer also serves the main web application. No new dependencies, services or persistence.

# Design

## Context

The cache model type is a stub. The GameClient and `docs/3d-rendering` provide the available reference for model, material and animation semantics. The cache service already owns cache access and authorization; Cache Studio owns navigation.

## Goals / Non-Goals

Provide shared model rendering for admin inspection. World rendering, cache writes and a published package are out of scope.

## Decisions

- Decode cache semantics in the cache layer, and expose render-focused, engine-neutral projections through the existing authenticated cache service. Sending raw cache files to the browser would duplicate decoding and tie consumers to cache format details.
- Use Three.js in a shared frontend component. It supplies camera and GPU lifecycle primitives; writing a WebGL renderer would create unnecessary infrastructure. The component owns renderer resources and disposes them on replacement and destruction. The admin page owns selection and request state, including retry; the component has no authentication dependency.
- Use definition context for animation associations and appearance overrides. A raw model has no assumed animation. Preserve source semantics and explicitly report unsupported formats or effects rather than presenting guessed behavior as parity.
- Reuse existing cache reads, authorization and definition codecs. No worker, persistent state or additional retry coordinator is required.

## Risks / Trade-offs

- Client format and effects are incompletely documented → compare representative cache fixtures with the GameClient and report unverified features.
- Model assets can be large → dispose replaced GPU resources and keep API responses scoped to requested assets.

## Migration Plan

Deploy cache service and admin together; existing APIs remain unchanged. Roll back both if needed. No data migration is required.

## Texture alpha handling

Decode material alpha mode from index 26 archive 0 using the client column layout. The protected PNG endpoint normalizes opaque and black-key cutout pixels, preserves source alpha for mode 2, and returns the raw mode in X-Texture-Alpha-Mode. The shared viewer receives image and alpha mode independently of HTTP, uses depth writes for opaque/cutout surfaces and blending for mode 2. Per-face transparency remains independent. Client lighting remains explicitly unsupported.

## Model texture projections

The cache decoder supplies per-face-corner UVs for explicit triangles, client default mapping, cylindrical, box and spherical projections. Projection bounds, orientation matrices, signed offsets, direction and seam correction follow GameClient Model.method4441/4442/4443/4444/4459/4492. UVs are calculated before definition transforms and remain fixed during animation. Angular only reorders corners with face winding; it does not duplicate projection rules. Existing mapping-vertex metadata is retained in the API.

## Material color preview

The same material table decoder supplies alpha, color-mix weight and brightness. The PNG response carries these appearance fields in headers; admin passes them as render data to the shared cache-geometry adapter. Textured face colors are mixed toward grayscale in sRGB, then brightness is applied, following Model_Sub3.method4552 with a neutral shade reference. Actual client lighting, palette gamma and advanced material effects remain unverified and are not claimed as parity.

## Definition appearance assembly

Item inventory previews use the same assembly path as NPCs and objects: normalize model coordinate units, apply definition scales, then recolor and retexture in pair order. IDs stored as signed shorts are compared as unsigned 16-bit values, with texture 65535 retaining the no-texture sentinel. UVs stay unchanged. Raw model responses do not apply definition appearance. Assets without any nonnegative skin groups keep vertexSkins null. Palette-index item recolors are reported as unsupported.

## Animation coordinate fidelity

Frame translation and pivot offsets are already in the normalized client coordinate units. CoordinateScale is 1 for assembled or modern raw geometry and 0.25 for pre-version-13 raw geometry. Posing follows Model_Sub3.method4470: 4-bit intermediate precision, integer sine/cosine coefficients, rotation rounding and final (value + 7) >> 4 reduction. NPC scale is returned as a renderer-neutral scale factor and applied after posing, as in NPCDefinition. Item/object assembly uses signed arithmetic-shift scale rounding. Tweening remains explicitly unsupported in the UI.

# Proposal

## Why

The admin model viewer and map editor omit some revision-742 model behavior. An animation can fail on an unsupported transform, and an inverted object can render on the map while its standalone preview returns 422.

## What Changes

- Inventory the production cache before porting animation transforms, tweening, particle forces and emitter settings, inverted appearances, and material effect 3.
- Use the revision-742 client as the reference. Verify each phase before advancing. Skip features with measured zero use, and record parity blockers in the design while retaining the visible limitation.
- Verify with client-derived regression values, real admin assets, shader link checks and a map animation frame-cost measurement.
- Model and animation editing, changes to the client, and changes to terrain, water and existing material effects are out of scope.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-model-viewing` covers playback, transform warnings and inverted object previews.
- `model-particles` covers posed effectors and verified emitter settings.
- `model-material-effects` covers the effect 3 preview or its scene-specific fallback.
- `model-contracts` covers additive model attachment data.
- `web-3d-map-rendering` covers shared animation sampling and placement mirroring.

## Impact

Production cache decoders and model endpoints, admin model DTOs, shared model rendering, and map object animation buffers. Reuse the existing scene clock, lighting path, material lifecycle and object assembly.

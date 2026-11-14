# Why

The Models page only provides a lookup and an unstructured preview. Recreate the supplied model-editor reference within the existing Angular admin application.

## What Changes

One goal: provide a model inspection workspace with a large interactive viewport, inspection tabs, texture strip, and properties/skin-group sidebar. Reuse the existing cache preview, requests, texture thumbnails, animation playback, shared Three.js renderer, Material components, fonts, and shell.

## Non-goals

No backend endpoints, model persistence, duplication, texture upload, definition recolor editing, new routes, global shell redesign, or invented bone hierarchy. Raw model endpoints are read-only. Save and duplicate are disabled with an explanation. Transforms affect the preview only. Existing cache-type navigation remains in place.

## Acceptance criteria

- AC1: Existing validated model lookup/reload works; loaded models use the reference's viewport/sidebar/texture composition, current Material theme, and responsive stacking.
- AC2: Model, textures, recolors, retextures, animations and metadata tabs expose actual data or honest empty/unsupported states; existing playback and retries continue working.
- AC3: Grid, wireframe, camera focus, preview scale/rotation/offset and reset controls work without modifying cache data. Properties and skin groups derive from the loaded DTO, never mock constants.
- AC4: Embedded previews retain their current interface and behavior. New GPU helpers are disposed. Regression tests, admin build, lint, coverage and browser comparison are recorded.

## Stop conditions

Model writes or new backend responsibilities require a separate change. Do not substitute generated stone models or fixed screenshot imagery for live cache content.

## Capabilities

### New Capabilities
- `admin-model-workspace`: Reference-based model inspection in Cache Studio.

## Impact

Admin Models page and existing model preview; additive display inputs in the shared renderer. No new dependency.

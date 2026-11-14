## Why

The admin map editor skips local lights stored in region terrain archives. This leaves areas lit by torches and other local sources looking flat.

## What Changes

- Preview cached point lights on the terrain and objects of loaded regions.
- Add a Point lights toggle under View > Lighting.
- Use steady light for the preview. Light authoring, flicker and shadow casting are out of scope.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering` gains a point light preview.

## Impact

The cache decoder and existing render response expose the light data. The admin scene draws it using the existing materials and camera. The change is ready when focused decoder, map scene and viewport checks pass and the admin app builds.

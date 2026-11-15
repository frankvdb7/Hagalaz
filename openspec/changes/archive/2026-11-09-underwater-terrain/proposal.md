## Why

The client draws a second terrain layer beneath water from separate archives and fogs it by depth. Overlay water parameters only take effect there, so the map preview cannot use them without that layer.

## What Changes

- Decode the `um` and `ul` archives and expose an optional underwater plane and objects on the map render API.
- Draw the layer in the map preview, with visibility toggles for the layer and the water surface, and fog its floors from each tile's surface overlay.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering`: render the underwater terrain layer with depth fog.

## Impact

Cache terrain reading, the map render API and the Angular map scene. Fog on underwater objects, the client's water-surface depth inputs and dynamic region parts remain out of scope.

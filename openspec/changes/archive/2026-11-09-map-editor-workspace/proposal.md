## Why

The map editor keeps its scene list and asset browser in secondary tabs and a tray, so browsing a region competes with the viewport.

## What Changes

- Arrange the editor in three columns: scene tree and asset list, viewport, and a two-tab inspector.
- Add per-group and per-placement visibility, a tile/world position switch, read-only object properties and a viewport image save.
- Show move, rotate and scale handles on the selected object in the viewport.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering`: organise the map editor around a scene tree with visibility controls.

## Impact

Angular map editor page, viewport toolbar, scene filtering and the shared scene's image capture. No API changes.

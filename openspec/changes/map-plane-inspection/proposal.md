# Map plane inspection

## Why

Multi-story buildings are hard to inspect in the admin map editor. Only plane 0 is drawn when a map opens, and inspecting an upper floor requires toggling visibility. Roofs and upper floors block clicks on the floors below. Hiding a plane only to select an object underneath it combines visibility with inspection focus.

## What Changes

- Draw planes 0 to 3, with their terrain and objects, together when a map opens.
- Add an explicit active plane. It sets the drop target for new objects, and the plane that the grid marks. It never changes visibility, geometry, or the camera.
- Let each plane be drawn see-through. See-through planes do not block picks, so floors below them can be selected. This changes only a shader uniform and pick filtering, and never changes cache data, scene geometry, or materials.
- Add stacked plane slabs below the orientation gizmo. Each click on a slab moves that plane from visible to see-through to hidden and back, and the slabs mark the active plane. The Layers menu chooses the active plane, and a single "Show all planes" action restores the combined view.
- When a click selects upper-floor geometry that covers the active plane, explain the overlap and direct the administrator to make that plane see-through.
- Add a camera action that centres on the active plane's geometry.
- Leave out persisted preferences, because the admin app has no preferences mechanism. Leave out roof occlusion, collision simulation, placement mutation, and cache writes.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering` loads all four planes by default and gains active-plane inspection, per-plane see-through, and lower-plane picking.

## Impact

The shared `ModelScene` and `MapLightMaterial` gain plane-focus uniforms, plane-aware picking, an active-plane grid, and plane camera focus. The admin map viewport and Maps detail page own the new controls and state. The initial load requests assets for all four planes.

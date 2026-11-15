# Proposal

## Why

The admin map editor approximates five water effects with procedural shading. The revision-742 client defines the noise, animation, reflections and shoreline behavior for those effects.

## What changes

- Measure effects 2, 4, 5, 8 and 9 in the local cache and identify a live test region for each floor effect present.
- Port the client water data and shader paths in phase order. Reuse the viewer's animation, terrain cache and resource lifecycle.
- Preserve underwater visibility and depth fog. Blend water floor edges where the existing terrain path can express the client result without a second animation system or substantial shader growth.
- Verify each phase before proceeding. Stop on an unresolved client parity mismatch. Leave the client side-by-side check open.

## Capabilities

### Modified capabilities

- `web-3d-map-rendering` updates underwater terrain, the region environment and floor blending.
- `model-material-effects` records the water effect programs and their fallbacks.

## Impact

The shared model viewer and admin map geometry own the render changes. Cache reads supply evidence and existing inputs. Terrain slope lighting, overlay spreading and the game client are outside scope. API changes are limited to necessary depth or atmosphere inputs.

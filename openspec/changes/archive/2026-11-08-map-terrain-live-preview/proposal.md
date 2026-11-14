# Proposal

## Why

Painting terrain gives no feedback during a drag, and a stroke then takes about 600 ms to appear after release. Measured on region 12850: terrain rebuild 195 ms, object rebuild 184 ms, upload 162 ms, next frame 61 ms. Terrain is only 53,000 of the scene's 1.59 million vertices, but every stroke rebuilds and re-uploads all of them.

## What Changes

Goal: terrain brushes show their effect while dragging, and releasing a stroke no longer rebuilds or re-uploads geometry the stroke did not change.

In scope:

- A lightweight stroke overlay that draws only the brushed tiles, at their new height or with their new floor, while the pointer moves.
- Drawing terrain and objects as separate meshes, so a stroke re-uploads only the terrain.
- Keeping built object geometry across paint strokes, and re-fitting only the objects on changed ground after a height stroke.

Non-goals:

- Moving objects under the overlay during a drag; they move on release.
- Web workers or off-main-thread building.
- Faster initial region load, or faster object edits such as move and rotate.
- Any change to what is saved, or to the brush tools and their rules.

Acceptance criteria, measured on region 12850 with the method in the design:

- Each pointer sample updates the overlay in under 16 ms at the largest brush.
- Releasing a paint stroke takes at most 250 ms until the ground is drawn.
- Releasing a height stroke takes at most 350 ms until ground and objects are drawn.
- The finished scene looks the same as before this change, including water and translucent surfaces.
- Picking, selection bounds, object drop, image capture, wireframe and undo behave as before.

Stop conditions: stop and report if splitting the meshes visibly changes how translucent surfaces overlap and cannot be matched, or if a release target cannot be met without off-main-thread building.

Reused mechanisms: `applyBrushStroke`, `applyTerrainEdits`, `MapScenePreparation`, the existing material and texture handling in `ModelScene`, and the brush outline's input path. No new dependency or worker.

This builds on `map-terrain-editing`.

## Capabilities

### New Capabilities

- `map-editor-live-painting`: how quickly and how visibly terrain brushes respond in the map editor.

### Modified Capabilities

None.

## Impact

- `Hagalaz.Web.App/shared/model-viewer/model-scene.ts`: a second mesh and a stroke overlay. The class is also used by the model preview, which must keep working unchanged.
- `Hagalaz.Web.App/src-admin`: scene geometry, viewport and map detail page.
- No backend or cache format change.

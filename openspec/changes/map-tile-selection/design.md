# Design

## Context

The viewport already distinguishes clicks from drags and raycasts terrain for hover and painting. The map page owns object and scene-tree selection, terrain previews, and Inspector content.

## Goals / Non-Goals

Add a persistent tile selection to the existing Inspector. Leave terrain painting, object transforms, and cache persistence under their current owners.

## Decisions

- In Select mode, use the existing placement pick first. When it misses, raycast the terrain at the click position. Using the last hover sample would select the wrong tile after a fast pointer move or when clicking before hover sampling finishes.
- Keep the selected tile in the map page as a region ID, local tile coordinates, and plane. The page coordinates selection with objects and the scene tree. The viewport handles click and Escape input. No retry is needed.
- Resolve Inspector values from the current terrain preview. A saved copy of the tile values would become stale after an edit or undo. Clear the selection when its region leaves the loaded set or its plane is hidden.
- Use the existing one-tile ground outline and viewport line renderer. The outline follows the selected tile's preview heights. Brush and drag previews take priority while active.

## Risks / Trade-offs

Object-first picking preserves the existing object workflow. Terrain covered by an object can be inspected after hiding that object with the existing visibility controls.

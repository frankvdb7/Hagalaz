# Map tile selection

## Why

The map viewport shows coordinates on hover. Inspecting terrain data requires a selection that stays in the Inspector when the pointer moves.

## What Changes

- Clicking visible terrain with the Select tool selects the tile and opens its details in the existing Inspector. Clicks on rendered objects continue to select those objects.
- Show the selected tile's region, world and local coordinates, plane, height, floor IDs, and terrain flags. Show pending terrain edits in the same details.
- Outline the selected tile in the viewport and keep its selection while hovering elsewhere. Escape, empty-space clicks, and another selection clear or replace it.
- Scope ends at read-only inspection of terrain already loaded by the editor. Leave terrain painting and cache writes outside this change. Stop and revisit scope if inspection requires new cache data or APIs.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering` gains persistent terrain tile selection in the admin map Inspector.

## Impact

The Angular admin viewport reuses its terrain raycast. The map detail page resolves the hit against its loaded terrain preview and renders the details in its existing Inspector tab.

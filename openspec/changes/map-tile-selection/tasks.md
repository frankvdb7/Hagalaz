# Tasks

## 1. Tile selection

- [x] 1.1 Reuse the viewport click and terrain hit tests to select tiles in Select mode, preserving object priority and painting behavior.
- [x] 1.2 Manage tile selection in the map page. Clear it on Escape, empty-space clicks, another selection, or when the tile is no longer visible.

## 2. Inspector and verification

- [x] 2.1 Show the selected tile's coordinates, plane, height, floors, and flags from the current terrain preview in the existing Inspector.
- [x] 2.2 Add and run focused viewport and Inspector regression tests covering selection priority, persistence across hover, draft values, and clearing. Run the admin build and strict OpenSpec validation.
- [x] 2.3 Draw the selected tile outline using the terrain preview. Verify that hovering elsewhere leaves it in place and clearing selection removes it.

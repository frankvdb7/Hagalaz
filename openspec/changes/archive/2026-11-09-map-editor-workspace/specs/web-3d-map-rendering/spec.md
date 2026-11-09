## ADDED Requirements

### Requirement: Map editor scene tree

The map editor SHALL list a region's terrain and placements in a tree grouped by object shape, and SHALL let the user hide terrain, groups or single placements without reloading the region.

#### Scenario: Hide a group

- **WHEN** the user hides a group in the scene tree
- **THEN** its placements leave the viewport and its rows remain available to show again

#### Scenario: Select from the map

- **WHEN** the user selects a placement in the viewport
- **THEN** the tree opens its group and marks its row, including when the row is beyond the listed limit

### Requirement: Viewport object handles

The map editor SHALL show handles on the selected object for the active Move, Rotate or Scale tool. Handles SHALL honour placement limits: moves snap to the tile grid inside the region and to planes 0 to 3 along Z, rotation uses quarter turns around the vertical axis, and scale changes the whole object in 0.25 steps between 0.25 and 4.

#### Scenario: Drag a handle

- **WHEN** the user drags a handle
- **THEN** the object follows in the viewport without orbiting the camera
- **AND** releasing records one undoable edit

#### Scenario: Drag ends where it began

- **WHEN** a drag ends without crossing a step
- **THEN** no edit is recorded

#### Scenario: Select tool

- **WHEN** the Select or Terrain tool is active
- **THEN** no handles are shown

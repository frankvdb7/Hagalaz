## ADDED Requirements

### Requirement: Pointer depth controls map navigation

The perspective map editor SHALL account for the depth under the pointer during wheel zoom and right-button pan.

#### Scenario: Navigate a near or far tile

- **GIVEN** the pointer intersects the map's ground reference plane
- **WHEN** the operator uses the wheel or drags with the right mouse button
- **THEN** wheel zoom scales by the same proportion at either depth, and a right drag moves the reference point with the pointer.

#### Scenario: The pointer has no usable ground depth

- **WHEN** the pointer looks away from the ground or its intersection is too far away
- **THEN** the existing camera controls handle the gesture.

## ADDED Requirements

### Requirement: Multi-tile interaction overlap is two-dimensional
`CanDecorationInteract` and `CanDoorInteract` SHALL treat a target as overlapping a multi-tile mover only when both target coordinates are within that mover's inclusive footprint.

#### Scenario: Target overlaps only one footprint axis
- **WHEN** a size-2 or size-3 mover has a target whose X is inside its footprint but Y is below or above it, or whose Y is inside its footprint but X is outside it
- **THEN** the method SHALL not report automatic overlap reach

#### Scenario: Target overlaps both footprint axes
- **WHEN** a target lies within both X and Y bounds of a size-2 or size-3 mover
- **THEN** the method SHALL report overlap reach before evaluating approach-side collision

### Requirement: Large door reach uses the approached collision side
`CanDoorInteract` SHALL evaluate size-2 and size-3 doorway approaches for shapes 0, 2, and 9 using the collision tile on the approached mover side for each supported rotation.

#### Scenario: Door approach is blocked on its actual side
- **WHEN** the collision flag that blocks a supported size-2 or size-3 door approach is placed on the actual approach-side tile
- **THEN** the method SHALL reject interaction reach

#### Scenario: Unrelated door collision is ignored
- **WHEN** the actual approach-side tile is clear and an unrelated tile is blocked
- **THEN** the method SHALL preserve interaction reach

#### Scenario: Rotation-zero door lookup uses current Y
- **WHEN** a size-2 or size-3 mover approaches the relevant shape-0 rotation-zero side with distinct X and Y coordinates
- **THEN** the collision lookup SHALL use the target X and mover current Y coordinates

### Requirement: Large decoration reach uses the approached collision side
`CanDecorationInteract` SHALL evaluate size-2 and size-3 decoration approaches for shapes 6, 7, and 8 using the collision tile on the approached mover side for each relevant rotation.

#### Scenario: Decoration approach is blocked on its actual side
- **WHEN** the collision flag that blocks a supported size-2 or size-3 decoration approach is placed on the actual approach-side tile
- **THEN** the method SHALL reject interaction reach

#### Scenario: Unrelated decoration collision is ignored
- **WHEN** the actual approach-side tile is clear and an unrelated tile is blocked
- **THEN** the method SHALL preserve interaction reach

#### Scenario: Rotation-zero decoration lookup uses the west-side Y
- **WHEN** a size-2 or size-3 mover approaches the relevant shape-6 or shape-7 rotation-zero west side with distinct X and Y coordinates
- **THEN** the collision lookup SHALL use the mover west-side X and target Y coordinates

### Requirement: Single-tile interaction behavior is preserved
The system SHALL preserve the existing `selfSize == 1` interaction-reach behavior for the supported door and decoration shapes.

#### Scenario: Existing single-tile approach remains valid
- **WHEN** a size-1 mover approaches a currently valid supported door or decoration side with clear collision
- **THEN** the method SHALL continue to report interaction reach

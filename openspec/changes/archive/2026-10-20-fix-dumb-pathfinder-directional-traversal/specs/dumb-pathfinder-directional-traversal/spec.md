## ADDED Requirements

### Requirement: Single-tile traversal preserves compass geometry
The system SHALL produce the adjacent compass-neighbor point requested by each of the eight `DumbPathFinder` size-one directions when the relevant traversal tiles are clear. A traversal SHALL fail when its matching directional collision composite blocks a required tile and SHALL not fail solely because the opposite directional composite is present on the diagonal destination tile.

#### Scenario: Each clear size-one compass direction reaches its requested neighbor
- **WHEN** a size-one mover starts one tile from its target in any compass direction and all traversal tiles are clear
- **THEN** the resulting path SHALL be successful and contain exactly that target coordinate

#### Scenario: Matching size-one directional collision blocks traversal
- **WHEN** a matching directional collision composite is placed on a required traversal tile for a size-one compass direction
- **THEN** the resulting path SHALL be unsuccessful and SHALL not add the target coordinate

#### Scenario: Opposite diagonal collision does not control northeast traversal
- **WHEN** a size-one mover traverses northeast and the northeast destination has only the northwest-exclusive collision bit
- **THEN** the resulting path SHALL remain successful

#### Scenario: Northeast collision blocks northeast traversal
- **WHEN** a size-one mover traverses northeast and the northeast destination has the northeast-exclusive collision bit
- **THEN** the resulting path SHALL be unsuccessful

### Requirement: Size-two traversal advances from the validated footprint
The system SHALL advance a size-two mover's anchor by the requested compass delta after all newly occupied footprint tiles pass their direction-specific collision checks. In particular, southwest traversal SHALL advance its anchor to `(x - 1, y - 1)`.

#### Scenario: Clear size-two movement reaches each requested neighbor
- **WHEN** a size-two mover starts one tile from its target in any compass direction and the newly occupied footprint is clear
- **THEN** the resulting path SHALL be successful and contain exactly that target anchor

#### Scenario: A newly occupied size-two footprint tile blocks traversal
- **WHEN** a direction-specific collision composite is placed on a newly occupied footprint tile for a size-two movement direction
- **THEN** the resulting path SHALL be unsuccessful

#### Scenario: Clear size-two southwest advances southwest
- **WHEN** a size-two mover at `(x, y)` traverses southwest with a clear incoming footprint
- **THEN** the resulting anchor SHALL be `(x - 1, y - 1)`

### Requirement: Existing variable-size southwest behavior is preserved
The system SHALL preserve the existing southwest anchor update for movers with size three or larger.

#### Scenario: Clear variable-size southwest advances southwest
- **WHEN** a size-three-or-larger mover at `(x, y)` traverses southwest with a clear incoming footprint
- **THEN** the resulting anchor SHALL be `(x - 1, y - 1)`

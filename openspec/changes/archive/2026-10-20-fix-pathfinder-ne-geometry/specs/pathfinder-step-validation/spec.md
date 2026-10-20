## ADDED Requirements

### Requirement: North-east footprint validation matches client geometry

The shared step validator MUST validate the newly occupied north-east footprint using the client coordinate and directional-mask geometry for size-2 and size-3-or-larger creatures.

#### Scenario: Size-2 north-east checks the new east-side tile
- **GIVEN** a size-2 creature at `(fromX, fromY)`
- **WHEN** it moves one tile north-east and `(fromX + 2, fromY + 1)` is blocked
- **THEN** the step is rejected
- **AND** an overlapping tile at `(fromX + 1, fromY + 1)` is not used as a substitute for that incoming east edge

#### Scenario: Variable-size north-east checks the top edge with the south mask
- **GIVEN** a size-3 or size-4 creature moving north-east
- **WHEN** an incoming top-edge tile contains `WallSouthEast`
- **THEN** the step is rejected by the `CheckSouthVariable` composite

#### Scenario: Variable-size north-east checks the right edge with the west mask
- **GIVEN** a size-3 or size-4 creature moving north-east
- **WHEN** an incoming right-edge tile contains `WallNorthEast`
- **THEN** the step is rejected by the `CheckWestVariable` composite

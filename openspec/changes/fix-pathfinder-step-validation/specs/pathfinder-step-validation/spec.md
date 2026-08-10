## ADDED Requirements

### Requirement: Creature footprint steps are collision validated

The shared step validator MUST validate every newly occupied footprint edge and corner for size-1, size-2, and size-3-or-larger creatures using the existing collision flags.

#### Scenario: Size-2 movement in every direction
- **WHEN** a size-2 creature attempts any of the eight unit directions
- **THEN** every newly occupied footprint tile required by that direction is checked and a blocked tile makes the step fail

#### Scenario: Size-2 southeast movement
- **WHEN** a size-2 creature moves southeast and a newly occupied southeast footprint tile is blocked
- **THEN** the step is rejected rather than accepted by an unmatched-direction fallback

#### Scenario: Size-2 northwest movement
- **WHEN** a size-2 creature moves northwest and only the distinct upper exposed edge tile is blocked
- **THEN** the step is rejected

#### Scenario: Variable-size cardinal movement
- **WHEN** a size-3 or size-4 creature moves cardinally and an interior tile of the incoming edge is blocked
- **THEN** the step is rejected for each cardinal direction

#### Scenario: Variable-size diagonal movement
- **WHEN** a size-3 or larger creature moves diagonally and an incoming edge or corner tile is blocked
- **THEN** the step is rejected using the same footprint geometry as the client routefinder

### Requirement: Unsupported step offsets fail closed

The shared scalar step validator MUST return false for zero offsets and any offset whose absolute X or Y component is greater than one. Supported unit directions MUST retain their existing size-1 behavior.

#### Scenario: Non-unit offset is supplied
- **WHEN** a caller supplies a multi-tile X, Y, or diagonal offset
- **THEN** the validator returns false without treating the movement as collision-free

#### Scenario: Zero offset is supplied
- **WHEN** a caller supplies no movement offset
- **THEN** the validator returns false

#### Scenario: Existing size-1 unit movement is supplied
- **WHEN** a size-1 creature supplies any supported unit direction
- **THEN** the validator uses the existing directional collision rules

### Requirement: Distance checks use unit validation

The distance-based shared check MUST validate the path between sampled locations through unit steps and MUST NOT pass unsupported offsets to the scalar validator.

#### Scenario: Distance check crosses a blocked unit step
- **WHEN** a distance-based check traverses a unit step whose collision is blocked
- **THEN** the distance check returns false

#### Scenario: Distance check has walkable unit steps
- **WHEN** every unit step in the checked distance is walkable
- **THEN** the distance check returns true

### Requirement: Runtime movement revalidates compressed waypoints

`Movement.Tick` MUST validate each one-tile movement it applies toward a queued waypoint, stop before the first blocked step, and retain the queued waypoint when movement stops.

#### Scenario: Collision appears on a long queued waypoint
- **WHEN** a waypoint more than one tile away was queued and an intervening tile becomes blocked before `Tick`
- **THEN** movement does not skip the blocked tile and the creature remains at the last valid location

#### Scenario: Multiple valid movement units are available
- **WHEN** run or warp movement has multiple movement units available toward a compressed waypoint
- **THEN** each applied unit is independently validated before the location is committed

#### Scenario: Existing valid waypoint movement is preserved
- **WHEN** a walk, run, or diagonal movement has a walkable queued waypoint
- **THEN** movement advances by the existing one-tile or two-tile budget without losing the queued waypoint

## ADDED Requirements

### Requirement: Collision writers preserve object geometry

`MapRegion` MUST write low wall, solid-blocked, and range wall collision layers at the positions and directions represented by each supported object shape and rotation.

#### Scenario: Diagonal and corner walls use client-parity directions
- **WHEN** a clipped, solid, non-gateway `WallCornerDiagonal` or `WallCorner` is written at rotations 0 through 3
- **THEN** its origin/opposite pairs MUST be north-west/south-east, north-east/south-west, south-east/north-west, and south-west/north-east respectively in every applicable layer

#### Scenario: Unfinished wall rotation zero uses west and north range directions
- **WHEN** a clipped, non-gateway `UnfinishedWall` at rotation 0 is written
- **THEN** its origin tile MUST include `WallAllowRangeWest` and `WallAllowRangeNorth`

#### Scenario: Standard objects use rotated footprints
- **WHEN** a clipped standard object is written at rotations 1 or 3
- **THEN** its collision footprint MUST use the definition's Y size for X extent and X size for Y extent

#### Scenario: Writer gates preserve layer meaning
- **WHEN** a supported wall or standard object is non-solid or a gateway
- **THEN** non-solid objects MUST omit only the solid-blocked layer and gateways MUST omit only the range layer

### Requirement: Collision writer removal is reversible

For an object that is eligible to write collision, `MapRegion` MUST remove exactly the flags that it wrote without clearing unrelated collision bits.

#### Scenario: Supported object is removed after being written
- **WHEN** a floor decoration, standard object, or supported wall shape is flagged and then unflagged
- **THEN** every affected tile MUST return to its pre-flag collision state

#### Scenario: Unrelated collision bit exists on an affected tile
- **WHEN** an affected tile contains a collision bit not owned by the object before the object is flagged
- **THEN** flagging and unflagging the object MUST preserve that bit

### Requirement: Collision dispatch selects the object layer writer

`MapRegion` MUST route wall, standard-object, and floor-decoration shapes to their corresponding collision writers and MUST leave wall decorations without collision mutations.

#### Scenario: Object is flagged through the public dispatcher
- **WHEN** a wall, standard object, floor decoration, or wall decoration is passed to `FlagCollision`
- **THEN** only the mapped writer's collision behavior MUST be applied

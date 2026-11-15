# Spec Delta

## ADDED Requirements

### Requirement: Walkability overlay shows the server's static collision

The walkability overlay SHALL derive its collision markers from one collision value per tile and plane that equals the game server's static collision for the same terrain flags and object placements, covering floor blocks, floor decorations, object tiles, solid objects, walls in eight directions and projectile-blocking walls.

#### Scenario: Agreement with the game server

- **GIVEN** pinned region 12850's terrain and landscape
- **WHEN** the editor's collision values are compared with the game server's region collision after loading the same data
- **THEN** every tile on every plane has the same value, and the comparison runs in the normal test run without a live cache or game process

#### Scenario: Unsaved edits

- **GIVEN** the overlay is on
- **WHEN** the user completes or undoes a flag stroke or moves, adds or removes a placement
- **THEN** the markers match the edited data without a save

### Requirement: Object collision uses the placed definition

Object collision in the walkability overlay MUST use the placed object definition's size, clip type, solid and gateway values, independent of whether the object has a model.

#### Scenario: Object without a model

- **GIVEN** a placed object whose definition blocks movement but has no model for its shape
- **WHEN** the overlay is on
- **THEN** its tiles are marked as blocked by an object

#### Scenario: Rotated footprint uses definition sizes

- **GIVEN** a blocking object whose definition is two tiles by one, placed with rotation 1
- **WHEN** the overlay is on
- **THEN** the one-by-two area it covers after rotation is marked

#### Scenario: Collision data unavailable

- **WHEN** an object's collision fields cannot be read
- **THEN** the object is left unmarked, the rest of the overlay is still drawn, and the option's label states that some object collision is missing

### Requirement: Walls mark both tiles they separate

The walkability overlay SHALL mark a wall or wall corner on its own tile and on the neighbouring tile whose movement it blocks, including a neighbour in another loaded region.

#### Scenario: Straight wall

- **GIVEN** a blocking straight wall with rotation 0 on a tile
- **WHEN** the overlay is on
- **THEN** the tile's west edge and the east edge of the tile to its west are marked

#### Scenario: Wall on a region border

- **GIVEN** a blocking wall on the west edge of a region whose western neighbour is loaded
- **WHEN** the overlay is on
- **THEN** the neighbour's facing edge is marked

#### Scenario: Neighbour not loaded

- **GIVEN** a blocking wall on a region edge whose neighbour is not loaded
- **WHEN** the overlay is on
- **THEN** only the wall's own tile is marked and nothing fails

### Requirement: Walkability overlay distinguishes movement and projectile clipping

The walkability overlay SHALL mark movement and projectile clipping together and label their markers in one legend.

#### Scenario: Walkable floor

- **GIVEN** a visible floor tile without a floor, floor-decoration or object tile block
- **WHEN** the user turns the overlay on
- **THEN** the floor tile has a green marker and any wall edges keep their own markers

#### Scenario: Movement and projectile blockers

- **GIVEN** a solid object, a non-solid blocking object, a solid wall and a non-solid blocking wall
- **WHEN** the user turns the overlay on
- **THEN** the non-solid blockers are orange, the solid object is pink and the solid wall is cyan

#### Scenario: Toggling the overlay

- **GIVEN** a loaded region
- **WHEN** the user turns the overlay on or off
- **THEN** terrain and object geometry are not rebuilt or re-uploaded

### Requirement: Hovered tile collision is readable

While the walkability overlay is on, the map viewport SHALL show, for the tile under the pointer, its world coordinates, plane, collision plane and the names of its collision bits.

#### Scenario: Hovering a blocked tile

- **GIVEN** the overlay is on
- **WHEN** the pointer is over a tile with a floor block and a north wall
- **THEN** the readout names both, with the tile's coordinates and plane

#### Scenario: Overlay off

- **WHEN** the overlay is off
- **THEN** no collision readout is shown

### Requirement: Object collision fields can be read in batches

The cache service SHALL return the size, clip type, solid and gateway values for a batch of object definition IDs, with an independent outcome per ID, and reject an empty or oversized batch with a validation error.

#### Scenario: Mixed batch

- **WHEN** a batch holds a valid ID and an ID outside the definition archive
- **THEN** the valid ID's fields are returned and the other is reported as not found, in one response

#### Scenario: Oversized batch

- **WHEN** a batch holds more IDs than the documented limit, or none
- **THEN** the response is a validation error and nothing is read

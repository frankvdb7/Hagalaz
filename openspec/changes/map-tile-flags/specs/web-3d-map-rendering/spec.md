# Spec Delta

## ADDED Requirements

### Requirement: Tile flags have named meanings

The map editor SHALL interpret a terrain tile's flag value as the named facts blocked, bridge, roof, drawn on the ground plane and hidden on the minimap, matching the revision-742 client.

#### Scenario: Combined flags

- **WHEN** a tile's flag value is 5
- **THEN** the tile is reported as blocked and roofed, and not as a bridge

#### Scenario: No flags

- **WHEN** a tile's flag value is 0
- **THEN** no fact is reported for the tile

### Requirement: Walkability overlay

The map viewport SHALL offer a walkability view option, off by default and listed with the other view options, that marks each flagged tile on its plane in a colour per flag that is distinct from floor colours.

#### Scenario: Off by default

- **WHEN** a region is opened
- **THEN** no walkability markers are drawn and the option is unchecked

#### Scenario: Turning the overlay on

- **GIVEN** a region with blocked, bridge and roof tiles
- **WHEN** the user turns the option on
- **THEN** each of those tiles is marked in its flag's colour at ground height, and unflagged tiles are unmarked

#### Scenario: Hidden planes

- **GIVEN** the overlay is on
- **WHEN** the user hides a plane
- **THEN** that plane's markers are not drawn

### Requirement: Overlay blocking follows the game's collision planes

The walkability overlay MUST determine blocking as the client and game server do: a blocked tile blocks its own plane, or the plane below when the same column is a bridge on plane 1, and blocks nothing when that would be below plane 0.

#### Scenario: Blocked tile on a bridge column

- **GIVEN** a blocked tile on plane 1 whose column is a bridge on plane 1
- **WHEN** the overlay is on
- **THEN** the tile is marked as blocked, and counts as blocking plane 0

#### Scenario: Blocked ground under a bridge

- **GIVEN** a blocked tile on plane 0 whose column is a bridge on plane 1
- **WHEN** the overlay is on
- **THEN** the tile is shown as having no effect

#### Scenario: Agreement with the game server

- **GIVEN** pinned region 12850
- **WHEN** the editor's blocked tiles per collision plane are compared with the game server's terrain blocking for the same archive
- **THEN** the two sets are equal, and the comparison runs in the normal test run without a live cache or game process

### Requirement: Overlay includes object collision

The walkability overlay SHALL mark tiles and tile edges blocked by placed objects, using the game server's footprint, rotation and bridge rules, or state in the option's label which object collision is not shown.

#### Scenario: Rotated object footprint

- **GIVEN** a blocking object two tiles by one placed with rotation 1
- **WHEN** the overlay is on
- **THEN** the two tiles it covers after rotation are marked as blocked by an object

#### Scenario: Non-blocking object

- **GIVEN** an object whose definition does not block movement
- **WHEN** the overlay is on
- **THEN** its tiles are not marked

#### Scenario: Collision not shown

- **WHEN** the overlay cannot show a kind of object collision
- **THEN** the option's label names what is not shown

### Requirement: Overlay updates without rebuilding terrain

Toggling the walkability overlay MUST NOT rebuild or re-upload terrain or object geometry, and the overlay MUST reflect unsaved terrain edits.

#### Scenario: Toggling

- **GIVEN** a loaded region
- **WHEN** the overlay is turned on and off
- **THEN** terrain and object geometry are not rebuilt or re-uploaded

#### Scenario: Following a flag edit

- **GIVEN** the overlay is on
- **WHEN** the user completes or undoes a flag stroke
- **THEN** the markers match the edited flags without a save

#### Scenario: Releasing resources

- **WHEN** the overlay is turned off or the viewport is destroyed
- **THEN** the overlay's GPU resources are released

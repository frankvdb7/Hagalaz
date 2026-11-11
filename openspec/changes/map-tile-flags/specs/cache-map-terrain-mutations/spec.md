# Spec Delta

## MODIFIED Requirements

### Requirement: Change individual terrain tiles

The Cache service SHALL apply submitted changes to individual terrain tiles, setting a tile's stored height, underlay, overlay or flag value, and leaving each value the request omits as stored.

#### Scenario: Setting a height and floors

- **GIVEN** a region with a readable terrain archive
- **WHEN** an administrator submits a height, underlay and overlay with shape and rotation for a tile
- **THEN** a later read returns those values for that tile

#### Scenario: Clearing floors

- **WHEN** an administrator submits underlay 0 or overlay 0 for a tile
- **THEN** a later read reports no underlay or no overlay for that tile

#### Scenario: Applying the same change twice

- **WHEN** the same tile changes are submitted again
- **THEN** the stored terrain decodes the same as after the first submission

#### Scenario: Setting a flag value

- **GIVEN** a tile with no flags
- **WHEN** an administrator submits flag value 1 for that tile
- **THEN** a later read returns flag value 1 for that tile and its height and floors are unchanged

#### Scenario: Replacing and clearing a flag value

- **GIVEN** a tile whose stored flag value is 5
- **WHEN** an administrator submits flag value 4, and later flag value 0
- **THEN** a later read returns 4 after the first submission and no flags after the second

### Requirement: Preserve everything else in the terrain archive

A terrain save MUST NOT change the flags of a tile whose change omits a flag value, unedited tiles, or the data stored after the last terrain tile in the same archive.

#### Scenario: Flags and atmosphere survive

- **GIVEN** a tile with flags in a region with atmosphere and light records
- **WHEN** that tile's height and floors are changed without a flag value
- **THEN** its flags are unchanged and the bytes after the last terrain tile are identical

#### Scenario: Automatic heights survive

- **GIVEN** tiles whose heights are generated automatically
- **WHEN** a different tile is changed
- **THEN** those tiles remain automatically generated

#### Scenario: No effective changes

- **WHEN** a save contains no tile changes
- **THEN** the archive content is reproduced byte for byte

#### Scenario: Unedited tiles stay byte-identical

- **GIVEN** a region where one tile's flag value is changed
- **WHEN** the save is stored
- **THEN** the stored bytes of every other tile are identical to before, and the bytes after the last terrain tile are identical

#### Scenario: Submitting a tile's current flag value

- **WHEN** a save submits, for each tile, the flag value that tile already stores
- **THEN** the archive content is reproduced byte for byte

### Requirement: Reject invalid terrain saves without writing

The Cache service MUST reject a terrain save and leave the cache unchanged when the region or any tile change is out of range or cannot be represented.

#### Scenario: Value out of range

- **WHEN** a change has a coordinate above 63, a plane above 3, a height above 255, an underlay above 174, an overlay above 255, an overlay shape above 11 or an overlay rotation above 3
- **THEN** the service responds with a validation error and writes nothing

#### Scenario: Height with no stored form

- **WHEN** a change submits the stored height value 1, which the format reads as 0
- **THEN** the service responds with a validation error and writes nothing

#### Scenario: Flag value with no stored form

- **WHEN** a change submits a flag value below 0 or above 32, alone or alongside valid changes to other tiles
- **THEN** the service responds with a validation error that names the allowed range and writes nothing

#### Scenario: Region has no terrain archive

- **WHEN** a save targets a region with no terrain archive
- **THEN** the service responds with not found and writes nothing

#### Scenario: Stored terrain cannot be decoded

- **WHEN** a save targets a region whose terrain archive cannot be decoded
- **THEN** the service responds that the terrain cannot be changed and writes nothing

### Requirement: Edit terrain with brushes

The map editor SHALL let the user raise, lower or flatten terrain, paint or clear floors, and set or clear a tile flag on a chosen surface plane using a brush of selectable size, previewing the result without saving.

#### Scenario: Previewing a height stroke

- **WHEN** the user completes a raise stroke
- **THEN** the affected ground and the objects resting on it move in the viewport and nothing is written

#### Scenario: Previewing a new floor

- **WHEN** the user paints a floor the region does not yet use
- **THEN** its colour or texture appears in the viewport

#### Scenario: Heights stay representable

- **WHEN** a stroke would produce a height the format cannot store, or put a plane below the plane under it
- **THEN** the height is limited to the nearest storable value and the preview shows that value

#### Scenario: Region-owned corners only

- **GIVEN** a brush reaching a corner owned by a region that is not loaded
- **WHEN** the stroke is applied
- **THEN** that corner is left unchanged

#### Scenario: Shared corners between loaded regions

- **GIVEN** two loaded neighbouring regions
- **WHEN** a stroke changes a corner on their border
- **THEN** both regions show the new height and only the owning region's tile is submitted

#### Scenario: Undoing a stroke

- **WHEN** the user undoes after a brush stroke
- **THEN** the whole stroke is reverted in one step

#### Scenario: Setting a flag

- **GIVEN** the set-flag tool with the blocked flag chosen
- **WHEN** the user completes a stroke over tiles that also have the roof flag
- **THEN** those tiles are blocked and still have the roof flag, and nothing is written

#### Scenario: Clearing a flag

- **GIVEN** the clear-flag tool with the blocked flag chosen
- **WHEN** the user completes a stroke over blocked tiles
- **THEN** those tiles are no longer blocked and their other flags are unchanged

#### Scenario: Undoing a flag stroke

- **WHEN** the user undoes after a flag stroke
- **THEN** every tile of that stroke returns to its previous flags in one step

### Requirement: Save terrain edits to the cache

The map editor SHALL include terrain edits in the existing save, only after confirmation, submitting only edited regions and only changed tiles, then reload from the cache once all succeed.

#### Scenario: Saving terrain and objects

- **GIVEN** terrain and object edits in one region
- **WHEN** the administrator confirms the save
- **THEN** both are stored and the scene is reloaded from the cache

#### Scenario: A tile returned to its original state

- **WHEN** a tile's edits leave it equal to its stored values
- **THEN** that tile is not submitted, and a region with nothing left to store is not submitted

#### Scenario: Failure keeps edits

- **GIVEN** edits across several saves
- **WHEN** one save fails
- **THEN** the editor reports what was and was not saved, stops submitting, and keeps all local edits

#### Scenario: Saving a flag edit

- **GIVEN** a tile whose flags were changed with a flag brush
- **WHEN** the administrator confirms the save and the scene reloads
- **THEN** the reloaded tile has the edited flags

#### Scenario: Unsaved flags warning

- **GIVEN** tile flag edits in a region
- **WHEN** the administrator is asked to confirm
- **THEN** the confirmation states that flag changes reach a running game world only after it reloads the region, and no longer states that tile flags are not changed

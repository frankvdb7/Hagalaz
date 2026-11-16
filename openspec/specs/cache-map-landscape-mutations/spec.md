# cache-map-landscape-mutations Specification

## Purpose
Defines how administrative edits replace a region's stored landscape placements in the cache, and how the map editor saves local edits safely.

## Requirements

### Requirement: Replace a region's landscape placements

The Cache service SHALL replace a region's stored landscape placements with a submitted list, so that reading the landscape afterwards returns exactly those placements.

#### Scenario: Saving edited placements

- **GIVEN** a region with a readable landscape archive
- **WHEN** an administrator submits a list of placements for that region
- **THEN** a later read of that landscape returns the submitted placements

#### Scenario: Object ID gaps of 32,767 or more

- **GIVEN** submitted placements whose consecutive object IDs differ by 32,767 or more
- **WHEN** they are saved
- **THEN** a later read returns the same object IDs

#### Scenario: Removing every placement

- **WHEN** an administrator submits an empty placement list
- **THEN** a later read of that landscape returns no placements

#### Scenario: Other archives are untouched

- **WHEN** a region's landscape is saved
- **THEN** its terrain archive and every other region's archives remain unchanged

### Requirement: Reject invalid landscape saves without writing

The Cache service MUST reject a landscape save and leave the cache unchanged when the region ID or any placement is out of range.

#### Scenario: Placement out of range

- **WHEN** a submitted placement has an unknown object ID, a shape above 22, a rotation above 3, a coordinate above 63 or a plane above 3
- **THEN** the service responds with a validation error and writes nothing

#### Scenario: Invalid region ID

- **WHEN** the region ID is outside 0 to 65535
- **THEN** the service responds with a validation error and writes nothing

### Requirement: Only replace landscapes that can be read

The Cache service MUST NOT overwrite a region's landscape unless its stored archive exists and decodes without keys, because unread placements would otherwise be lost.

#### Scenario: Region has no landscape archive

- **WHEN** a save targets a region with no landscape archive
- **THEN** the service responds with not found and writes nothing

#### Scenario: Stored landscape cannot be decoded

- **GIVEN** a region whose landscape is encrypted or corrupt
- **WHEN** a save targets that region
- **THEN** the service responds that the landscape cannot be replaced and writes nothing

### Requirement: Save map editor edits to the cache

The editor SHALL present the exact before and after values of each stored placement change, grouped deterministically by region and kind, and require confirmation of the displayed changes before writing. It SHALL explain that requests commit independently and that the operation updates static cache data. Live spawns and the running world remain outside the operation.

#### Scenario: Review exact placement edits

- **GIVEN** changes to stored placements across one or more regions
- **WHEN** the administrator opens the review
- **THEN** it shows the object ID, local coordinates, source plane, shape and rotation for the applicable before and after placements, including additions and removals
- **AND** it discloses scale and underwater edits that will not be stored

#### Scenario: Cancelling confirmation

- **WHEN** the administrator cancels, or the reviewed batch changes before confirmation
- **THEN** the editor writes nothing and preserves the draft

#### Scenario: Scale edits are disclosed

- **GIVEN** an edit changes an object's scale
- **WHEN** the administrator opens the review
- **THEN** the review states that scale changes are not stored

#### Scenario: Underwater objects are left alone

- **GIVEN** edits to underwater objects
- **WHEN** the administrator publishes the batch
- **THEN** the review discloses that those edits are not stored and the write excludes underwater placements

#### Scenario: Successful save

- **WHEN** the administrator confirms and all reviewed writes succeed
- **THEN** the editor reloads the scene from the cache and clears the saved draft and its undo history

#### Scenario: Nothing to save

- **WHEN** there are no writable edits or the editor is already publishing
- **THEN** the save action is unavailable

### Requirement: Keep edits when a save fails

The editor SHALL track outcomes by region and kind and stop at the first failed request. It SHALL retain unsaved edits and their original version checks and reconcile confirmed saves against the cache before allowing further edits against those baselines.

#### Scenario: One region fails

- **GIVEN** an earlier terrain or landscape request succeeds
- **WHEN** a later request fails
- **THEN** the editor identifies the confirmed, failed and unattempted region/kind requests

#### Scenario: Retrying after a partial failure

- **WHEN** the administrator reviews and publishes again after partial success
- **THEN** the batch omits confirmed writes while retaining unsaved edits, including edits of a different kind in the same region

#### Scenario: Conflict or failed write verification

- **WHEN** the server reports a version conflict or cannot verify a write
- **THEN** the editor reports the failure, retains the affected edit and never treats it as confirmed or silently replaces its expected version

#### Scenario: Reload of a confirmed write fails

- **WHEN** a confirmed write cannot be reloaded
- **THEN** the editor reports the reload failure and allows read recovery without resending the confirmed write

### Requirement: Landscape saves never overwrite a landscape they did not check

The Cache service MUST write a landscape save only over the stored landscape it checked was readable. When another write changes the region's landscape after that check, the save MUST check the new stored landscape again before writing. The service does not order two whole-landscape saves of the same region: whichever is written last is stored.

#### Scenario: Another save lands between the check and the write

- **GIVEN** a save has checked that region 12850's stored landscape is readable
- **WHEN** another save writes region 12850's landscape before the first save writes
- **THEN** the first save checks the newly stored landscape before writing, and a later read returns the first save's placements

#### Scenario: The landscape becomes unreadable before the write

- **GIVEN** a save has checked that a region's stored landscape is readable
- **WHEN** the stored landscape is replaced by data that cannot be decoded before the save writes
- **THEN** the service responds that the landscape cannot be replaced and writes nothing

### Requirement: Landscape saves that change nothing are not written

The Cache service MUST NOT write a region's landscape when the submitted placements encode to the same bytes as the stored placements re-encoded, and MUST still report the save as successful.

#### Scenario: Saving the stored placements again

- **GIVEN** a region with a readable landscape
- **WHEN** an administrator submits exactly the placements that are stored
- **THEN** the service reports the landscape as updated and the landscape file, its version and the reference table are unchanged

#### Scenario: Retrying a save that already succeeded

- **GIVEN** a landscape save that was written
- **WHEN** the same save is submitted again
- **THEN** nothing is written, so the region's world map entry is not regenerated because of the retry

### Requirement: Use region-specific keys when publishing protected landscapes

The editor SHALL use the supplied, verified XTEA keys for the target landscape during publish and reconciliation.

#### Scenario: Known keys for one region

- **GIVEN** keys for a protected landscape that has been read successfully
- **WHEN** the administrator publishes changes
- **THEN** the editor supplies those keys for that landscape and does not apply them to other regions

#### Scenario: Missing or unverifiable keys

- **WHEN** a protected landscape cannot be verified with the keys available for that region
- **THEN** the editor explains how to supply or correct its keys, preserves the edit and does not fall back to a write without keys
